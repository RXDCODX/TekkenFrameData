using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.Twitch;

namespace TekkenFrameData.Library.DB;

public partial class AppDbContext
{
    public DbSet<TwitchTekkenChannel> TekkenChannels { get; set; } = null!;
    public DbSet<TwitchAcceptesToken> AcceptesTokens { get; set; } = null!;
    public DbSet<TwitchLeaderboardUser> TwitchLeaderboardUsers { get; set; } = null!;
    public DbSet<GlobalNotificationMessage> GlobalNotificationMessage { get; set; } = null!;
    public DbSet<TwitchNotificationChannelsState> GlobalNotificatoinChannelsState { get; set; } =
        null!;

    private static void OnTwitchCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<TwitchNotificationChannelsState>()
            .HasOne(e => e.Message)
            .WithMany()
            .HasForeignKey(e => e.MessageId)
            .IsRequired();

        modelBuilder
            .Entity<TwitchNotificationChannelsState>()
            .HasOne(e => e.Channel)
            .WithMany()
            .HasForeignKey(e => e.ChannelId)
            .IsRequired();
    }
}
