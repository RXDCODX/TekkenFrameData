using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TekkenFrameData.Library.Models.DailyStreak.structures;

namespace TekkenFrameData.Library.DB;

public sealed partial class AppDbContext : DbContext
{
    private static readonly Lock Locker = new();
    private static bool _isMigrated;

    public AppDbContext(DbContextOptions<AppDbContext> options, bool isMigrations)
        : base(options)
    {
        if (!_isMigrated && !isMigrations)
        {
            Locker.Enter();
            if (!_isMigrated)
            {
                var migrations = Database.GetPendingMigrations();

                if (migrations.Any())
                {
                    Database.Migrate();
                }

                _isMigrated = true;
            }

            Locker.Exit();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        AppDbContext.OnFrameDataModelCreatingPartial(modelBuilder);
        AppDbContext.OnConfigurationModelCreatingPartial(modelBuilder);
        AppDbContext.OnFrameDataModelCreatingPartial(modelBuilder);
        AppDbContext.OnTwitchCreatingPartial(modelBuilder);
    }

    public class DateTimeToDateTimeUtc()
        : ValueConverter<DateTime, DateTime>(
            c => DateTime.SpecifyKind(c, DateTimeKind.Utc),
            c => c
        );

    public class TekkenIdToStringConversion()
        : ValueConverter<TekkenId, string>(id => id.ToString(), a => TekkenId.Parse(a)) { }

    protected sealed override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder
    )
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeToDateTimeUtc>();
        configurationBuilder.Properties<TekkenId>().HaveConversion<TekkenIdToStringConversion>();
    }
}
