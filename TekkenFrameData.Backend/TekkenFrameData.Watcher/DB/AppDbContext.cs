using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Watcher.Domains.FrameData;

namespace TekkenFrameData.Watcher.DB;

public sealed class AppDbContext : DbContext
{
    private static bool _isDbReCreated = false;
    private static readonly Lock Locker = new();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        ChangeTracker.AutoDetectChangesEnabled = false;

        if (!_isDbReCreated)
        {
            lock (Locker)
            {
                _isDbReCreated = true;

                Database.EnsureDeleted();
                Database.EnsureCreated();
            }
        }
    }

    public DbSet<TekkenMove> TekkenMoves { get; set; } = null!;
    public DbSet<TekkenCharacter> TekkenCharacters { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TekkenMove>()
            .HasKey(o => new { o.CharacterName, o.Command });

        modelBuilder.Entity<TekkenMove>()
            .HasOne(m => m.Character)
            .WithMany(c => c.Movelist)
            .HasForeignKey(e => e.CharacterName)
            .OnDelete(DeleteBehavior.NoAction); // assuming you add a CharacterId property to Move

        modelBuilder.Entity<TekkenCharacter>()
            .HasMany(c => c.Movelist)
            .WithOne(m => m.Character)
            .HasForeignKey(e => e.CharacterName)
            .OnDelete(DeleteBehavior.NoAction);
    }
}