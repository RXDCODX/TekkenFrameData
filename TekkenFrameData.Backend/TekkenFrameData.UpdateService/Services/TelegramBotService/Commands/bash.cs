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
                    var caption = 512;
                    var result = await string.Join(' ', splits.Skip(1)).Bash();
                    if (result.Length > caption)
                    {
                        var split = result.Take(caption).ToArray();
                        result = new string(result.Skip(caption).ToArray());
                        var newMessage = new string(split);

                        await Task.Delay(3000, token);

                        if (result.Length > caption)
                        {
                            await client.SendMessage(
                                message.Chat,
                                newMessage,
                                cancellationToken: token
                            );
                        }
                        else
                        {
                            return await client.SendMessage(
                                message.Chat,
                                newMessage,
                                cancellationToken: token
                            );
                        }
                    }
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
