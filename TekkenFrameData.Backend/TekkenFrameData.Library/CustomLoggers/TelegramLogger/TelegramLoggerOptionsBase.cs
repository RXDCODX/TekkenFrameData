using Microsoft.Extensions.Logging;

namespace TekkenFrameData.Library.CustomLoggers.TelegramLogger;

public class TelegramLoggerOptionsBase
{
    /// <summary>
    ///     Unique identifier for the target chat or username of the target channel (in the format @channelusername)
    /// </summary>
    public long[] ChatId { get; set; }

    /// <summary>
    ///     The name of the source of logs
    /// </summary>
    public string SourceName { get; set; }

    public LogLevel MinimumLevel { get; set; } = LogLevel.None;
}
