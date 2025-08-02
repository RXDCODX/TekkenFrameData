using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.StreamersNotificationsService;

public class StreamersNotificationWorker(
    IDbContextFactory<AppDbContext> contextFactory,
    IHostApplicationLifetime lifetime,
    TwitchFramedataChannelsEvents events,
    ITwitchClient twitchClient,
    ILogger<StreamersNotificationWorker> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            events.ChannelConnected += EventsOnChannelConnected;
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            events.ChannelConnected -= EventsOnChannelConnected;
        });

        return Task.CompletedTask;
    }

    private async Task EventsOnChannelConnected(
        object? sender,
        TwitchFramedataChannelsEvents.ChannelConnectedEventArgs args
    )
    {
        await Task.Factory.StartNew(async () =>
        {
            var channelsTwitchIds = args.Streams.Select(e => e.UserId).ToArray();
            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var allChannels = await dbContext.TekkenChannels.ToListAsync();
            var activeChannels = allChannels
                .Where(c => channelsTwitchIds.Contains(c.TwitchId))
                .ToList();

            if (activeChannels.Count == 0)
            {
                return; // Нет активных каналов для уведомлений
            }

            // Получаем незавершенные уведомления для активных каналов
            var streamNotifsNotFinished = await dbContext
                .GlobalNotificatoinChannelsState.Include(s => s.Channel)
                .Include(s => s.Message)
                .Where(e =>
                    activeChannels.Select(c => c.Id).Contains(e.ChannelId) && e.IsFinished == false
                )
                .ToListAsync();

            if (streamNotifsNotFinished.Count == 0)
            {
                return; // Нет незавершенных уведомлений
            }

            foreach (var notificationState in streamNotifsNotFinished)
            {
                try
                {
                    // Отправляем сообщение
                    twitchClient.SendMessage(
                        notificationState.Channel.Name,
                        "@"
                            + notificationState.Channel.Name
                            + ", "
                            + notificationState.Message.Message
                    );

                    // Помечаем как завершенное
                    notificationState.IsFinished = true;
                    dbContext.Update(notificationState);
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    logger.LogException(ex);
                }
            }

            await dbContext.SaveChangesAsync();
        });
    }
}
