using System.Collections.Frozen;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.DailyStreak;
using TekkenFrameData.Library.Models.DailyStreak.structures;

namespace TekkenFrameData.Watcher.Services.DailyStreak;

public class DailyStreakService(
    DailyStreakSiteParser parser,
    IDbContextFactory<AppDbContext> contextFactory,
    IHostApplicationLifetime lifetime,
    ILogger<DailyStreakService> logger
) : BackgroundService
{
    public static FrozenSet<string> ChannelsIdsWithWank { get; private set; } = [];

    public async Task<WankWavuPlayer> GetOrCreatePlayerAsync(string twitchId, string tekkenId)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var player = await context.WankWavuPlayers.FirstOrDefaultAsync(p =>
                p.TwitchId == twitchId
            );

            if (player == null)
            {
                // Создаем нового игрока
                var tekkenIdObj = TekkenId.Parse(tekkenId);
                var uri = new Uri(
                    $"https://wank.wavu.wiki/player/{tekkenIdObj.ToStringWithoutDashes()}"
                );

                player = await parser.GetWankWavuPlayerAsync(twitchId, uri);

                context.WankWavuPlayers.Add(player);
                await context.SaveChangesAsync();
                
                // Обновляем кэш после добавления нового игрока
                await UpdateChannels();
            }

            return player;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            throw new Exception($"Ошибка получения/создания игрока {twitchId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Получает статистику для конкретного канала по Twitch ID канала
    /// </summary>
    public async Task<WankWavuPlayerStats?> GetPlayerDailyStatsAsync(string channelTwitchId)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            
            // Получаем игрока из базы данных по Twitch ID канала
            var player =
                await context.WankWavuPlayers.FirstOrDefaultAsync(p => p.TwitchId == channelTwitchId)
                ?? throw new Exception($"Игрок с Twitch ID канала {channelTwitchId} не найден в базе данных");

            // Получаем статистику с сайта
            var stats = await parser.GetDailyStats(player);

            return stats;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            throw new Exception($"Ошибка получения статистики для канала {channelTwitchId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет, есть ли у канала подключенный профиль wavu wank
    /// </summary>
    public bool HasChannelProfile(string channelTwitchId)
    {
        return ChannelsIdsWithWank.Contains(channelTwitchId);
    }

    public async Task UpdateChannels()
    {
        await using var dbcontext = await contextFactory.CreateDbContextAsync();
        ChannelsIdsWithWank = dbcontext
            .WankWavuPlayers.AsNoTracking()
            .Select(e => e.TwitchId)
            .ToFrozenSet();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            Task.Factory.StartNew(UpdateChannels, stoppingToken);
        });

        return Task.CompletedTask;
    }
}
