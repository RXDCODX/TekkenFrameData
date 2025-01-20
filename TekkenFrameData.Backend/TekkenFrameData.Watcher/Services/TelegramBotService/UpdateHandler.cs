using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Watcher.Services.Framedata;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace TekkenFrameData.Watcher.Services.TelegramBotService;

public class UpdateHandler : IUpdateHandler
{
    private delegate Task TelegramUpdateDelegate(ITelegramBotClient client, Update update);

    private readonly ITelegramBotClient _botClient;
    private readonly Commands.Commands _commands;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<UpdateHandler> _logger;
    private long[] AdminLongs { get; init; }

    private TelegramUpdateDelegate TelegramDelegate = (client, update) => Task.CompletedTask;

    public UpdateHandler(ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        Commands.Commands commands,
        IHostEnvironment environment,
        Tekken8FrameData _frameData)
    {
        _botClient = botClient;
        _logger = logger;
        _commands = commands;
        _environment = environment;
        AdminLongs = new long[]{ 402763435, 1917524881 };

        TelegramDelegate += _frameData.HandAlert;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        try
        {
            ResendMessage(update);
        }
        catch (Exception e)
        {
            _logger.LogWarning("�� ���� forward ��������. {0} # {1}", e.Message, e.StackTrace);
        }

        if (AdminLongs.Any(e => e.Equals(update.Message?.Chat.Id)))
        {
            var handler = update switch
            {
                //{ ChannelPost: {} channelPost } => BotOnChannelPost(channelPost, cancellationToken),
                { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
                { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update, cancellationToken)
            };

            await handler;

            await TelegramDelegate.Invoke(_, update);
        }
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private async void ResendMessage(Update update)
    {
        foreach (var id in AdminLongs)
            switch (update.Type)
            {
                case UpdateType.Message:
                    var messageId = update.Message!.MessageId;
                    var chatId = update.Message.Chat.Id;

                    if (update.Message.Chat.HasProtectedContent != true) await _botClient.ForwardMessageAsync(id, chatId, messageId);

                    break;
                case UpdateType.ChannelPost:
                    messageId = update.ChannelPost!.MessageId;
                    chatId = update.ChannelPost.Chat.Id;

                    if (update.ChannelPost.HasProtectedContent != true) await _botClient.ForwardMessageAsync(id, chatId, messageId);

                    //if (_environment.IsDevelopment())
                    //    _logger.LogCritical(update.ChannelPost.Text);
                    break;
            }
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (message.Type == MessageType.Text)
        {
            if (message.Text is not { } messageText)
                return;

            if (!messageText.StartsWith("/")) return;

            Task<Message> action;

            action = messageText.Split(' ')[0] switch
            {
                "/help" => _commands.OnHelpCommandReceived(_botClient, message, cancellationToken),
                "/framedate" => _commands.OnFramedataCommandReceived(_botClient, message, cancellationToken),
                "/fd" => _commands.OnFramedataCommandReceived(_botClient, message, cancellationToken),
                "/commands" => _commands.OnUsageCommandReceived(_botClient, message, cancellationToken),
                "/start" => _commands.OnStartCommandReceived(_botClient, message, cancellationToken),
                "/updatedb" => _commands.OnUpdatedbReceived(_botClient, message, cancellationToken),
                _ => ErrorCommand(_botClient, message, cancellationToken)
            };

            static Task<Message> ErrorCommand(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
            {
                return client.SendTextMessageAsync(message.Chat.Id, Commands.Commands.Template, cancellationToken: cancellationToken);
            }

            var sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        }
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results =
        {
            // displayed result
            new InlineQueryResultArticle(
                "1",
                "TgBots",
                new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQuery.Id,
            results,
            0,
            true,
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}