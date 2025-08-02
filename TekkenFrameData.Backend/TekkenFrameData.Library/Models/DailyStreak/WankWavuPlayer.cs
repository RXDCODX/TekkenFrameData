using System.ComponentModel.DataAnnotations;
using TekkenFrameData.Library.Models.DailyStreak.structures;

namespace TekkenFrameData.Library.Models.DailyStreak;

public class WankWavuPlayer
{
    [Key]
    public required string TwitchId { get; set; }
    public TekkenId TekkenId { get; set; }
    public string? SteamLink { get; set; }
    public string[]? Nicknames { get; set; }
    public string? PSNLink { get; set; }
    public required string CurrentNickname { get; set; }
}
