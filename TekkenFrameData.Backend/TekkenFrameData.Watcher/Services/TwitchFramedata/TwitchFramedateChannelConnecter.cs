using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Watcher.Services.Framedata;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
using Timer = System.Timers.Timer;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class TwitchFramedateChannelConnecter : IHostedService
{
    private readonly ILogger<TwitchFramedateChannelConnecter> _logger;
    private readonly ITwitchClient _client;
    private readonly Tekken8FrameData _frameData;
    private readonly ITwitchAPI _api;

    private Timer? _timer;

    public TwitchFramedateChannelConnecter(
        ILogger<TwitchFramedateChannelConnecter> logger,
        ITwitchClient client,
        Tekken8FrameData frameData,
        ITwitchAPI api
    )
    {
        _logger = logger;
        _client = client;
        _client.OnMessageReceived += FrameDateMessage;

        _frameData = frameData;
        _api = api;

        _client.OnConnected += (sender, args) => _logger.LogInformation("Твич подключился!");
        _client.OnReconnected += (sender, args) => _logger.LogInformation("Твич подключился!");
        _client.OnDisconnected += (sender, args) => _logger.LogInformation("Твич отключился(");
        _client.OnConnectionError += (sender, args) =>
            _logger.LogError(
                "{BotUsername} # {ErrorMessage}",
                args.BotUsername,
                args.Error.Message
            );
        _client.OnLog += (sender, args) => _logger.LogInformation("{Data}", args.Data);
    }

    public async Task ConnectToStreams()
    {
        if (!_client.IsConnected)
            return;

        var streams = await GetStreamsFromRuTekken();
        var joined = _client.JoinedChannels;
        var newStreams = streams.Where(e =>
            !joined.Any(joined =>
                joined.Channel.Equals(e.UserLogin, StringComparison.OrdinalIgnoreCase)
            ) && !e.Id.Equals("40792090693", StringComparison.OrdinalIgnoreCase)
        );
        var streamsToLeave = joined.Where(e =>
            !streams.Any(stream =>
                stream.UserLogin.Equals(e.Channel, StringComparison.OrdinalIgnoreCase)
            )
        );

        foreach (var joinedChannel in streamsToLeave)
        {
            _client.LeaveChannel(joinedChannel);
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
                _client.JoinChannel(stream.UserLogin);
            }

            await Task.Delay(500);
        }
    }

    private async Task<ChatSettingsResponseModel> GetStreamInfo(string id)
    {
        try
        {
            var response = await _api.Helix.Chat.GetChatSettingsAsync(id, id);
            return response.Data[0];
        }
        catch (Exception e)
            when (e.Message.Contains("Invalid OAuth token", StringComparison.OrdinalIgnoreCase))
        {
            var aa = await ValidateToken(_api.Settings.AccessToken);
            if (!aa)
            {
                _api.Settings.AccessToken = await _api.Auth.GetAccessTokenAsync();
            }

            return await GetStreamInfo(id);
        }
    }

    private async Task<Stream[]> GetStreamsFromRuTekken()
    {
        try
        {
            var clipsResponse = await _api.Helix.Streams.GetStreamsAsync(
                first: 100,
                gameIds: ["538054672"],
                languages: ["ru"]
            );
            return clipsResponse.Streams;
        }
        catch (Exception e)
            when (e.Message.Contains("Invalid OAuth token", StringComparison.OrdinalIgnoreCase))
        {
            var aa = await ValidateToken(_api.Settings.AccessToken);
            if (!aa)
            {
                _api.Settings.AccessToken = await _api.Auth.GetAccessTokenAsync();
            }

            return await GetStreamsFromRuTekken();
        }
    }

    private async Task<bool> ValidateToken(string token)
    {
        try
        {
            var response = await _api.Auth.ValidateAccessTokenAsync(token);

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
            _logger.LogException(e);
            return false;
        }
    }

    private async void FrameDateMessage(object? sender, OnMessageReceivedArgs args)
    {
        var message = args.ChatMessage.Message;

        if (message.StartsWith("!fd ", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Factory.StartNew(async () =>
            {
                var split = message.Split(' ');

                if (split.Length > 2)
                {
                    var bb = split.Skip(1).ToArray();
                    var move = await _frameData.GetMoveAsync(bb);
                    var channel = args.ChatMessage.Channel;

                    if (move is not null)
                    {
                        var teges = await _frameData.GetMoveTags(move);

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
                                !_client.JoinedChannels.Any(e =>
                                    e.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)
                                )
                            )
                            {
                                _client.JoinChannel(channel);
                            }

                            var joinedChannel = _client.GetJoinedChannel(channel);
                            _client.SendMessage(joinedChannel, message);
                            return;
                        }
                        catch (Exception e)
                        {
                            _logger.LogException(e);
                        }
                    }

                    const string tempLate = @"@{0}, кривые параметры запроса фреймдаты";

                    message = string.Format(tempLate, args.ChatMessage.Username);

                    if (
                        !_client.JoinedChannels.Any(e =>
                            e.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        _client.JoinChannel(channel);
                    }

                    var joined = _client.GetJoinedChannel(channel);
                    _client.SendMessage(joined, message);
                }
            });
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(TimeSpan.FromMinutes(5)) { AutoReset = true };
        _timer.Elapsed += async (sender, args) => await ConnectToStreams();

        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Dispose();
        return Task.CompletedTask;
    }
}
