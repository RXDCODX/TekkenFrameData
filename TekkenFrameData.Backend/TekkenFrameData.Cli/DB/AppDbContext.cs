using Microsoft.EntityFrameworkCore;

namespace TekkenFrameData.Cli.DB;

public partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        OnFrameDataModelCreatingPartial(modelBuilder);
        OnConfigurationModelCreatingPartial(modelBuilder);
    }

}