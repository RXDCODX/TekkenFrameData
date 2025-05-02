using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace TekkenFrameData.Library.CustomLoggers.TelegramLogger;

public static class TelegramLoggerProviderExtensions
{
    public static ILoggingBuilder AddTelegramLogger(
        this ILoggingBuilder loggerFactory,
        TelegramLoggerOptions options,
        Func<string, LogLevel, bool>? filter = default
    )
    {
        if (filter is null)
        {
            return loggerFactory;
        }

        var botClient = new TelegramBotClient(options.BotToken);
        loggerFactory.AddProvider(new TelegramLoggerProvider(botClient, options, filter));
        return loggerFactory;
    }

    public static ILoggingBuilder AddTelegramLogger(
        this ILoggingBuilder loggerFactory,
        Func<TelegramLoggerOptions> configure,
        Func<string, LogLevel, bool>? filter = default
    )
    {
        if (filter is null)
        {
            return loggerFactory;
        }

        var options = configure();
        return loggerFactory.AddTelegramLogger(options, filter);
    }

    public static ILoggingBuilder AddTelegramLogger(
        this ILoggingBuilder loggerFactory,
        TelegramLoggerOptionsBase options,
        Func<ITelegramBotClient> configure,
        Func<string, LogLevel, bool>? filter = default
    )
    {
        if (filter is null)
        {
            return loggerFactory;
        }

        var result = configure();

        loggerFactory.AddProvider(new TelegramLoggerProvider(result, options, filter));
        return loggerFactory;
    }
}
