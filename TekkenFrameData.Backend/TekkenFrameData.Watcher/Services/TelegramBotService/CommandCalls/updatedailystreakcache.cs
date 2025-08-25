using TekkenFrameData.Watcher.Services.DailyStreak;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description("принудительно обновить кэш дейли стрика")]
    public async Task<Message> OnUpdateDailyStreakCacheCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        try
        {
            await dailyStreakService.UpdateChannels();

            var updatedChannels = DailyStreakService.ChannelsIdsWithWank;

            var messageText =
                $"✅ <b>Кэш дейли стрика обновлен!</b>\n\n"
                + $"<b>Текущее количество каналов в кэше:</b> {updatedChannels.Count}\n\n"
                + $"<b>Каналы:</b>\n"
                + string.Join("\n", updatedChannels.Select((id, index) => $"{index + 1}. {id}"));

            return await client.SendMessage(
                message.Chat,
                messageText,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: token
            );
        }
        catch (Exception ex)
        {
            return await client.SendMessage(
                message.Chat,
                $"❌ <b>Ошибка при обновлении кэша:</b>\n{ex.Message}",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: token
            );
        }
    }
}
