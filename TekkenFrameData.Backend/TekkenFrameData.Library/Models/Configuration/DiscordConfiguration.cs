using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Library.Models.Configuration;

public partial class Configuration
{
    public required string DiscordToken { get; set; }
}
