namespace TekkenFrameData.Library.Models.SignalRInterfaces;

public interface IMainHubCommands
{
    Task StartStream();
    Task StopStream();
    Task SendToMainTwitchMessage();
    Task SendToAdminsTelegramMessage();
}
