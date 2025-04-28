namespace TekkenFrameData.Library.Models.Configuration;

public class TelegramConfiguration
{
    public static readonly string Configuration = "BotConfiguration";

    public string BotToken { get; set; } = "";
    public long[] AdminIdsArray { get; set; } = [];
}
