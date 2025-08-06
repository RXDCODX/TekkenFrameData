using System.Net.Http;
using TekkenFrameData.Library.Exstensions;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
using Timer = System.Timers.Timer;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class TwitchFramedateChannelConnecter(
    ILogger<TwitchFramedateChannelConnecter> logger,
    ITwitchClient client,
    ITwitchAPI api,
    IHostApplicationLifetime lifetime,
    TwitchFramedataChannelsEvents events
) : IHostedService
{
    private Timer? _timer;
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;

    private bool IsRequesting { get; set; }

    public async Task ConnectToStreams()
    {
        IsRequesting = true;
        var streams = await GetStreamsFromRuTekken();
        IsRequesting = false;
        var joined = client.JoinedChannels;
        var newStreams = streams
            .Where(e =>
                joined.All(joinedChannel =>
                    !joinedChannel.Channel.Equals(e.UserLogin, StringComparison.OrdinalIgnoreCase)
                ) && !e.UserId.Equals("785975641", StringComparison.OrdinalIgnoreCase)
            )
            .ToArray();
        //var streamsToLeave = joined.Where(joinedChannel =>
        //    // Канал не найден в текущих стримах
        //    !streams.Any(stream =>
        //        string.Equals(
        //            stream.UserLogin,
        //            joinedChannel.Channel,
        //            StringComparison.OrdinalIgnoreCase
        //        )
        //    )
        //    &&
        //    // И это не основной канал
        //    !string.Equals(
        //        joinedChannel.Channel,
        //        TwitchClientExstension.Channel,
        //        StringComparison.OrdinalIgnoreCase
        //    )
        //);

        //foreach (var joinedChannel in streamsToLeave)
        //{
        //    client.LeaveChannel(joinedChannel);
        //}

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

            await Task.Delay(500, _cancellationToken);
        }

        if (
            !client.JoinedChannels.Any(e =>
                e.Channel.Equals(TwitchClientExstension.Channel, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            client.JoinChannel(TwitchClientExstension.Channel);
        }

        events.InvokeChannelsConnected(newStreams);
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
            var clipsResponse = await api.Helix.Streams.GetStreamsAsync(
                first: 100,
                gameIds: ["538054672"],
                languages: ["ru"]
            );

            return [.. clipsResponse.Streams];
        }
        catch (HttpRequestException e)
            when (e.Message.Contains("The SSL connection could not be established")
                || e.Message.Contains("Resource temporarily unavailable")
            )
        {
            logger.LogException(e);
            await Task.Delay(TimeSpan.FromMinutes(5), _cancellationToken);
            return await GetStreamsFromRuTekken();
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

            return response != null;
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(TimeSpan.FromMinutes(2)) { AutoReset = true };
        _timer.Elapsed += async (sender, args) =>
        {
            if (!IsRequesting)
            {
                await ConnectToStreams();
            }
        };

        _timer.Start();

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
