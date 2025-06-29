﻿using TekkenFrameData.Library.Models.Twitch;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.StreamersNotificationsService;

public class StreamersNotificationWorker(
    IDbContextFactory<AppDbContext> contextFactory,
    IHostApplicationLifetime lifetime,
    TwitchFramedataChannelsEvents events,
    ITwitchClient twitchClient
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

    private Task EventsOnChannelConnected(
        object? sender,
        TwitchFramedataChannelsEvents.ChannelConnectedEventArgs args
    )
    {
        return Task.Factory.StartNew(async () =>
        {
            var channelsTwitchIds = args.Streams.Select(e => e.UserId).ToArray();
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var streamNotifsNotFinished = dbContext
                .GlobalNotificatoinChannelsState.Include(twitchNotificationChannelsState =>
                    twitchNotificationChannelsState.Channel
                )
                .Where(e => channelsTwitchIds.Contains(e.Channel.TwitchId) && e.IsFinished == false)
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
                twitchClient.SendMessage(
                    twitchNotificationChannelsState.Channel.Name,
                    messages.First(e => e.Id == twitchNotificationChannelsState.MessageId).Message
                );
                twitchNotificationChannelsState.IsFinished = true;
                dbContext.Entry(twitchNotificationChannelsState).State = EntityState.Modified;
            }

            await dbContext.SaveChangesAsync();
        });
    }
}
