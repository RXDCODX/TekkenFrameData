using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description("показать список каналов подключенных к дейли стрику (wavu wank)")]
    public async Task<Message> OnDailyStreakChannelsCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);

        var connectedChannels = dbContext
            .WankWavuPlayers.AsNoTracking()
            .Select(e => new { e.TwitchId, e.CurrentNickname })
            .ToList();

        if (connectedChannels.Count < 1)
        {
            return await client.SendMessage(
                message.Chat,
                "Нет подключенных каналов для дейли стрика",
                cancellationToken: token
            );
        }

        var channelsList = connectedChannels
            .Select(
                (channel, index) =>
                    $"{index + 1}. {channel.CurrentNickname} (ID: {channel.TwitchId})"
            )
            .ToList();

        var messageText =
            $"📊 <b>Каналы подключенные к дейли стрику:</b>\n\n"
            + string.Join("\n", channelsList)
            + $"\n\n<b>Всего каналов:</b> {connectedChannels.Count}";

        return await client.SendMessage(
            message.Chat,
            messageText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: token
        );
    }
}
