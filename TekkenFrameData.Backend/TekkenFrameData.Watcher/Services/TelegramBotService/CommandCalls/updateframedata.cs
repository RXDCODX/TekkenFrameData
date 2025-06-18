using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description("scrupframedate")]
    public async Task<Message> OnScrupCommandReceived(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken
    )
    {
        if (!frameData.ParsingActive)
        {
            await Task.Factory.StartNew(
                () => frameData.StartScrupFrameData(message.Chat),
                cancellationToken
            );

            return await botClient.SendMessage(
                message.Chat,
                "Начал парсинг",
                cancellationToken: cancellationToken
            );
        }

        return await botClient.SendMessage(
            message.Chat,
            "Парсинг уже идет!",
            cancellationToken: cancellationToken
        );
    }
}
