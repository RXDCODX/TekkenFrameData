using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TekkenFrameData.Library.DB;

public sealed partial class AppDbContext : DbContext
{
    private static readonly Lock Locker = new();
    private static bool _isMigrated;

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
        configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeToDateTimeUtc>();
    }
}
