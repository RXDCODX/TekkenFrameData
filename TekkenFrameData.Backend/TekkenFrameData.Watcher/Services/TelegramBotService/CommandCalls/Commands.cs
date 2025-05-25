using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.SignalRInterfaces;
using TekkenFrameData.Watcher.Hubs;
using TekkenFrameData.Watcher.Services.Framedata;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands(
    Tekken8FrameData frameData,
    ITwitchClient twitchClient,
    IHostApplicationLifetime lifetime,
    IDbContextFactory<AppDbContext> dbContextFactory,
    IHubContext<MainHub, IMainHubCommands> hubContext,
    ITwitchClient client
)
{
    public const string Template =
        "Не получилось получить комманды бота, сообщите об этой ошибке разработчику";

    internal static async Task<Message> OnUsageCommandReceived(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken
    )
    {
        var commands = typeof(Commands);

        var methods = commands.GetMethods(
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public
        );

        string usage;

        if (methods.Length > 0)
        {
            var names = GetCommandName(methods);
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

    private static string[] GetCommandName(MethodInfo[] methods)
    {
        var commandNames = new string[methods.Length];
        const string template = "OnCommandReceived";

        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            var length = method.Name.Length - template.Length;
            var name = method.Name.Substring(2, length);
            commandNames[i] = "/" + name;
        }

        return commandNames;
    }
}
