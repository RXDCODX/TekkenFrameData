using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.Commands;

public partial class Commands
{
    public async Task<Message> OnHigemReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userName = message.Chat.Id;
        var split = message?.Text?.Split(' ');
        
        if (split is { Length: < 3 })
        {
            return await botClient.SendMessage(
                userName, 
                text: "Кривые параметры котисы!", 
                messageThreadId: message!.MessageThreadId,
                replyParameters: message.MessageId,
                cancellationToken: cancellationToken);
        }
        else
        {
            var channel = split?[1];
            var text = split?.Skip(2).ToList();

            try
            {
                client.JoinChannel(channel, true);
                client.SendMessage(channel, string.Join(' ', text));

                return await botClient.SendMessage(userName, $"Сообщение на канал {channel} отправленно!", 
                    messageThreadId: message!.MessageThreadId, 
                    replyParameters: message.MessageId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                return await botClient.SendMessage(userName, $"Ошибка отправления сообщения на канал {channel}! {ex.Message}", 
                    messageThreadId: message!.MessageThreadId,
                    replyParameters: message.MessageId,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
