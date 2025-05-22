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
        var result = await _rebootServiceWorker.UpdateService();
        return await client.SendMessage(message.Chat, result, cancellationToken: token);
    }
}
