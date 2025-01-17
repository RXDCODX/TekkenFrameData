using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TwitchLib.Communication.Interfaces;

namespace tekkenfd.Services.TelegramBotService.Commands;

public partial class Commands
{
    public async Task<Message> OnHigemReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userName = message.Chat.Id;
        var split = message?.Text?.Split(' ');

        if (split is { Length: < 3 })
        {
            return await botClient.SendTextMessageAsync(userName, "Кривые параметры котисы!", message!.MessageThreadId, replyToMessageId: message.MessageId,
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

                return await botClient.SendTextMessageAsync(userName, $"Сообщение на канал {channel} отправленно!", message!.MessageThreadId, replyToMessageId: message.MessageId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(userName, $"Ошибка отправления сообщения на канал {channel}! {ex.Message}", message!.MessageThreadId, replyToMessageId: message.MessageId,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
