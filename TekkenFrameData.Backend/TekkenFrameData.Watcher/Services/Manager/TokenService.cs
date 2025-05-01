using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.ExternalServices.Twitch;
using TwitchLib.Api.Interfaces;

namespace TekkenFrameData.Watcher.Services.Manager;

public class TokenService(
    ITwitchAPI api,
    ILogger<TokenService> logger,
    IDbContextFactory<AppDbContext> factory
)
{
    private TwitchTokenInfo? _tokenInfo;

    public TwitchTokenInfo? Token
    {
        get => _tokenInfo;
        internal set
        {
            if (value != null)
            {
                if (_tokenInfo?.Id != null)
                {
                    value.Id = _tokenInfo.Id;
                }

                _tokenInfo = value;
            }
        }
    }

    public async Task<TwitchTokenInfo?> GetTokenAsync(CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context.TwitchToken.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> RefreshTokenAsync(TwitchTokenInfo refreshToken)
    {
        try
        {
            await using AppDbContext dbContext = await factory.CreateDbContextAsync();

            var result = await api.Auth.RefreshAuthTokenAsync(
                refreshToken.RefreshToken,
                api.Settings.Secret,
                api.Settings.ClientId
            );

            var token = (await dbContext.TwitchToken.Where(e => true).ToListAsync())[0];

            token.AccessToken = result.AccessToken;
            token.ExpiresIn = TimeSpan.FromSeconds(result.ExpiresIn);
            token.RefreshToken = result.RefreshToken;
            token.WhenCreated = DateTimeOffset.Now.AddSeconds(-30);
            dbContext.TwitchToken.Update(token);

            refreshToken.AccessToken = result.AccessToken;
            refreshToken.ExpiresIn = TimeSpan.FromSeconds(result.ExpiresIn);
            refreshToken.RefreshToken = result.RefreshToken;
            refreshToken.WhenCreated = DateTimeOffset.Now.AddSeconds(-30);

            Token = refreshToken;

            await dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            return false;
        }
    }
}
