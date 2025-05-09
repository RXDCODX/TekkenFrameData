using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using TekkenFrameData.Library.Models.Configuration;
using Telegram.Bot;
using TwitchLib.Api.Core;

namespace TekkenFrameData.Library.DB;

public sealed partial class AppDbContext : DbContext
{
    private static readonly object Locker = new();
    private static bool _isMigrated;
    private static bool _isInitDataChecked;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        if (!_isMigrated)
        {
            lock (Locker)
            {
                if (!_isMigrated)
                {
                    var migrations = Database.GetPendingMigrations();

                    if (migrations.Any())
                    {
                        Database.Migrate();
                    }

                    _isMigrated = true;
                }
            }
        }

        if (!_isInitDataChecked)
        {
            var data = this.Configuration.SingleOrDefault();
            
            if (data is null)
            {
                // Читаем конфигурацию из файла
                var jsonString = File.ReadAllText("def_conf.json");
                var conf = JsonSerializer.Deserialize<Configuration>(jsonString);
                // Добавляем Configuration В бд
                Configuration.Add(conf);
                
            }

            _isInitDataChecked = true;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        AppDbContext.OnFrameDataModelCreatingPartial(modelBuilder);
        AppDbContext.OnConfigurationModelCreatingPartial(modelBuilder);
        AppDbContext.OnFrameDataModelCreatingPartial(modelBuilder);
    }

    public class DateTimeToDateTimeUtc()
        : ValueConverter<DateTime, DateTime>(
            c => DateTime.SpecifyKind(c, DateTimeKind.Utc),
            c => c
        );

    protected sealed override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder
    )
    {
        configurationBuilder.Properties<DateTime>().HaveConversion(typeof(DateTimeToDateTimeUtc));
    }
}
