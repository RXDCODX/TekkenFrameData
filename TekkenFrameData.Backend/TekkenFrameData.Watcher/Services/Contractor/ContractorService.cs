using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.Contractor;

public class ContractorService(
    IHostApplicationLifetime lifetime,
    ITwitchClient client,
    IDbContextFactory<AppDbContext> factory
) : BackgroundService
{
    private CancellationToken CancellationToken => lifetime.ApplicationStopping;
    private readonly ConcurrentDictionary<string, DateTimeOffset> CoolDowns = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += ClientOnOnMessageReceived;
            client.OnChatCommandReceived += ClientOnOnChatCommandReceived;
        });

        return Task.CompletedTask;
    }

    private async void ClientOnOnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var channel = e.Command.ChatMessage.Channel;
        var command = e.Command.CommandText.ToLower();

        if (channel.Equals(TwitchClientExstension.Channel, StringComparison.OrdinalIgnoreCase))
        {
            await Task.Factory.StartNew(
                () =>
                {
                    return command switch
                    {
                        "start" => ContractorHelper.StartTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        "accept" => ContractorHelper.AcceptTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        "cancel" => ContractorHelper.CancelTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        _ => Task.CompletedTask,
                    };
                },
                CancellationToken
            );
        }
    }

    private async void ClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var channel = e.ChatMessage.Channel;
        var userName = e.ChatMessage.DisplayName;
        var userId = e.ChatMessage.UserId;

        if (channel.Equals("higemus", StringComparison.OrdinalIgnoreCase))
        {
            if (CoolDowns.Count > 50)
            {
                while (CoolDowns.TryRemove(CoolDowns.First()))
                {
                    await Task.Delay(500, CancellationToken);
                }
            }

            if (!CoolDowns.TryGetValue(userId, out var value))
            {
                await Task.Factory.StartNew(
                    async () =>
                    {
                        await client.SendMessageToMainTwitchAsync(
                            $"@{userName}, для начала - команда !start"
                        );
                    },
                    CancellationToken
                );
                CoolDowns.AddOrUpdate(
                    userId,
                    (s => DateTimeOffset.Now),
                    (_, __) => DateTimeOffset.Now
                );
            }
            else
            {
                if (DateTimeOffset.Now - value < TimeSpan.FromMinutes(5))
                    return;
                await Task.Factory.StartNew(
                    async () =>
                    {
                        await client.SendMessageToMainTwitchAsync(
                            $"@{userName}, для начала - команда !start"
                        );
                    },
                    CancellationToken
                );
                CoolDowns.AddOrUpdate(
                    userId,
                    (s => DateTimeOffset.Now),
                    (_, __) => DateTimeOffset.Now
                );
            }
        }
    }
}
