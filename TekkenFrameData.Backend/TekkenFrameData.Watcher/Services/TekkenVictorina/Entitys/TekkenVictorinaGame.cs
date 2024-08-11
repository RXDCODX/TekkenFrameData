using System.Collections.Generic;

namespace TekkenFrameData.Watcher.Services.TekkenVictorina.Entitys;

public class TekkenVictorinaGame(IntRange answer)
{
    public bool Active { get; set; } = true;
    public CancellationTokenSource CancellationTokenForRightAnswer = new();
    public IntRange Answer = answer;
    public List<string> Users = [];
    public bool IsWaifuHelp = false;
    public string? WaifuId { get; set; }
    public List<(string displayName, IntRange answer)> GoodAnswers = [];

    public Task FoundRightAnswerTask()
    {
        return CancellationTokenForRightAnswer.CancelAsync();
    }
}
