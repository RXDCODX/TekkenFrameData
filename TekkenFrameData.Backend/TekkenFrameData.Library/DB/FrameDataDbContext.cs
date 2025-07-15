using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.Discord;
using TekkenFrameData.Library.Models.FrameData;

namespace TekkenFrameData.Library.DB;

public sealed partial class AppDbContext
{
    public DbSet<Move> TekkenMoves { get; set; } = null!;
    public DbSet<Character> TekkenCharacters { get; set; } = null!;

    private static void OnFrameDataModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Move>().HasKey(o => new { o.CharacterName, o.Command });

        modelBuilder
            .Entity<Move>()
            .HasOne(m => m.Character)
            .WithMany(c => c.Movelist)
            .HasForeignKey(e => e.CharacterName)
            .OnDelete(DeleteBehavior.NoAction); // assuming you add a CharacterId property to Move

        modelBuilder
            .Entity<Character>()
            .HasMany(c => c.Movelist)
            .WithOne(m => m.Character)
            .HasForeignKey(e => e.CharacterName)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TwitchLeaderboardUser>().HasKey(e => new { e.TwitchId, e.ChannelId });
    }
}
