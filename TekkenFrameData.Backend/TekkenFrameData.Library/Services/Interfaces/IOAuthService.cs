using TekkenFrameData.Library.Models.Identity;

namespace TekkenFrameData.Library.Services.Interfaces;

public interface IOAuthService
{
    Task<ApplicationUser?> GetOrCreateUserFromTwitchAsync(string accessToken);
    Task<ApplicationUser?> GetOrCreateUserFromGoogleAsync(string accessToken);
    Task<string> GetTwitchAccessTokenAsync(string code);
    Task<string> GetGoogleAccessTokenAsync(string code);
    Task<TwitchUserInfo?> GetTwitchUserInfoAsync(string accessToken);
    Task<GoogleUserInfo?> GetGoogleUserInfoAsync(string accessToken);
}

public class TwitchUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string BroadcasterType { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
}

public class GoogleUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
} 