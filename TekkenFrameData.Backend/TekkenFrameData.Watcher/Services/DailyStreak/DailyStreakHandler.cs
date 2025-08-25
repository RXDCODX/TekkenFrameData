using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.DailyStreak;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.Contractor;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.DailyStreak;

public class DailyStreakHandler(
    ITwitchClient client,
    IHostApplicationLifetime lifetime,
    DailyStreakService dailyStreakService,
    ILogger<DailyStreakHandler> logger,
    IDbContextFactory<AppDbContext> factory
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnChatCommandReceived += ClientOnOnMessageReceived;
            client.OnChatCommandReceived += ClientOnKorobkaMessageReceived;
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            client.OnChatCommandReceived -= ClientOnOnMessageReceived;
            client.OnChatCommandReceived -= ClientOnKorobkaMessageReceived;
        });

        return Task.CompletedTask;
    }

    private async void ClientOnKorobkaMessageReceived(
        object? sender,
        OnChatCommandReceivedArgs onChatCommandReceivedArgs
    )
    {
        var channelName = onChatCommandReceivedArgs.Command.ChatMessage.Channel;
        var channelId = onChatCommandReceivedArgs.Command.ChatMessage.RoomId;
        var message = onChatCommandReceivedArgs.Command.ChatMessage.Message;
        var diplayname = onChatCommandReceivedArgs.Command.ChatMessage.DisplayName;
        var userId = onChatCommandReceivedArgs.Command.ChatMessage.UserId;

        // Убираем проверку на конкретный канал - теперь команда работает на любом канале
        await Task.Factory.StartNew(async () =>
        {
            if (
                IsChannelApproved(channelId)
                && ContractorHelper.IsAuthorizedUser(onChatCommandReceivedArgs)
            )
            {
                if (
                    onChatCommandReceivedArgs.Command.CommandText.Equals(
                        "wank",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    var count = onChatCommandReceivedArgs.Command.ArgumentsAsList.Count;

                    if (onChatCommandReceivedArgs.Command.ArgumentsAsList.Count == 0)
                    {
                        client.SendMessage(
                            channelName,
                            dailyStreakService.HasChannelProfile(channelId)
                                ? "Твой ваву профиль уже добавлен!"
                                : "Если хочешь добавить себе на канал твой персональный дневную статистику, напиши !wank <ссылка на твой wavu wank профиль>"
                        );
                    }
                    else if (count == 1 && !dailyStreakService.HasChannelProfile(channelId))
                    {
                        try
                        {
                            var link = onChatCommandReceivedArgs.Command.ArgumentsAsList[0]!;
                            if (DailyStreakSiteParser.TryParseWankWavuUrl(link, out var tekkenId))
                            {
                                await dailyStreakService.GetOrCreatePlayerAsync(
                                    channelId,
                                    tekkenId
                                );
                            }
                            else
                            {
                                client.SendMessage(
                                    channelName,
                                    "@"
                                        + diplayname
                                        + ", кривая ссылка на профиль, проверь что это ссылка типа wank.wavu.wiki/player/{tekkenId}!"
                                );
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            client.SendMessage(channelName, "@" + diplayname + ", " + e.Message);
                            return;
                        }

                        client.SendMessage(
                            channelName,
                            "@"
                                + diplayname
                                + ", добавил твой профиль! Теперь любой на твоем канале может писать !wl и увидит твою дневную статистику."
                        );

                        // Автоматически присоединяемся к каналу, если еще не присоединены
                        if (
                            !client.JoinedChannels.Any(t =>
                                t.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase)
                            )
                        )
                        {
                            client.JoinChannel(channelName);
                        }
                    }
                }
            }
        });
    }

    private async void ClientOnOnMessageReceived(
        object? sender,
        OnChatCommandReceivedArgs onChatCommandReceivedArgs
    )
    {
        var chatMessage = onChatCommandReceivedArgs.Command.ChatMessage;
        var command = onChatCommandReceivedArgs.Command.CommandText;
        var channelName = chatMessage.Channel;
        var channelId = chatMessage.RoomId;

        if (command.Equals("wl", StringComparison.OrdinalIgnoreCase))
        {
            // Проверяем, есть ли у владельца канала подключенный профиль
            if (dailyStreakService.HasChannelProfile(channelId))
            {
                // Автоматически присоединяемся к каналу, если еще не присоединены
                if (
                    !client.JoinedChannels.Any(t =>
                        t.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    client.JoinChannel(channelName);
                }

                await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var dailyStats = await dailyStreakService.GetPlayerDailyStatsAsync(
                            channelId
                        );

                        var resultMessage = FormatDailyStats(dailyStats, channelName);

                        if (resultMessage.Length > 500)
                        {
                            var messages = SplitIntoChunks(resultMessage, 450);

                            foreach (var mas in messages)
                            {
                                client.SendMessage(channelName, mas);
                            }
                        }
                        else
                        {
                            client.SendMessage(channelName, resultMessage);
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogException(exception);
                        client.SendMessage(channelName, "Бот пока на техобслуживании");
                    }
                });
            }
            else
            {
                // Информируем пользователей, что на этом канале нет подключенного профиля
                client.SendMessage(
                    channelName,
                    "На этом канале не подключен профиль wavu wank. Стример может подключить его командой !wank <ссылка на профиль>"
                );
            }
        }
    }

    private static string FormatDailyStats(WankWavuPlayerStats? stats, string channelDisplayName)
    {
        if (stats == null)
        {
            return "Не получилось собрать статистику";
        }

        if (stats.StatsChanges.Count == 0)
        {
            return "Нету статистики за сегодняшний день";
        }

        var charResult = stats.StatsChanges.Select(e =>
            e.Key
            + ": "
            + "TM "
            + e.Value.TotalMatchesCount
            + ", W "
            + e.Value.Wins
            + ", PTS "
            + (
                e.Value.PtsDifference > 0
                    ? "+" + e.Value.PtsDifference + " \ud83d\udfe2 "
                    : e.Value.PtsDifference + " \ud83d\udd34 "
            )
        );

        var result =
            $"@{channelDisplayName}, ✅ TM: {stats.TotalMatches}, W: {stats.Wins} ✅ {string.Join(
            "  ",
            charResult
        )}";

        return result;
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
            var isApproved = dbContext.TekkenChannels.Any(e =>
                e.TwitchId == channelId && e.FramedataStatus == TekkenFramedataStatus.Accepted
            );
            if (isApproved)
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

    private static string[] SplitIntoChunks(string text, int maxChunkSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        if (text.Length <= maxChunkSize)
        {
            return [text];
        }

        var chunks = new System.Collections.Generic.List<string>();
        var startIndex = 0;

        while (startIndex < text.Length)
        {
            // Определяем длину текущего куска
            var length = Math.Min(maxChunkSize, text.Length - startIndex);

            // Если это не последний кусок, ищем последний пробел для разбиения
            if (startIndex + length < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', startIndex + length, length);
                if (lastSpace > startIndex)
                {
                    length = lastSpace - startIndex;
                }
            }

            chunks.Add(text.Substring(startIndex, length).Trim());
            startIndex += length;
        }

        return [.. chunks];
    }
}
