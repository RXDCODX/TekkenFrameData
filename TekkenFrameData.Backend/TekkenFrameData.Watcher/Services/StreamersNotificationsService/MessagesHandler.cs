using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Library.Models.Twitch;

namespace TekkenFrameData.Watcher.Services.StreamersNotificationsService;

public class MessagesHandler(IDbContextFactory<AppDbContext> contextFactory)
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
                TwitchId = e.TwitchId,
                IsFinished = false,
            })
            .ToList();

        await dbContext.GlobalNotificationMessage.AddAsync(msg);
        await dbContext.GlobalNotificatoinChannelsState.AddRangeAsync(twar);

        await dbContext.SaveChangesAsync();
    }
}
