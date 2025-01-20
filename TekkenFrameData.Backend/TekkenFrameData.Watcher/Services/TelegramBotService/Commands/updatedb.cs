using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.Commands;

public partial class Commands
{
    public async Task<Message> OnUpdatedbReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        try
        {
            frameData.Init();
            const string usage = """
                                 База данных была принудительно обновленна!
                                 """;

            return await botClient.SendTextMessageAsync(
                message.Chat.Id,
                usage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            const string usage = """
                                 Не удалось обновить бд! {0} # {1}
                                 """;

            return await botClient.SendTextMessageAsync(
                message.Chat.Id,
                string.Format(usage, e.Message, e.StackTrace),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

    }
}