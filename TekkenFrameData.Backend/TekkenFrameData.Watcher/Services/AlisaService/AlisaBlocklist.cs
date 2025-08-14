using System.Collections.Generic;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Library.Models.Twitch.AlisaCollab;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.AlisaService;

public class AlisaBlocklist(
    ITwitchClient client,
    IDbContextFactory<AppDbContext> factory,
    IHostApplicationLifetime lifetime
) : BackgroundService
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private static HashSet<string> _userIds = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += ClientOnOnMessageReceived;
            Task.Factory.StartNew(
                async () =>
                {
                    await using var dbContext = await factory.CreateDbContextAsync(
                        _cancellationToken
                    );
                    _userIds = await dbContext
                        .AlisaIgnoreTwitchUsers.AsNoTracking()
                        .Select(e => e.TwitchId)
                        .ToHashSetAsync(cancellationToken: _cancellationToken);
                },
                stoppingToken
            );
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            client.OnMessageReceived -= ClientOnOnMessageReceived;
        });

        return Task.CompletedTask;
    }

    private async void ClientOnOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var channelId = e.ChatMessage.RoomId;
        var channelName = e.ChatMessage.Channel;
        var userId = e.ChatMessage.UserId;
        var userName = e.ChatMessage.Username;
        var userMessage = e.ChatMessage.Message;
        if (
            _userIds.Contains(userId)
            && userMessage.Contains("alisaassistant", StringComparison.OrdinalIgnoreCase)
        )
        {
            await Task.Factory.StartNew(
                () =>
                {
                    if (IsChannelApproved(channelId))
                    {
                        client.SendMessage(
                            channelName,
                            "Уважаемый зритель @"
                                + userName
                                + ", Вы находитесь в чёрном списке Ассистента Алисы. Она не может ответить на ваше сообщение."
                        );
                    }
                },
                _cancellationToken
            );
        }
    }

    public async Task AddBlockerUser(User user)
    {
        await using var dbContext = await factory.CreateDbContextAsync(_cancellationToken);

        var isExists = dbContext.AlisaIgnoreTwitchUsers.Any(e => e.TwitchId == user.Id);

        if (!isExists)
        {
            await dbContext.AlisaIgnoreTwitchUsers.AddAsync(
                new AlisaIgnoreTwitchUser() { TwitchId = user.Id, TwitchName = user.DisplayName },
                _cancellationToken
            );

            await dbContext.SaveChangesAsync(_cancellationToken);

            _userIds = await dbContext
                .AlisaIgnoreTwitchUsers.AsNoTracking()
                .Select(e => e.TwitchId)
                .ToHashSetAsync(cancellationToken: _cancellationToken);
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
