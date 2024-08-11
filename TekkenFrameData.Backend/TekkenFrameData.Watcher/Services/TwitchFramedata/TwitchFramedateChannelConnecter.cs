using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.TekkenVictorina;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
using Timer = System.Timers.Timer;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class TwitchFramedateChannelConnecter(
    ILogger<TwitchFramedateChannelConnecter> logger,
    ITwitchClient client,
    Tekken8FrameData frameData,
    ITwitchAPI api,
    IDbContextFactory<AppDbContext> factory
) : IHostedService
{
    private Timer? _timer;
    private static readonly Regex Regex = new Regex(@"\p{C}+");

    public async Task ConnectToStreams()
    {
        var streams = await GetStreamsFromRuTekken();
        var joined = client.JoinedChannels;
        var newStreams = streams.Where(e =>
            joined.All(joinedChannel =>
                !joinedChannel.Channel.Equals(e.UserLogin, StringComparison.OrdinalIgnoreCase)
            ) && !e.Id.Equals("40792090693", StringComparison.OrdinalIgnoreCase)
        );
        var streamsToLeave = joined.Where(joinedChannel =>
            // Канал не найден в текущих стримах
            !streams.Any(stream =>
                string.Equals(
                    stream.UserLogin,
                    joinedChannel.Channel,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            &&
            // И это не основной канал
            !string.Equals(
                joinedChannel.Channel,
                TwitchClientExstension.Channel,
                StringComparison.OrdinalIgnoreCase
            )
        );

        foreach (var joinedChannel in streamsToLeave)
        {
            client.LeaveChannel(joinedChannel);
        }

        foreach (var stream in newStreams)
        {
            var channelInformations = await GetStreamInfo(stream.Id);
            if (
                channelInformations is
                {
                    FollowerMode: false,
                    EmoteMode: false,
                    SubscriberMode: false,
                    UniqueChatMode: false
                }
            )
            {
                client.JoinChannel(stream.UserLogin);
            }

            await Task.Delay(500);
        }

        if (
            !client.JoinedChannels.Any(e =>
                e.Channel.Equals(TwitchClientExstension.Channel, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            client.JoinChannel(TwitchClientExstension.Channel);
        }
    }

    private async Task<ChatSettingsResponseModel> GetStreamInfo(string id)
    {
        try
        {
            var response = await api.Helix.Chat.GetChatSettingsAsync(id, id);
            return response.Data[0];
        }
        catch (Exception e)
            when (e.Message.Contains("Invalid OAuth token", StringComparison.OrdinalIgnoreCase))
        {
            var aa = await ValidateToken(api.Settings.AccessToken);
            if (!aa)
            {
                api.Settings.AccessToken = await api.Auth.GetAccessTokenAsync();
            }

            return await GetStreamInfo(id);
        }
    }

    private async Task<Stream[]> GetStreamsFromRuTekken()
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var allowedChannels = await dbContext
                .TekkenChannels.Where(e => e.FramedataStatus == TekkenFramedataStatus.Accepted)
                .ToArrayAsync();

            var clipsResponse = await api.Helix.Streams.GetStreamsAsync(
                first: 100,
                gameIds: ["538054672"],
                languages: ["ru"]
            );

            return clipsResponse
                .Streams.Where(e => allowedChannels.Any(t => t.TwitchId == e.UserId))
                .ToArray();
        }
        catch (Exception e)
            when (e.Message.Contains("Invalid OAuth token", StringComparison.OrdinalIgnoreCase))
        {
            var aa = await ValidateToken(api.Settings.AccessToken);
            if (!aa)
            {
                api.Settings.AccessToken = await api.Auth.GetAccessTokenAsync();
            }

            return await GetStreamsFromRuTekken();
        }
    }

    private async Task<bool> ValidateToken(string token)
    {
        try
        {
            var response = await api.Auth.ValidateAccessTokenAsync(token);

            if (response == null)
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
            when (e.Message.Contains("invalid access token", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            return false;
        }
    }

    private async void FrameDateCommand(object? sender, OnChatCommandReceivedArgs args)
    {
        var message = args.Command.ChatMessage.Message;
        var userName = args.Command.ChatMessage.DisplayName;
        var command = args.Command.CommandText;

        if (command.Equals("fd", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Factory.StartNew(async () =>
            {
                var split = Regex.Replace(message, "").Trim().Split(' ');

                if (split.Length > 2)
                {
                    var bb = split.Skip(1).ToArray();
                    var move = await frameData.GetMoveAsync(bb);
                    var channel = args.Command.ChatMessage.Channel;

                    if (move is not null)
                    {
                        if (CrossChannelManager.MovesInVictorina.Values.Contains(move))
                        {
                            client.SendMessage(
                                channel,
                                $"@{userName}, этот удар находиться в теккен виткорине, пока что не могу подсказать!"
                            );
                            return;
                        }

                        var teges = await frameData.GetMoveTags(move);

                        message =
                            "✅ "
                            + move.Character.Name
                            + " > "
                            + move.Command
                            + " ✅  "
                            + "Block: "
                            + move.BlockFrame
                            + " | Dmg: "
                            + move.Damage
                            + " | Hit: "
                            + move.HitFrame
                            + " | HitLvl: "
                            + move.HitLevel
                            + " | StartUp: "
                            + move.StartUpFrame
                            + (string.IsNullOrEmpty(teges) ? "" : " | Tags: " + teges);

                        try
                        {
                            if (
                                !client.JoinedChannels.Any(e =>
                                    e.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)
                                )
                            )
                            {
                                client.JoinChannel(channel);
                            }

                            var joinedChannel = client.GetJoinedChannel(channel);
                            client.SendMessage(joinedChannel, message);
                            return;
                        }
                        catch (Exception e)
                        {
                            logger.LogException(e);
                        }
                    }

                    const string tempLate = @"@{0}, кривые параметры запроса фреймдаты";

                    message = string.Format(tempLate, userName);

                    if (
                        !client.JoinedChannels.Any(e =>
                            e.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        client.JoinChannel(channel);
                    }

                    var joined = client.GetJoinedChannel(channel);
                    client.SendMessage(joined, message);
                }
            });
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(TimeSpan.FromMinutes(2)) { AutoReset = true };
        _timer.Elapsed += async (sender, args) => await ConnectToStreams();

        _timer.Start();

        client.OnChatCommandReceived += FrameDateCommand;
        client.OnConnected += (sender, args) => logger.LogInformation("Твич подключился!");
        client.OnReconnected += (sender, args) => logger.LogInformation("Твич подключился!");
        client.OnDisconnected += (sender, args) => logger.LogInformation("Твич отключился(");
        client.OnConnectionError += (sender, args) =>
            logger.LogError("{BotUsername} # {ErrorMessage}", args.BotUsername, args.Error.Message);
        client.OnLog += (sender, args) => logger.LogInformation("{Data}", args.Data);
        return Task.Factory.StartNew(ConnectToStreams, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Dispose();
        return Task.CompletedTask;
    }
}
