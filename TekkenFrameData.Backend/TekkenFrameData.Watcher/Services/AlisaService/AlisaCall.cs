using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using Telegram.Bot;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.AlisaService;

public class AlisaCall(
    ITelegramBotClient telegramClient,
    ITwitchClient twitchClient,
    IHostApplicationLifetime lifetime,
    IDbContextFactory<AppDbContext> factory
) : BackgroundService
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            twitchClient.OnMessageReceived += TwitchClientOnOnMessageReceived;
        });
        lifetime.ApplicationStopping.Register(() =>
        {
            twitchClient.OnMessageReceived -= TwitchClientOnOnMessageReceived;
        });
        return Task.CompletedTask;
    }

    private async void TwitchClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var channelId = e.ChatMessage.RoomId;
        var text = e.ChatMessage.Message;
        var userName = e.ChatMessage.DisplayName;
        var userId = e.ChatMessage.UserId;
        var channelName = e.ChatMessage.Channel;

        if (text.StartsWith("!callAlisa", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Factory.StartNew(
                async () =>
                {
                    if (IsChannelApproved(channelId))
                    {
                        var message = $"""
                        Получен Алиса колл!

                        Отправитель: {userName}(id: {userId})
                        С канала: {channelName}

                        Текст:
                              {text}
                        
                        """;
                        await telegramClient.SendMessage(
                            1234835911,
                            message,
                            cancellationToken: _cancellationToken
                        );
                    }
                },
                _cancellationToken
            );
        }
    }

    private bool IsChannelApproved(string channelId)
    {
        if (TwitchFramedate.ApprovedChannels.Contains(channelId))
        {
            return true;
        }
        else
        {
            //проверяем наличие канала в бд
            using var dbContext = factory.CreateDbContext();
            var IsApproved = dbContext.TekkenChannels.Any(e =>
                e.TwitchId == channelId && e.FramedataStatus == TekkenFramedataStatus.Accepted
            );
            if (IsApproved)
            {
                TwitchFramedate.ApprovedChannels.Add(channelId);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
