using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Library.Models.Twitch;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.StreamersNotificationsService;

public class MessagesHandler(
    IDbContextFactory<AppDbContext> contextFactory,
    ITwitchClient client,
    ILogger<MessagesHandler> logger
)
{
    public async Task AddNewNotification(
        string message,
        GlobalNotificationsPlatforms platforms = GlobalNotificationsPlatforms.Twitch
    )
    {
        var msg = new GlobalNotificationMessage() { Message = message, Services = platforms };
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var coolChannels = await dbContext
            .TekkenChannels.Where(e => e.FramedataStatus == TekkenFramedataStatus.Accepted)
            .ToListAsync();

        var twar = coolChannels
            .Select(e => new TwitchNotificationChannelsState()
            {
                Message = msg,
                MessageId = msg.Id,
                ChannelId = e.Id,
                Channel = e,
                IsFinished = false,
            })
            .ToList();

        await dbContext.GlobalNotificationMessage.AddAsync(msg);
        await dbContext.GlobalNotificatoinChannelsState.AddRangeAsync(twar);

        await dbContext.SaveChangesAsync();
        await NotifOnlineStreamers();
    }

    public async Task NotifOnlineStreamers()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Получаем имена подключенных каналов
        var joinedChannelNames = client.JoinedChannels.Select(e => e.Channel).ToArray();

        var allChannels = await dbContext.TekkenChannels.ToListAsync();
        var activeChannels = allChannels
            .Where(c => joinedChannelNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
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
                client.SendMessage(
                    notificationState.Channel.Name,
                    notificationState.Message.Message
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
    }
}
