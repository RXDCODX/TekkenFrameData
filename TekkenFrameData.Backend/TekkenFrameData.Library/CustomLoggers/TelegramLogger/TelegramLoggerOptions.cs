using Microsoft.Extensions.Logging;

namespace TekkenFrameData.Library.CustomLoggers.TelegramLogger;

public class TelegramLoggerOptions : TelegramLoggerOptionsBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public string BotToken { get; set; }
}
