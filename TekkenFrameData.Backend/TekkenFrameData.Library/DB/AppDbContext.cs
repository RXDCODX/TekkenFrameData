using Microsoft.EntityFrameworkCore;

namespace TekkenFrameData.Library.DB;

public sealed partial class AppDbContext : DbContext
{
    private static readonly object Locker = new();
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
}
