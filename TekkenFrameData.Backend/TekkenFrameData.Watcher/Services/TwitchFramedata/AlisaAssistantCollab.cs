using System.Collections.Generic;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class AlisaAssistantCollab(ITwitchClient client, IHostApplicationLifetime lifetime)
    : BackgroundService
{
    private static readonly HashSet<string> Channels = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += ClientOnOnMessageReceived;
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            client.OnMessageReceived -= ClientOnOnMessageReceived;
        });

        return Task.CompletedTask;
    }

    private void ClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        if (
            e.ChatMessage.DisplayName.Equals("AlisaAssistant", StringComparison.OrdinalIgnoreCase)
            && !Channels.Contains(e.ChatMessage.Channel)
        )
        {
            Task.Factory.StartNew(() =>
            {
                client.SendMessage(
                    e.ChatMessage.Channel,
                    "Произошла перезагрузка помощника Ассистента Алисы. Все протоколы были обновлены. Ожидаю команды."
                );
                Channels.Add(e.ChatMessage.Channel);
            });
        }
    }
}
