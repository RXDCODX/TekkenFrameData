namespace TekkenFrameData.Library.Models.Configuration;

public partial class Configuration
{
    public string BotToken { get; set; } = "";
    public long[] AdminIdsArray { get; set; } = [];
}
