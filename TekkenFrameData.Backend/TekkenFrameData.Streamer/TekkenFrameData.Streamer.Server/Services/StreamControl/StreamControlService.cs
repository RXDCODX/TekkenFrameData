using Microsoft.AspNetCore.SignalR.Client;
using OBSWebsocketDotNet;
using TekkenFrameData.Library.Models.SignalRInterfaces;

namespace TekkenFrameData.Streamer.Server.Services.StreamControl;

public class StreamControlService(HubConnection connection, OBSWebsocket webSocket)
    : BackgroundService
{
    private Task SendTwitch(string message) =>
        connection.InvokeAsync(nameof(IMainHubCommands.SendToMainTwitchMessage), message);

    private Task SendTelegram(string message) =>
        connection.InvokeAsync(nameof(IMainHubCommands.SendToAdminsTelegramMessage), message);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        webSocket.Disconnected += (sender, info) =>
            SendTelegram($"Streamer websocker disconnected # " + info.DisconnectReason);

        webSocket.Connected += (sender, args) => SendTelegram($"Streamer wesocket connected!");

        webSocket.ConnectAsync(
            Environment.GetEnvironmentVariable("https://172.17.0.1:9992")
                ?? throw new NullReferenceException(),
            Environment.GetEnvironmentVariable("WS_PASSWORD") ?? throw new NullReferenceException()
        );

        return Task.CompletedTask;
    }

    public void StartStream()
    {
        webSocket.StartStream();
    }

    public void StopStream()
    {
        webSocket.StopStream();
    }
}
