using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description("реконнект дискорда")]
    public async Task<Message> OnDiscordReconnectCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        const string messageText = "Отправлен реконнект дисокрда";
        await Task.Factory.StartNew(() => discordClient.ReconnectAsync(true), token);
        return await client.SendMessage(message.Chat, messageText, cancellationToken: token);
    }
}
