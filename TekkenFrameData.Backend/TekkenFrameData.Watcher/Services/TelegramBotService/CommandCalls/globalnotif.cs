using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description("Глобальный месседж для всех стримеров твича и в будущем дискорда")]
    public async Task<Message> OnGlobalNotifCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        var text = message.Text;
        if (text != null)
        {
            var splits = text.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            if (splits is { Length: > 1 })
            {
                var msg = string.Join(' ', splits.Skip(1));
                await globalNotifHandler.AddNewNotification(msg);

                return await client.SendMessage(
                    message.Chat,
                    "Глобальный нотиф отправлен!",
                    cancellationToken: token
                );
            }
        }

        return await client.SendMessage(
            message.Chat,
            "Не удалось отправить глобальное уведомление",
            cancellationToken: token
        );
    }
}
