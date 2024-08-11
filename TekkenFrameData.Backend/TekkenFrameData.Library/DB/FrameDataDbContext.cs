using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.FrameData;

namespace TekkenFrameData.Library.DB;

public sealed partial class AppDbContext
{
    public DbSet<TekkenMove> TekkenMoves { get; set; } = null!;
    public DbSet<TekkenCharacter> TekkenCharacters { get; set; } = null!;
    public DbSet<TwitchTekkenChannel> TekkenChannels { get; set; } = null!;
    public DbSet<TwitchAcceptesToken> AcceptesTokens { get; set; } = null!;
    public DbSet<TwitchLeaderboardUser> TwitchLeaderboardUsers { get; set; } = null!;

    private static void OnFrameDataModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TekkenMove>().HasKey(o => new { o.CharacterName, o.Command });

        modelBuilder
            .Entity<TekkenMove>()
            .HasOne(m => m.Character)
            .WithMany(c => c.Movelist)
            .HasForeignKey(e => e.CharacterName)
            .OnDelete(DeleteBehavior.NoAction); // assuming you add a CharacterId property to Move

        modelBuilder
            .Entity<TekkenCharacter>()
            .HasMany(c => c.Movelist)
            .WithOne(m => m.Character)
            .HasForeignKey(e => e.CharacterName)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TwitchLeaderboardUser>().HasKey(e => new { e.TwitchId, e.ChannelId });
    }
}
