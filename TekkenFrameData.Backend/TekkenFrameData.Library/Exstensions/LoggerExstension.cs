using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TekkenFrameData.Library.Exstensions;

public static class LoggerExstension
{
    public static ILogger LogException(this ILogger logger, Exception exception)
    {
        var stackTrace = exception.StackTrace;
        Exception? innerException = exception;

        while (innerException.InnerException != null)
        {
            innerException = innerException.InnerException;
        }

        logger.LogError("Error: {Message} # {StackTrace}", innerException.Message, stackTrace);
        return logger;
    }

    public static ILogger<T> LogException<T>(this ILogger<T> logger, Exception exception)
    {
        var stackTrace = exception.StackTrace;
        Exception? innerException = exception;

        var sb = new StringBuilder(exception.Message);

        while (innerException.InnerException != null)
        {
            innerException = innerException.InnerException;
            sb.Append(" + " + innerException.Message);
        }

        logger.LogError(
            "({LoggerName}): {Message} # {StackTrace}",
            nameof(T),
            sb.ToString(),
            stackTrace
        );

        return logger;
    }
}

public sealed class CustomFormatter : ConsoleFormatter
{
    private static readonly Dictionary<LogLevel, string> _logLevelAbbreviations = new()
    {
        [LogLevel.Trace] = "TRCE",
        [LogLevel.Debug] = "DBUG",
        [LogLevel.Information] = "INFO",
        [LogLevel.Warning] = "WARN",
        [LogLevel.Error] = "FAIL",
        [LogLevel.Critical] = "CRIT",
    };

    public CustomFormatter()
        : base("custom") { }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logLevel = GetLogLevelAbbreviation(logEntry.LogLevel);

        textWriter.WriteLine($"{timestamp}\t{logLevel}\t{message}");
    }

    private static string GetLogLevelAbbreviation(LogLevel level)
    {
        return _logLevelAbbreviations.TryGetValue(level, out var abbreviation)
            ? abbreviation
            : level.ToString().ToUpper();
    }
}
