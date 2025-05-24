using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.AspNetCore.Hosting;
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

    private readonly IHttpClientFactory _factory;

    public UpdateHandler(
        ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        Commands commands,
        Tekken8FrameData frameData,
        IHostApplicationLifetime lifetime,
        IDbContextFactory<AppDbContext> factory,
        IWebHostEnvironment environment,
        IHttpClientFactory factory1
    )
    {
        _botClient = botClient;
        _logger = logger;
        _commands = commands;
        this._factory = factory1;
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

        if (message.Type == MessageType.Text)
        {
            var chatId = message.Chat.Id;

            if (message.Text is not { } messageText)
            {
                return;
            }

            if (!messageText.StartsWith('/'))
            {
                return;
            }

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
                    "/joined" => _commands.OnJoinedCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/shutdown" => _commands.OnShutdownCommandReceived(
                        _botClient,
                        message,
                        cancellationToken
                    ),
                    "/cmd" => SendBashCommand(_factory, _botClient, message, cancellationToken),
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

            static async Task<Message> SendBashCommand(
                IHttpClientFactory factory,
                ITelegramBotClient telegramBotClient,
                Message message,
                CancellationToken cancellationToken
            )
            {
                if (message.Text == null)
                {
                    throw new NullReferenceException();
                }

                var splits = message.Text.Split(' ');
                var address = splits[1];
                var cmd = string.Join(' ', message.Text.Split(' ').Skip(2));
                using var client = factory.CreateClient();
                var msg = new HttpRequestMessage(HttpMethod.Post, address);
                msg.Content = new FormUrlEncodedContent(
                    new Dictionary<string, string>() { { "cmd", cmd } }
                );
                var result = await client.SendAsync(msg, cancellationToken);
                var resultContent = await result.Content.ReadAsStringAsync(cancellationToken);
                if (resultContent.Length > 4095)
                {
                    while (resultContent.Length > 450)
                    {
                        var split = resultContent.Take(4095).ToArray();
                        var newmessage = resultContent.Skip(450).ToArray();
                        resultContent = new string(newmessage);

                        await Task.Delay(3000, cancellationToken);

                        if (newmessage.Length <= 4095)
                        {
                            return await telegramBotClient.SendMessage(
                                message.Chat.Id,
                                new string(split),
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await telegramBotClient.SendMessage(
                                message.Chat.Id,
                                new string(split),
                                cancellationToken: cancellationToken
                            );
                        }
                    }
                }
                return await telegramBotClient.SendMessage(
                    message.Chat.Id,
                    await result.Content.ReadAsStringAsync(cancellationToken),
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
