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

    public async Task<WankWavuPlayerStats?> GetPlayerDailyStatsAsync(string twitchId)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            // Получаем игрока из базы данных
            var player =
                await context.WankWavuPlayers.FirstOrDefaultAsync(p => p.TwitchId == twitchId)
                ?? throw new Exception($"Игрок с Twitch ID {twitchId} не найден в базе данных");

            // Получаем статистику с сайта
            var stats = await parser.GetDailyStats(player);

            return stats;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            throw new Exception($"Ошибка получения статистики для игрока {twitchId}: {ex.Message}");
        }
    }

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
