using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.Commands;

public partial class Commands
{
    public async Task<Message> OnStartCommandReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        const string usage = """
                             Создатель бота - https://www.twitch.tv/pyrokxnezxz
                             /commands
                             """;

        return await botClient.SendMessage(
            message.Chat.Id,
            usage,
            cancellationToken: cancellationToken, parseMode: ParseMode.Html, replyParameters: message.MessageId);
    }
}