using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TekkenFrameData.Watcher.Exstensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.Commands;

public partial class Commands
{
    public async Task<Message> OnHigemReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userName = message.Chat.Id;
        var split = message.Text?.Split(' ');
        string text;

        if (split is { Length: < 3 })
        {
            text = "Кривые параметры котисы!";
        }
        else
        {
            var channel = split?[1];
            var splits = split?.Skip(2).ToList();

            if (splits?.Count > 0)
            {
                client.JoinChannel(channel, true);
                client.SendMessage(channel, string.Join(' ', splits));

                text = $"Сообщение на канал {channel} отправленно!";
            }
            else
            {
                text = $"Ошибка отправления сообщения на канал {channel}!";

            }
        }

        return await botClient.SendMessage(userName, text, messageThreadId: message!.MessageThreadId, replyParameters: message.MessageId,
            cancellationToken: cancellationToken);
    }
}
