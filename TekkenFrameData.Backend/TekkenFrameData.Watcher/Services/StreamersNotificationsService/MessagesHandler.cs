using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Library.Models.Twitch;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.StreamersNotificationsService;

public class MessagesHandler(IDbContextFactory<AppDbContext> contextFactory, ITwitchClient client)
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
        var joinedChannels = client.JoinedChannels.Select(e => e.Channel).ToArray();

        var streamNotifsNotFinished = dbContext
            .GlobalNotificatoinChannelsState.Include(twitchNotificationChannelsState =>
                twitchNotificationChannelsState.Channel
            )
            .Where(e => joinedChannels.Contains(e.Channel.Name) && e.IsFinished == false)
            .ToList();

        var messagesId = streamNotifsNotFinished
            .DistinctBy(e => e.MessageId)
            .Select(e => e.MessageId)
            .ToArray();
        var messages = await dbContext
            .GlobalNotificationMessage.Where(e =>
                messagesId.Contains(e.Id) && e.Services == GlobalNotificationsPlatforms.Twitch
            )
            .ToArrayAsync();

        foreach (var twitchNotificationChannelsState in streamNotifsNotFinished)
        {
            client.SendMessage(
                twitchNotificationChannelsState.Channel.Name,
                messages.First(e => e.Id == twitchNotificationChannelsState.MessageId).Message
            );
            twitchNotificationChannelsState.IsFinished = true;
            dbContext.Entry(twitchNotificationChannelsState).State = EntityState.Modified;
        }

        await dbContext.SaveChangesAsync();
    }
}
