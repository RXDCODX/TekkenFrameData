namespace TekkenFrameData.Library.Models.DailyStreak;

public class MatchStat
{
    public DateTime DateTime { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public string Player1Character { get; set; } = string.Empty;
    public int Player1Rating { get; set; }
    public int Player1RatingChange { get; set; }
    public string Player2Name { get; set; } = string.Empty;
    public string Player2Character { get; set; } = string.Empty;
    public int Player2Rating { get; set; }
    public int Player2RatingChange { get; set; }
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public bool IsWin { get; set; }
} 