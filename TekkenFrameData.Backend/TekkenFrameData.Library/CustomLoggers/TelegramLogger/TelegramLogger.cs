﻿using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace TekkenFrameData.Library.CustomLoggers.TelegramLogger;

public class TelegramLogger : ILogger
{
    private readonly string _category;
    private readonly Func<string, LogLevel, bool> _filter;
    private readonly TelegramLoggerSender _messageQueue;
    private readonly TelegramLoggerOptions _options;
    private StringBuilder? _logBuilder;

    internal TelegramLogger(
        string category,
        TelegramLoggerSender messageQueue,
        TelegramLoggerOptions options,
        Func<string, LogLevel, bool> filter
    )
    {
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _messageQueue = messageQueue;
        _filter = filter ?? ((cat, logLevel) => true);
        _options = options;
    }

    public TelegramLogger(
        string category,
        ITelegramBotClient botClient,
        TelegramLoggerOptions options,
        Func<string, LogLevel, bool> filter
    )
        : this(category, new TelegramLoggerSender(botClient, options.ChatId), options, filter) { }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        ArgumentNullException.ThrowIfNull(formatter);

        if (exception != null)
        {
            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) && !string.IsNullOrWhiteSpace(message))
                SendMessage(logLevel, _category, eventId.Id, message, exception);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _filter(_category, logLevel);
    }

#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    public IDisposable BeginScope<TState>(TState state) => null;
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
#pragma warning restore CS8603 // Possible null reference return.

    private void SendMessage(
        LogLevel logLevel,
        string logName,
        int eventId,
        string message,
        Exception? exception
    )
    {
        var logBuilder = _logBuilder;
        _logBuilder = null;

        if ((int)logLevel < (int)_options.MinimumLevel)
            return;

        logBuilder ??= new StringBuilder();

        var logLevelString = GetLogLevelString(logLevel);

        logBuilder.AppendLine($"Log source: {_options.SourceName}");
        logBuilder.AppendLine("```");

        if (!string.IsNullOrEmpty(logLevelString))
            logBuilder.Append($"{logLevelString}: ");

        logBuilder.Append(logName);
        logBuilder.Append('[');
        logBuilder.Append(eventId);
        logBuilder.Append(']');
        logBuilder.AppendLine("```");

        if (!string.IsNullOrEmpty(message))
        {
            logBuilder.AppendLine();
            logBuilder.AppendLine(message);
        }

        if (exception != null)
        {
            logBuilder.AppendLine();
            logBuilder.AppendLine(exception.ToString());
        }

        if (logBuilder.Length == 0)
            return;

        if (logBuilder.Length > 4096)
        {
            logBuilder.Remove(4080, logBuilder.Length - 4080);
            logBuilder.Append("...");
            logBuilder.AppendLine();
            logBuilder.Append("...");
        }

        var content = logBuilder.ToString();

        _messageQueue.EnqueueMessage(content);

        logBuilder.Clear();
        if (logBuilder.Capacity > 1024)
            logBuilder.Capacity = 1024;
        _logBuilder = logBuilder;
    }

    private static string? GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => null,
        };
    }
}
