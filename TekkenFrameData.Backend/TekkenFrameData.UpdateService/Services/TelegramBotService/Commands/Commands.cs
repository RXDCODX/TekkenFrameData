using System.Reflection;
using TekkenFrameData.UpdateService.Services.TelegramBotService.Commands.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService.Commands;

public partial class Commands
{
    public const string Template =
        "Не получилось получить комманды бота, сообщите об этой ошибке разработчику";

    public async Task<Message> OnCommandsCommandReceived(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken,
        bool isAdminCall = false
    )
    {
        Type commands = typeof(Commands);
        MethodInfo[] methods;

        if (isAdminCall)
        {
            methods = commands.GetMethods(
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public
            );
        }
        else
        {
            methods = commands
                .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttribute<AdminAttribute>() == null)
                .ToArray();
        }

        string usage;

        if (methods.Any())
        {
            var names = GetCommandName(methods);
            if (isAdminCall)
            {
                names.AddRange(ScriptsParser.ScriptsDictionary.Keys);
            }
            usage = string.Join(Environment.NewLine, names);
        }
        else
        {
            usage = Template;
        }

        return await botClient.SendMessage(
            message.Chat.Id,
            usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken
        );
    }

    [Ignore]
    private List<string> GetCommandName(MethodInfo[] methods)
    {
        var commandNames = new List<string>(methods.Length);
        const string template = "OnCommandReceived";

        for (var i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            var length = method.Name.Length - template.Length;
            var name = method.Name.Substring(2, length);
            commandNames[i] = "/" + name.ToLower();
        }

        return commandNames;
    }
}
