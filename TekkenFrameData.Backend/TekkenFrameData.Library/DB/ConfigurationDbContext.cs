using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.Configuration;

namespace TekkenFrameData.Library.DB;

public partial class AppDbContext
{
    public DbSet<Configuration> Configuration { get; set; } = null!;

    private static void OnConfigurationModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Configuration>().HasKey(e => e.Id);
    }
}