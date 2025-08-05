using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Library.Services;

public class OAuthService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<OAuthService> logger,
    HttpClient httpClient
) : IOAuthService
{
    public async Task<ApplicationUser?> GetOrCreateUserFromTwitchAsync(string accessToken)
    {
        try
        {
            var twitchUser = await GetTwitchUserInfoAsync(accessToken);
            if (twitchUser == null)
            {
                return null;
            }

            // Try to find existing user by Twitch ID
            var existingUser = await userManager.FindByLoginAsync("Twitch", twitchUser.Id);
            if (existingUser != null)
            {
                return existingUser;
            }

            // Try to find by email
            if (!string.IsNullOrEmpty(twitchUser.Email))
            {
                existingUser = await userManager.FindByEmailAsync(twitchUser.Email);
                if (existingUser != null)
                {
                    // Link existing account to Twitch
                    var result = await userManager.AddLoginAsync(
                        existingUser,
                        new UserLoginInfo("Twitch", twitchUser.Id, "Twitch")
                    );
                    if (result.Succeeded)
                    {
                        return existingUser;
                    }
                }
            }

            // Create new user
            var newUser = new ApplicationUser
            {
                UserName = $"twitch_{twitchUser.Login}",
                Email = twitchUser.Email,
                FirstName = twitchUser.DisplayName,
                LastName = string.Empty,
                EmailConfirmed = !string.IsNullOrEmpty(twitchUser.Email),
                IsActive = true,
            };

            var createResult = await userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to create Twitch user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description))
                );
                return null;
            }

            // Add Twitch login
            var loginResult = await userManager.AddLoginAsync(
                newUser,
                new UserLoginInfo("Twitch", twitchUser.Id, "Twitch")
            );
            if (!loginResult.Succeeded)
            {
                logger.LogError(
                    "Failed to add Twitch login: {Errors}",
                    string.Join(", ", loginResult.Errors.Select(e => e.Description))
                );
                return null;
            }

            // Add default role
            await userManager.AddToRoleAsync(newUser, Roles.User);

            return newUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating/getting Twitch user");
            return null;
        }
    }

    public async Task<ApplicationUser?> GetOrCreateUserFromGoogleAsync(string accessToken)
    {
        try
        {
            var googleUser = await GetGoogleUserInfoAsync(accessToken);
            if (googleUser == null)
            {
                return null;
            }

            // Try to find existing user by Google ID
            var existingUser = await userManager.FindByLoginAsync("Google", googleUser.Id);
            if (existingUser != null)
            {
                return existingUser;
            }

            // Try to find by email
            if (!string.IsNullOrEmpty(googleUser.Email))
            {
                existingUser = await userManager.FindByEmailAsync(googleUser.Email);
                if (existingUser != null)
                {
                    // Link existing account to Google
                    var result = await userManager.AddLoginAsync(
                        existingUser,
                        new UserLoginInfo("Google", googleUser.Id, "Google")
                    );
                    if (result.Succeeded)
                    {
                        return existingUser;
                    }
                }
            }

            // Create new user
            var newUser = new ApplicationUser
            {
                UserName = $"google_{googleUser.Id}",
                Email = googleUser.Email,
                FirstName = googleUser.GivenName,
                LastName = googleUser.FamilyName,
                EmailConfirmed = googleUser.EmailVerified,
                IsActive = true,
            };

            var createResult = await userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to create Google user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description))
                );
                return null;
            }

            // Add Google login
            var loginResult = await userManager.AddLoginAsync(
                newUser,
                new UserLoginInfo("Google", googleUser.Id, "Google")
            );
            if (!loginResult.Succeeded)
            {
                logger.LogError(
                    "Failed to add Google login: {Errors}",
                    string.Join(", ", loginResult.Errors.Select(e => e.Description))
                );
                return null;
            }

            // Add default role
            await userManager.AddToRoleAsync(newUser, Roles.User);

            return newUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating/getting Google user");
            return null;
        }
    }

    public async Task<string> GetTwitchAccessTokenAsync(string code)
    {
        var twitchConfig = configuration.GetSection("OAuth:Twitch");
        var clientId = twitchConfig["ClientId"];
        var clientSecret = twitchConfig["ClientSecret"];
        var redirectUri = twitchConfig["RedirectUri"];

        var tokenRequest = new
        {
            client_id = clientId,
            client_secret = clientSecret,
            code,
            grant_type = "authorization_code",
            redirect_uri = redirectUri,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(tokenRequest),
            Encoding.UTF8,
            "application/json"
        );
        var response = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return tokenResponse.GetProperty("access_token").GetString() ?? string.Empty;
        }

        throw new Exception($"Failed to get Twitch access token: {response.StatusCode}");
    }

    public async Task<string> GetGoogleAccessTokenAsync(string code)
    {
        var googleConfig = configuration.GetSection("OAuth:Google");
        var clientId = googleConfig["ClientId"];
        var clientSecret = googleConfig["ClientSecret"];
        var redirectUri = googleConfig["RedirectUri"];

        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = clientId ?? string.Empty,
            ["client_secret"] = clientSecret ?? string.Empty,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri ?? string.Empty,
        };

        var content = new FormUrlEncodedContent(tokenRequest);
        var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return tokenResponse.GetProperty("access_token").GetString() ?? string.Empty;
        }

        throw new Exception($"Failed to get Google access token: {response.StatusCode}");
    }

    public async Task<TwitchUserInfo?> GetTwitchUserInfoAsync(string accessToken)
    {
        try
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );
            httpClient.DefaultRequestHeaders.Add(
                "Client-Id",
                configuration["OAuth:Twitch:ClientId"]
            );

            var response = await httpClient.GetAsync("https://api.twitch.tv/helix/users");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userResponse = JsonSerializer.Deserialize<JsonElement>(content);
                var users = userResponse.GetProperty("data");

                if (users.GetArrayLength() > 0)
                {
                    var user = users[0];
                    return new TwitchUserInfo
                    {
                        Id = user.GetProperty("id").GetString() ?? string.Empty,
                        Login = user.GetProperty("login").GetString() ?? string.Empty,
                        DisplayName = user.GetProperty("display_name").GetString() ?? string.Empty,
                        Email = user.GetProperty("email").GetString() ?? string.Empty,
                        ProfileImageUrl =
                            user.GetProperty("profile_image_url").GetString() ?? string.Empty,
                        BroadcasterType =
                            user.GetProperty("broadcaster_type").GetString() ?? string.Empty,
                        UserType = user.GetProperty("user_type").GetString() ?? string.Empty,
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting Twitch user info");
            return null;
        }
    }

    public async Task<GoogleUserInfo?> GetGoogleUserInfoAsync(string accessToken)
    {
        try
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );

            var response = await httpClient.GetAsync(
                "https://www.googleapis.com/oauth2/v2/userinfo"
            );
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content);
                return userInfo;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting Google user info");
            return null;
        }
    }
}
