using Microsoft.AspNetCore.SignalR.Client;
using OBSWebsocketDotNet;
using TekkenFrameData.Library.Models.SignalRInterfaces;

namespace TekkenFrameData.Streamer.Server.Services.StreamControl;

public class StreamControlService(HubConnection connection) : BackgroundService
{
    private readonly OBSWebsocket webSocket = new();

    private Task SendTwitch(string message) =>
        connection.InvokeAsync(nameof(IMainHubCommands.SendToMainTwitchMessage), message);

    private Task SendTelegram(string message) =>
        connection.InvokeAsync(nameof(IMainHubCommands.SendToAdminsTelegramMessage), message);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        webSocket.Disconnected += (sender, info) =>
            SendTelegram($"Streamer websocker disconnected # " + info.DisconnectReason);

        webSocket.Connected += (sender, args) => SendTelegram($"Streamer wesocket connected!");

        var pass = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AUTOSTART"));
        var port = Environment.GetEnvironmentVariable("WS_PORT") ?? "9992";
        var hostname = Environment.GetEnvironmentVariable("WS_URL") + ":" + port;
        var url = !string.IsNullOrWhiteSpace(hostname) ? hostname : "ws://172.17.0.1:" + port;
        var password =
            Environment.GetEnvironmentVariable("WS_PASSWORD") ?? throw new NullReferenceException();

        if (pass)
        {
            webSocket.ConnectAsync(url, password);
        }

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
