using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;
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
    private long[] AdminLongs { get; init; }

    private TelegramUpdateDelegate _telegramDelegate = (client, update) => Task.CompletedTask;

    public UpdateHandler(
        ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        Commands commands,
        Tekken8FrameData frameData,
        IHostApplicationLifetime lifetime,
        IDbContextFactory<AppDbContext> factory
    )
    {
        _botClient = botClient;
        _logger = logger;
        _commands = commands;
        AdminLongs = factory.CreateDbContext().Configuration.Single().AdminIdsArray;

        lifetime.ApplicationStarted.Register(() =>
        {
            _telegramDelegate += frameData.HandAlert;
        });
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
            _ => UnknownUpdateHandlerAsync(update),
        };

        await handler;

        await _telegramDelegate.Invoke(_, update);
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
                        await _botClient.ForwardMessage(id, chatId, messageId);

                    break;
                case UpdateType.ChannelPost:
                    messageId = update.ChannelPost!.MessageId;
                    chatId = update.ChannelPost.Chat.Id;

                    if (update.ChannelPost.HasProtectedContent != true)
                        await _botClient.ForwardMessage(id, chatId, messageId);

                    //if (_environment.IsDevelopment())
                    //    _logger.LogCritical(update.ChannelPost.Text);
                    break;
            }
        }
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (message.Type == MessageType.Text)
        {
            var chatId = message.Chat.Id;

            if (message.Text is not { } messageText)
                return;

            if (!messageText.StartsWith('/'))
                return;

            Task<Message> action;

            if (AdminLongs.Contains(chatId))
            {
                action = messageText.Split(' ')[0] switch
                {
                    "/help" => _commands.OnHelpCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/framedate" => _commands.OnFramedataCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/fd" => _commands.OnFramedataCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/commands" => Commands.OnUsageCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/start" => _commands.OnStartCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/scrup" => _commands.OnScrupCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    _ => ErrorCommand(_botClient, message, cancellationToken),
                };
            }
            else
            {
                action = messageText.Split(' ')[0] switch
                {
                    "/help" => _commands.OnHelpCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/framedate" => _commands.OnFramedataCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/fd" => _commands.OnFramedataCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/commands" => Commands.OnUsageCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/start" => _commands.OnStartCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    _ => ErrorCommand(_botClient, message, cancellationToken),
                };
            }

            static Task<Message> ErrorCommand(
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

            var sentMessage = await action;
            _logger.LogInformation(
                "The message was sent with id: {SentMessageId}",
                sentMessage.MessageId
            );
        }
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
