using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Library.Models.DailyStreak;

public class WankWavuPlayerStats
{
    public int Id { get; set; }
    public required string TwitchId { get; init; }
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public required Dictionary<string, DailyStatsChanges> StatsChanges { get; set; }
}

public class DailyStatsChanges
{
    public int TotalMatchesCount { get; set; }
    public int Wins { get; set; }
    public int PtsDifference { get; set; }
}
