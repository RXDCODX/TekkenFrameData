using TwitchLib.Client.Events;

namespace TekkenFrameData.Watcher.Services.TekkenVictorina.Entitys;

public interface ITekkenVictorina
{
    Task GameStart(string userName, string userId);
    void ClearGame();
    void TwitchClientOnMessageReceived(object? sender, OnMessageReceivedArgs args);
}
