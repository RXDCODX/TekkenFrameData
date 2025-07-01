using Microsoft.Extensions.Logging;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Interfaces;

namespace TekkenFrameData.Library.Exstensions;

public static class TwitchApiExstension
{
    public static async Task<bool> ValidateToken<T>(
        this ITwitchAPI api,
        ILogger<T> logger,
        string? token = null
    )
        where T : class
    {
        try
        {
            var response = await api.Auth.ValidateAccessTokenAsync(
                token ?? api.Settings.AccessToken
            );

            return response != null;
        }
        catch (Exception e)
            when (e.Message.Contains("invalid access token", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            return false;
        }
    }
}
