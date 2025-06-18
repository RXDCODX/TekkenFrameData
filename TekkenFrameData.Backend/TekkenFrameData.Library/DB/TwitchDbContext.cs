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
}
