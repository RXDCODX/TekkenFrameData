using TekkenFrameData.Library.Exstensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    public async Task<Message> OnRebootCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        try
        {
            await Task.Factory.StartNew(rebootServiceWorker.UpdateService, token);
            return await client.SendMessage(
                message.Chat,
                "Запустил перезапуск!",
                cancellationToken: token
            );
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return await client.SendMessage(
                message.Chat,
                ex.Message + "#" + ex.StackTrace,
                cancellationToken: token
            );
        }
    }
}
