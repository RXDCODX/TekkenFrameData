using System.Collections.Generic;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class TwitchRulesCommand(
    ITwitchClient client,
    IHostApplicationLifetime life,
    IDbContextFactory<AppDbContext> factory
) : BackgroundService
{
    private static readonly HashSet<string> ApprovedChannels = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        life.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += ClientOnOnMessageReceived;
        });

        life.ApplicationStopping.Register(() =>
        {
            client.OnMessageReceived -= ClientOnOnMessageReceived;
        });

        return Task.CompletedTask;
    }

    private async void ClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var channelName = e.ChatMessage.Channel;
        var channelId = e.ChatMessage.RoomId;
        var message = e.ChatMessage.Message;

        if (IsChannelApproved(channelId))
        {
            if (message.StartsWith("!twrules"))
            {
                await Task.Factory.StartNew(() =>
                {
                    const string rules = "https://safety.twitch.tv/s?language=ru";
                    var splits = message.Split(' ');

                    if (splits is { Length: 2 } && splits[1].StartsWith('@'))
                    {
                        client.SendMessage(
                            channelName,
                            $"{splits[1]}, тебе следует прочитать правила - {rules}"
                        );
                    }
                    else if (splits is { Length: 1 })
                    {
                        client.SendMessage(
                            channelName,
                            $"@{e.ChatMessage.DisplayName}, держи правила твича - {rules}"
                        );
                    }
                });
            }
        }
    }

    private bool IsChannelApproved(string channelId)
    {
        if (ApprovedChannels.Contains(channelId))
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
                ApprovedChannels.Add(channelId);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
