using TekkenFrameData.Library.Models.ExternalServices.Twitch;

namespace TekkenFrameData.Library.Models.Configuration;

public partial class Configuration
{
    public string ClientOAuthToken { get; set; }
    public string ApiClientId { get; set; }
    public string ApiClientSecret { get; set; }
}
