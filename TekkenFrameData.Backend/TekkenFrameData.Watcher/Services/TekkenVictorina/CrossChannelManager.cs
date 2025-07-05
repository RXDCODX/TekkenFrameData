using System.Collections.Concurrent;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.TekkenVictorina.Entitys;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TekkenVictorina;

public class CrossChannelManager(
    IHostApplicationLifetime lifetime,
    ITwitchClient client,
    IDbContextFactory<AppDbContext> factory,
    Tekken8FrameData frameData,
    TekkenVictorinaLeaderbord leaderbord
) : BackgroundService
{
    private readonly CancellationToken cancellationToken = lifetime.ApplicationStopping;

    public static readonly ConcurrentDictionary<
        string,
        ITekkenVictorina
    > ChannelsWithActiveVictorina = [];
    public static readonly ConcurrentDictionary<string, TekkenMove> MovesInVictorina = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnChatCommandReceived += ClientOnOnChatCommandReceived;
            client.OnMessageReceived += ClientOnOnMessageReceived;
        });

        return Task.CompletedTask;
    }

    private async void ClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        foreach (var pair in ChannelsWithActiveVictorina)
        {
            await Task.Factory.StartNew(
                () =>
                {
                    pair.Value.TwitchClientOnMessageReceived(sender, e);
                },
                cancellationToken
            );
        }
    }

    private async void ClientOnOnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var channelId = e.Command.ChatMessage.RoomId;
        var channel = e.Command.ChatMessage.Channel;
        var command = e.Command.CommandText.ToLower();
        var userName = e.Command.ChatMessage.DisplayName;
        var userId = e.Command.ChatMessage.UserId;

        if (
            command == "tekken_victorina"
            && (
                e.Command.ChatMessage.IsBroadcaster
                || e.Command.ChatMessage.IsModerator
                || e.Command.ChatMessage.UserId == TwitchClientExstension.AuthorId.ToString()
            )
        )
        {
            await Task.Factory.StartNew(
                async () =>
                {
                    if (ChannelsWithActiveVictorina.Any(ce => ce.Key.Equals(channelId)))
                    {
                        var joinedChannel = client.GetJoinedChannel(channel);

                        if (joinedChannel != null)
                        {
                            client.SendMessage(
                                joinedChannel,
                                $"@{userName}, другая викторина уже запущенна!"
                            );
                        }

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

                    if (isPassed)
                    {
                        var randomIndex = Random.Shared.Next(frameData.VictorinaMoves.Count) - 1;
                        var randomMove = frameData.VictorinaMoves[randomIndex];
                        MovesInVictorina.AddOrUpdate(
                            channel,
                            (_) => randomMove,
                            (k, move) => randomMove
                        );

                        var game = new TekkenVictorina(
                            leaderbord,
                            randomMove,
                            channel,
                            channelId,
                            (message) =>
                            {
                                client.SendMessage(channel, message);
                            },
                            () =>
                            {
                                Task.Factory.StartNew(
                                    async () =>
                                    {
                                        while (!MovesInVictorina.TryRemove(channel, out var value))
                                        {
                                            await Task.Delay(500, cancellationToken)
                                                .ConfigureAwait(false);
                                        }

                                        while (
                                            !ChannelsWithActiveVictorina.TryRemove(
                                                channelId,
                                                out var value
                                            )
                                        )
                                        {
                                            await Task.Delay(500, cancellationToken)
                                                .ConfigureAwait(false);
                                        }
                                    },
                                    cancellationToken
                                );
                            }
                        );

                        ChannelsWithActiveVictorina.GetOrAdd(channelId, game);
                        await game.GameStart(userName, userId);
                    }
                },
                cancellationToken
            );
        }
    }
}
