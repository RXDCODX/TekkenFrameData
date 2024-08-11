using System.Text;
using Microsoft.Extensions.Logging;
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

        logger.LogError("({LoggerName}): {Message} # {StackTrace}", nameof(T), sb.ToString(), stackTrace);

        return logger;
    }
}