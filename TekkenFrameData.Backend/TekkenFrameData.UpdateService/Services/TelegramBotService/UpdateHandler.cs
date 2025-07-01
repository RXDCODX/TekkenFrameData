using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.UpdateService.Services.TelegramBotService.Commands.Attribute;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using TwitchLib.Communication.Interfaces;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService;

public class UpdateHandler(
    ITelegramBotClient botClient,
    ILogger<UpdateHandler> logger,
    Commands.Commands commands,
    IDbContextFactory<AppDbContext> dbContextFactory
) : IUpdateHandler
{
    public delegate Task TelegramUpdateDelegate(ITelegramBotClient client, Update update);

    public readonly long[] AdminsIds = dbContextFactory
        .CreateDbContext()
        .Configuration.Single()
        .AdminIdsArray;

    public async Task HandleUpdateAsync(
        ITelegramBotClient _,
        Update update,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ResendMessage(update);
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }

        var handler = update switch
        {
            //{ ChannelPost: {} channelPost } => BotOnChannelPost(channelPost, cancellationToken),
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(
                inlineQuery,
                cancellationToken
            ),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken),
        };

        await handler;
        await TelegramUpdate.Invoke(_, update);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken
    )
    {
        logger.LogException(exception);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString(),
        };

        logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    public event TelegramUpdateDelegate TelegramUpdate = (client, update) => Task.CompletedTask;

    private async void ResendMessage(Update update)
    {
        foreach (var id in AdminsIds)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    var messageId = update.Message!.MessageId;
                    var chatId = update.Message.Chat.Id;

                    if (update.Message.HasProtectedContent != true)
                    {
                        await botClient.ForwardMessage(id, chatId, messageId);
                    }

                    break;
                case UpdateType.ChannelPost:
                    messageId = update.ChannelPost!.MessageId;
                    chatId = update.ChannelPost.Chat.Id;

                    if (update.ChannelPost.HasProtectedContent != true)
                    {
                        await botClient.ForwardMessage(id, chatId, messageId);
                    }

                    //if (_environment.IsDevelopment())
                    //    _logger.LogCritical(update.ChannelPost.Text);
                    break;
            }
        }
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (
            message.Type != MessageType.Text
            || message.Text is not { } messageText
            || !messageText.StartsWith('/')
        )
        {
            return;
        }

        Task<Message>? action;

        try
        {
            var command = messageText.Split(' ')[0];
            var methodName = GetMethodName(command);
            var methods = commands
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            var method = methods.FirstOrDefault(e =>
                e.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase)
            );
            if (method == null)
            {
                var methodWithAliases = methods.Where(e =>
                    e.GetCustomAttribute<AliasAttribute>() != null
                );
                var commandWithoutSlash = command.Trim()[1..];
                method = methodWithAliases.FirstOrDefault(
                    e =>
                    {
                        var aliasAttr = e?.GetCustomAttribute<AliasAttribute>();
                        return aliasAttr?.MethodAliases.Contains(commandWithoutSlash) == true;
                    },
                    null
                );

                if (method == null)
                {
                    var scriptName = ScriptsParser.ScriptsDictionary[commandWithoutSlash];
                    if (!string.IsNullOrWhiteSpace(scriptName))
                    {
                        var scriptPath = Path.Combine(ScriptsParser.ScriptsFolder, scriptName);
                        await ExecBashScript(scriptPath, message.Chat, cancellationToken);
                    }
                }
            }

            if (method != null)
            {
                var isAdminMethod = method.GetCustomAttribute<AdminAttribute>() != null;
                var isIgnore = method.GetCustomAttribute<IgnoreAttribute>() != null;
                var isAdminUser = AdminsIds.Any(e => e == message.Chat.Id);

                if (isIgnore || (isAdminMethod && !isAdminUser))
                {
                    action = ErrorCommand(botClient, message, cancellationToken);
                }
                else
                {
                    var parameters = new object[] { botClient, message, cancellationToken };
                    if (methodName == "OnCommandsCommandReceived")
                    {
                        parameters = isAdminUser
                            ? [botClient, message, cancellationToken, true]
                            : [botClient, message, cancellationToken, false];
                    }

                    action = (Task<Message>?)method.Invoke(commands, parameters);
                }
            }
            else
            {
                action = ErrorCommand(botClient, message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling command");
            action = ErrorCommand(botClient, message, cancellationToken);
        }

        if (action != null)
        {
            var sentMessage = await action.ConfigureAwait(false);
            logger.LogInformation(
                "The message was sent with id: {SentMessageId}",
                sentMessage.MessageId
            );
        }
    }

    private async Task<Message> ExecBashScript(
        string scriptPath,
        Chat chat,
        CancellationToken token
    )
    {
        try
        {
            var caption = 4095;
            var result = await ("sh " + scriptPath).Bash();
            while (result.Length > caption)
            {
                var split = result.Take(caption).ToArray();
                result = new string(result.AsSpan()[caption..]);
                var newMessage = new string(split);

                await Task.Delay(3000, token);

                if (result.Length > caption)
                {
                    await botClient.SendMessage(chat, newMessage, cancellationToken: token);
                }
                else
                {
                    break;
                }
            }
            return await botClient.SendMessage(chat, result, cancellationToken: token);
        }
        catch (Exception e)
        {
            return await botClient.SendMessage(
                chat,
                e.Message + "#" + e.StackTrace,
                cancellationToken: token
            );
        }
    }

    private static Task<Message>? ErrorCommand(
        ITelegramBotClient client,
        Message message,
        CancellationToken cancellationToken
    )
    {
        return client.SendMessage(
            message.Chat.Id,
            Commands.Commands.Template,
            cancellationToken: cancellationToken
        );
    }

    private static string GetMethodName(string command)
    {
        return string.Concat(
            "On",
            command[1..].First().ToString().ToUpper(),
            command.AsSpan(2),
            "CommandReceived"
        );
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(
        InlineQuery inlineQuery,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Received inline query from: {InlineQueryFromId}",
            inlineQuery.From.Id
        );

        InlineQueryResult[] results =
        [
            // displayed result
            new InlineQueryResultArticle("1", "TgBots", new InputTextMessageContent("hello")),
        ];

        await botClient.AnswerInlineQuery(
            inlineQuery.Id,
            results,
            0,
            true,
            cancellationToken: cancellationToken
        );
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
