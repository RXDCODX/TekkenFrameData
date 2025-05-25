using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    public async Task<Message> OnStopStreamCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        await Task.Factory.StartNew(
            () =>
            {
                hubContext.Clients.All.StopStream();
            },
            token
        );

        return await client.SendMessage(
            message.Chat,
            "Попробовал отключить стрим!",
            cancellationToken: token
        );
    }
}
