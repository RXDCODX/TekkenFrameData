using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.FrameData;

namespace TekkenFrameData.Cli.DB;

public partial class AppDbContext
{
    public DbSet<TekkenMove> TekkenMoves { get; set; } = null!;
    public DbSet<TekkenCharacter> TekkenCharacters { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
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