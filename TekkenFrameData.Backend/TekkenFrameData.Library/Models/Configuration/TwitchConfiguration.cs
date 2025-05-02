using TekkenFrameData.Library.Models.ExternalServices.Twitch;

namespace TekkenFrameData.Library.Models.Configuration;

public partial class Configuration
{
    public required string ClientOAuthToken { get; set; }
    public required string ApiClientId { get; set; }
    public required string ApiClientSecret { get; set; }
}
