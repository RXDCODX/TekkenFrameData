using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.FrameData;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TekkenVictorina;

public class TekkenVictorinaLeaderbord(
    IDbContextFactory<AppDbContext> factory,
    IHostApplicationLifetime lifetime,
    ITwitchClient client
) : BackgroundService
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private static readonly ConcurrentDictionary<string, DateTime> cooldownDictionary = [];

    public async Task<TwitchLeaderboardUser[]> GetTopFiveGlobal()
    {
        await using var dbContext = await factory.CreateDbContextAsync(_cancellationToken);
        var topFiveGlobal = await dbContext
            .TwitchLeaderboardUsers.OrderByDescending(e => e.TekkenVictorinaWins)
            .AsNoTracking()
            .Take(5)
            .ToArrayAsync(cancellationToken: _cancellationToken);

        return topFiveGlobal;
    }

    public async Task<TwitchLeaderboardUser[]> GetTopFiveChannel(string channelId)
    {
        await using var dbContext = await factory.CreateDbContextAsync(_cancellationToken);
        var topFiveChannel = await dbContext
            .TwitchLeaderboardUsers.Where(e => e.ChannelId == channelId)
            .AsNoTracking()
            .OrderByDescending(e => e.TekkenVictorinaWins)
            .Take(5)
            .ToArrayAsync(cancellationToken: _cancellationToken);

        return topFiveChannel;
    }

    public async Task<(
        int globalOrder,
        int globalPoints,
        int channelOrder,
        int channelPoints
    )?> GetUserStat(string channelId, string twitchId)
    {
        await using var dbContext = await factory.CreateDbContextAsync(_cancellationToken);

        // Получаем данные пользователя
        var user = await dbContext
            .TwitchLeaderboardUsers.AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.TwitchId.Equals(twitchId) && u.ChannelId.Equals(channelId),
                cancellationToken: _cancellationToken
            );

        if (user == null)
        {
            return null;
        }

        // 2. Глобальный рейтинг через COUNT
        var globalOrder =
            await dbContext
                .TwitchLeaderboardUsers.AsNoTracking()
                .CountAsync(
                    u => u.TekkenVictorinaWins > user.TekkenVictorinaWins,
                    cancellationToken: _cancellationToken
                ) + 1;

        var globalPoints = await dbContext
            .TwitchLeaderboardUsers.Where(e => e.TwitchId == twitchId)
            .AsNoTracking()
            .SumAsync(e => e.TekkenVictorinaWins, cancellationToken: _cancellationToken);

        // Получаем рейтинг на канале
        var channelOrder = await dbContext
            .TwitchLeaderboardUsers.Where(u => u.ChannelId.Equals(channelId))
            .OrderByDescending(u => u.TekkenVictorinaWins)
            .AsNoTracking()
            .CountAsync(
                u => u.TekkenVictorinaWins > user.TekkenVictorinaWins,
                cancellationToken: _cancellationToken
            );

        var channelPoints = user.TekkenVictorinaWins;

        return (globalOrder, globalPoints, channelOrder, channelPoints);
    }

    public async Task AddOrUpdateUserLeaderBoard(
        string channelId,
        string userId,
        string displayName
    )
    {
        await using var dbContext = await factory.CreateDbContextAsync(_cancellationToken);
        var isExists = await dbContext.TwitchLeaderboardUsers.AnyAsync(
            e => e.TwitchId == userId && e.ChannelId == channelId,
            cancellationToken: _cancellationToken
        );

        if (isExists)
        {
            await dbContext
                .TwitchLeaderboardUsers.Where(e => e.TwitchId == userId)
                .ExecuteUpdateAsync(
                    e =>
                        e.SetProperty(
                            property => property.TekkenVictorinaWins,
                            value => value.TekkenVictorinaWins + 1
                        ),
                    cancellationToken: _cancellationToken
                );
        }
        else
        {
            var newUser = new TwitchLeaderboardUser()
            {
                DisplayName = displayName,
                ChannelId = channelId,
                TwitchId = userId,
                TekkenVictorinaWins = 1,
            };

            await dbContext.TwitchLeaderboardUsers.AddAsync(newUser, _cancellationToken);
        }

        await dbContext.SaveChangesAsync(_cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnChatCommandReceived += ClientOnOnMessageReceived;
        });
        return Task.CompletedTask;
    }

    private async void ClientOnOnMessageReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var twitchId = e.Command.ChatMessage.UserId!;
        var channel = e.Command.ChatMessage.Channel;
        var user = e.Command.ChatMessage.DisplayName;
        var channelId = e.Command.ChatMessage.RoomId;
        var command = e.Command.CommandText.ToLower();

        if (!string.IsNullOrWhiteSpace(channel) && CheckCooldownPass())
        {
            await Task.Factory.StartNew(
                async () =>
                {
                    switch (command)
                    {
                        case "tekken_leaders_global":
                            var globalLeaders = await GetTopFiveGlobal();
                            var globalText = string.Join(
                                " | ",
                                globalLeaders.Select(
                                    (leaderboardUser, index) =>
                                        $"[@{leaderboardUser.DisplayName}, Order: {index + 1}, Wins: {leaderboardUser.TekkenVictorinaWins}]"
                                )
                            );
                            client.SendMessage(channel, $"@{user}, Global leaders: " + globalText);
                            break;
                        case "tekken_leaders_channel":
                            var channelLeaders = await GetTopFiveChannel(channelId);
                            var channelText = string.Join(
                                " | ",
                                channelLeaders.Select(
                                    (leaderboardUser, index) =>
                                        $"[@{leaderboardUser.DisplayName}, Order: {index + 1}, Wins: {leaderboardUser.TekkenVictorinaWins}]"
                                )
                            );
                            client.SendMessage(
                                channel,
                                $"@{user}, Channel leaders: " + channelText
                            );
                            break;
                        case "tekken_me":
                            var userStats = await GetUserStat(channelId, twitchId);
                            if (userStats.HasValue)
                            {
                                client.SendMessage(
                                    channel,
                                    $"@{user}, твое место [Global: {userStats.Value.globalOrder} | {channel}: {userStats.Value.channelOrder}] "
                                        + $"с [Global: {userStats.Value.globalPoints} | {channel}: {userStats.Value.channelPoints}] побед!"
                                );
                            }
                            else
                            {
                                client.SendMessage(
                                    channel,
                                    $"@{user}, нету информации о твоих победах в теккен викторине."
                                );
                            }
                            break;
                    }
                },
                _cancellationToken
            );
        }

        bool CheckCooldownPass()
        {
            if (!cooldownDictionary.TryGetValue(channelId, out var value))
            {
                cooldownDictionary.AddOrUpdate(
                    channelId,
                    static _ => DateTime.Now,
                    static (_, __) => DateTime.Now
                );
                return true;
            }
            else
            {
                if (DateTimeOffset.Now - value >= TimeSpan.FromSeconds(30))
                {
                    cooldownDictionary.AddOrUpdate(
                        channelId,
                        static _ => DateTime.Now,
                        static (_, __) => DateTime.Now
                    );
                    return true;
                }

                return false;
            }
        }
    }
}
