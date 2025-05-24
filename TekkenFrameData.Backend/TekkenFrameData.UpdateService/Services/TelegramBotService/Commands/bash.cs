using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.UpdateService.Services.TelegramBotService.Commands.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService.Commands;

public partial class Commands
{
    [Admin]
    public async Task<Message> OnBashCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        var text = message.Text;
        var splits = text?.Split(' ');

        if (splits is { Length: > 1 })
        {
            if (text != null)
            {
                try
                {
                    var result = await text.Bash();
                    return await client.SendMessage(message.Chat, result, cancellationToken: token);
                }
                catch (Exception e)
                {
                    return await client.SendMessage(
                        message.Chat,
                        e.Message + "#" + e.StackTrace,
                        cancellationToken: token
                    );
                }
            }
        }

        return await client.SendMessage(
            message.Chat,
            "Кривые параметры!",
            cancellationToken: token
        );
    }
}
