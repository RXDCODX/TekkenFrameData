using System;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace TekkenFrameData.Watcher.TelegramLogger;

public static class TelegramLoggerProviderExtensions
{
    public static ILoggingBuilder AddTelegramLogger(
        this ILoggingBuilder loggerFactory,
        TelegramLoggerOptions options,
        Func<string, LogLevel, bool>? filter = default)
    {
        if(filter is null)
        {
            return loggerFactory;
        }

        var botClient = new TelegramBotClient(options.BotToken);
        loggerFactory?.AddProvider(new TelegramLoggerProvider(botClient, options, filter));
        return loggerFactory;
    }

    public static ILoggingBuilder AddTelegramLogger(
        this ILoggingBuilder loggerFactory,
        Action<TelegramLoggerOptions> configure,
        Func<string, LogLevel, bool>? filter = default)
    {
        if (filter is null)
        {
            return loggerFactory;
        }

        var options = new TelegramLoggerOptions();
        configure(options);
        return loggerFactory?.AddTelegramLogger(options, filter);
    }
}