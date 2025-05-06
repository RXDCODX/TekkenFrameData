using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    public async Task<Message> OnShutdownCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        await Task.Factory.StartNew(lifetime.StopApplication, token);

        return await client.SendMessage(
            message.Chat,
            "Выключил приложение!",
            cancellationToken: token
        );
    }
}
