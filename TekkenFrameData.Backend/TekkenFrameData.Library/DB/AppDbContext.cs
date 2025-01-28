using Microsoft.EntityFrameworkCore;

namespace TekkenFrameData.Library.DB;

public partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        AppDbContext.OnFrameDataModelCreatingPartial(modelBuilder);
        AppDbContext.OnConfigurationModelCreatingPartial(modelBuilder);
    }

}