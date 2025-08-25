using TekkenFrameData.Watcher.Services.DailyStreak;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description("показать текущее состояние кэша дейли стрика (ChannelsIdsWithWank)")]
    public async Task<Message> OnDailyStreakCacheCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        var cachedChannels = DailyStreakService.ChannelsIdsWithWank;

        if (!cachedChannels.Any())
        {
            return await client.SendMessage(
                message.Chat,
                "📊 <b>Кэш дейли стрика пуст</b>\n\nНет каналов в кэше ChannelsIdsWithWank",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: token
            );
        }

        var channelsList = cachedChannels
            .Select((channelId, index) => $"{index + 1}. {channelId}")
            .ToList();

        var messageText =
            $"📊 <b>Текущее состояние кэша дейли стрика:</b>\n\n"
            + $"<b>Каналы в ChannelsIdsWithWank:</b>\n"
            + string.Join("\n", channelsList)
            + $"\n\n<b>Всего каналов в кэше:</b> {cachedChannels.Count}";

        return await client.SendMessage(
            message.Chat,
            messageText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: token
        );
    }
}
