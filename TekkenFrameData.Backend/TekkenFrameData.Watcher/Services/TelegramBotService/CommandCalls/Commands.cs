using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.SignalRInterfaces;
using TekkenFrameData.Watcher.Hubs;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.StreamersNotificationsService;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
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
    MessagesHandler globalNotifHandler
)
{
    public const string Template =
        "Не получилось получить комманды бота, сообщите об этой ошибке разработчику";

    [Description("список комманд бота")]
    public async Task<Message> OnCommandsCommandReceived(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken,
        bool isAdminCall = false
    )
    {
        var commands = typeof(Commands);
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
    private string[] GetCommandName(MethodInfo[] methods)
    {
        var commandNames = new string[methods.Length];
        const string template = "OnCommandReceived";

        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            var length = method.Name.Length - template.Length;
            var name = method.Name.Substring(2, length);

            var description = method.GetCustomAttribute<DescriptionAttribute>();
            var isDescription = !string.IsNullOrWhiteSpace(description?.Description);

            commandNames[i] = isDescription
                ? "/" + name.ToLower() + " - " + description!.Description
                : "/" + name.ToLower();
        }

        return commandNames;
    }
}
