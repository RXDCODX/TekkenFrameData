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
                    const int maxMessageLength = 4096; // Максимальная длина сообщения в Telegram
                    var command = string.Join(' ', splits.Skip(1));
                    var result = await command.Bash();

                    // Если результат помещается в одно сообщение
                    if (result.Length <= maxMessageLength)
                    {
                        return await client.SendMessage(
                            message.Chat,
                            result,
                            cancellationToken: token
                        );
                    }

                    // Разбиваем длинный результат на части
                    var messages = new List<string>();
                    for (int i = 0; i < result.Length; i += maxMessageLength)
                    {
                        var length = Math.Min(maxMessageLength, result.Length - i);
                        messages.Add(result.Substring(i, length));
                    }

                    // Отправляем части с задержкой
                    Message lastMessage = null!;
                    foreach (var part in messages)
                    {
                        await Task.Delay(1000, token); // Задержка между сообщениями
                        lastMessage = await client.SendMessage(
                            message.Chat,
                            part,
                            cancellationToken: token
                        );
                    }

                    return lastMessage;
                }
                catch (Exception e)
                {
                    return await client.SendMessage(
                        message.Chat,
                        $"Error: {e.Message}\nStack trace: {e.StackTrace}",
                        cancellationToken: token
                    );
                }
            }
        }

        return await client.SendMessage(
            message.Chat,
            "Invalid command parameters!",
            cancellationToken: token
        );
    }
}
