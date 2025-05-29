using TekkenFrameData.UpdateService.Services.TelegramBotService.Commands.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService.Commands;

public partial class Commands
{
    [Admin]
    public Task<Message> OnUpdateScriptsCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        try
        {
            ScriptsParser.UpdateScripts();
            return client.SendMessage(
                message.Chat,
                "Скрипты обновлены! Вот список скриптов:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + string.Join(
                        Environment.NewLine,
                        ScriptsParser.ScriptsDictionary.Select(e => e.Key + " - " + e.Value)
                    ),
                cancellationToken: token
            );
        }
        catch (Exception e)
        {
            return client.SendMessage(message.Chat, e.Message, cancellationToken: token);
        }
    }
}
