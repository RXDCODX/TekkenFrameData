using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TekkenVictorina;

public class CrossChannelManager(
    IHostApplicationLifetime lifetime,
    ITwitchClient client,
    IDbContextFactory<AppDbContext> factory
) : BackgroundService
{
    private readonly CancellationToken cancellationToken = lifetime.ApplicationStopping;

    public static readonly ConcurrentBag<string> ChannelsWithActiveVictorina = [];
    public static readonly ConcurrentBag<TekkenMove> MovesInVictorina = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnChatCommandReceived += ClientOnOnChatCommandReceived;
        });

        return Task.CompletedTask;
    }

    private async void ClientOnOnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var channelId = e.Command.ChatMessage.RoomId;
        var channel = e.Command.ChatMessage.Channel;
        var command = e.Command.CommandText;
        var userName = e.Command.ChatMessage.DisplayName;

        if (command == "tekken_victorina")
        {
            await Task.Factory.StartNew(
                async () =>
                {
                    if (ChannelsWithActiveVictorina.Any(ce => ce.Equals(channelId)))
                    {
                        var joinedChannel = client.GetJoinedChannel(channel);

                        if (joinedChannel != null)
                            client.SendMessage(
                                joinedChannel,
                                $"@{userName}, другая викторина уже запущенна!"
                            );
                        return;
                    }
                    await using var dbContext = await factory.CreateDbContextAsync(
                        cancellationToken
                    );

                    var isPassed = await dbContext.TekkenChannels.AnyAsync(
                        c =>
                            c.TwitchId == channelId
                            && c.FramedataStatus == TekkenFramedataStatus.Accepted,
                        cancellationToken: cancellationToken
                    );

                    if (isPassed) { }
                },
                cancellationToken
            );
        }
    }
}
