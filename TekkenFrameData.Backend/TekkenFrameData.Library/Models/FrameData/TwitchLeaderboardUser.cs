namespace TekkenFrameData.Library.Models.FrameData;

public class TwitchLeaderboardUser
{
    public required string TwitchId { get; set; }
    public required string ChannelId { get; set; }
    public required string DisplayName { get; set; }
    public int TekkenVictorinaWins { get; set; }
}
