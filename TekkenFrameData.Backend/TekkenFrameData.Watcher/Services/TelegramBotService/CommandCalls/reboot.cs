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
            Console.WriteLine("zxc");
            var result = await rebootServiceWorker.UpdateService();
            Console.WriteLine("zxc123");
            return await client.SendMessage(message.Chat, result, cancellationToken: token);
        }
        catch (Exception e)
        {
            return await client.SendMessage(message.Chat, e.Message, cancellationToken: token);
        }
    }
}
