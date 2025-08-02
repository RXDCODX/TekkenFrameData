using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TekkenFrameData.Watcher.Services.TelegramBotService;

public class UpdateHandler : IUpdateHandler
{
    private delegate Task TelegramUpdateDelegate(ITelegramBotClient client, Update update);

    private readonly ITelegramBotClient _botClient;
    private readonly Commands _commands;
    private readonly ILogger<UpdateHandler> _logger;
    public static long[] AdminLongs { get; private set; } = [];

    private TelegramUpdateDelegate _telegramDelegate = (client, update) => Task.CompletedTask;

    public UpdateHandler(
        ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        Commands commands,
        Tekken8FrameData frameData,
        IHostApplicationLifetime lifetime,
        IDbContextFactory<AppDbContext> factory,
        IWebHostEnvironment environment
    )
    {
        _botClient = botClient;
        _logger = logger;
        _commands = commands;
        AdminLongs = factory.CreateDbContext().Configuration.Single().AdminIdsArray;

        if (environment.IsProduction())
        {
            lifetime.ApplicationStarted.Register(() =>
            {
                _telegramDelegate += frameData.HandAlert;

                foreach (var admins in AdminLongs)
                {
                    botClient
                        .SendMessage(admins, "Приложение запустилось")
                        .GetAwaiter()
                        .GetResult();
                }
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                foreach (var admins in AdminLongs)
                {
                    botClient
                        .SendMessage(admins, "Приложение было отключено через graceful shutdown")
                        .GetAwaiter()
                        .GetResult();
                }
            });
        }
    }

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
            _logger.LogException(e);
        }

        var handler = update switch
        {
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { InlineQuery: not null } => InlinqQuery(),
            _ => UnknownUpdateHandlerAsync(update),
        };

        await handler;

        await _telegramDelegate.Invoke(_, update);
    }

    private static Task InlinqQuery()
    {
        return Task.CompletedTask;
    }

    private async void ResendMessage(Update update)
    {
        foreach (var id in AdminLongs)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    var messageId = update.Message!.MessageId;
                    var chatId = update.Message.Chat.Id;

                    if (update.Message.HasProtectedContent != true)
                    {
                        await _botClient.ForwardMessage(id, chatId, messageId);
                    }

                    break;
                case UpdateType.ChannelPost:
                    messageId = update.ChannelPost!.MessageId;
                    chatId = update.ChannelPost.Chat.Id;

                    if (update.ChannelPost.HasProtectedContent != true)
                    {
                        await _botClient.ForwardMessage(id, chatId, messageId);
                    }

                    //if (_environment.IsDevelopment())
                    //    _logger.LogCritical(update.ChannelPost.Text);
                    break;
            }
        }
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);

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
            var methods = _commands
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
                var commandWithoutSlash = command[1..];
                method = methodWithAliases.FirstOrDefault(
                    e =>
                    {
                        var aliasAttr = e?.GetCustomAttribute<AliasAttribute>();
                        if (aliasAttr?.MethodAliases.Contains(commandWithoutSlash) == true)
                        {
                            return true;
                        }

                        return false;
                    },
                    null
                );
            }

            if (method != null)
            {
                var isAdminMethod = method.GetCustomAttribute<AdminAttribute>() != null;
                var isIgnore = method.GetCustomAttribute<IgnoreAttribute>() != null;
                var isAdminUser = AdminLongs.Any(e => e == message.Chat.Id);

                if (isIgnore || (isAdminMethod && !isAdminUser))
                {
                    action = ErrorCommand(_botClient, message, cancellationToken);
                }
                else
                {
                    var parameters = new object[] { _botClient, message, cancellationToken };
                    if (methodName == "OnCommandsCommandReceived")
                    {
                        if (isAdminUser)
                        {
                            parameters = [_botClient, message, cancellationToken, true];
                        }
                        else
                        {
                            parameters = [_botClient, message, cancellationToken, false];
                        }
                    }

                    action = (Task<Message>?)method.Invoke(_commands, parameters);
                }
            }
            else
            {
                action = ErrorCommand(_botClient, message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command");
            action = ErrorCommand(_botClient, message, cancellationToken);
        }

        if (action != null)
        {
            var sentMessage = await action.ConfigureAwait(false);
            _logger.LogInformation(
                "The message was sent with id: {SentMessageId}",
                sentMessage.MessageId
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
            Commands.Template,
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

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken
    )
    {
        _logger.LogException(exception);
        return Task.CompletedTask;
    }
}
