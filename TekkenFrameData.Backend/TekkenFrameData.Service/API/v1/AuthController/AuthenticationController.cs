using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Service.API.v1.AuthController;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthenticationController(
    IUserService userService,
    IJwtService jwtService,
    IOAuthService oauthService,
    IConfiguration configuration,
    ILogger<AuthenticationController> logger
) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Account is deactivated" });
            }

            var userManager = HttpContext.RequestServices.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var isValidPassword = await userManager.CheckPasswordAsync(user, request.Password);

            if (!isValidPassword)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var roles = await userService.GetUserRolesAsync(user);
            var token = jwtService.GenerateJwtToken(user, roles);
            var refreshToken = jwtService.GenerateRefreshToken();

            await userService.UpdateLastLoginAsync(user);

            var response = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = [.. roles],
                },
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var existingUser = await userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true,
                IsActive = true,
            };

            var result = await userService.CreateUserAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(
                    new
                    {
                        message = "Failed to create user",
                        errors = result.Errors.Select(e => e.Description),
                    }
                );
            }

            // Добавляем роль User по умолчанию
            await userService.AddToRoleAsync(user, Roles.User);

            var roles = await userService.GetUserRolesAsync(user);
            var token = jwtService.GenerateJwtToken(user, roles);
            var refreshToken = jwtService.GenerateRefreshToken();

            var response = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = [.. roles],
                },
            };

            return CreatedAtAction(nameof(Login), response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("twitch-login")]
    public IActionResult TwitchLogin()
    {
        var clientId = configuration["OAuth:Twitch:ClientId"];
        var redirectUri = configuration["OAuth:Twitch:RedirectUri"];
        var scope = "user:read:email";

        var authUrl =
            $"https://id.twitch.tv/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri ?? string.Empty)}&response_type=code&scope={scope}";

        return Ok(new { authUrl });
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var clientId = configuration["OAuth:Google:ClientId"];
        var redirectUri = configuration["OAuth:Google:RedirectUri"];
        var scope = "email profile";

        var authUrl =
            $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri ?? string.Empty)}&response_type=code&scope={Uri.EscapeDataString(scope)}";

        return Ok(new { authUrl });
    }

    [HttpGet("twitch-callback")]
    public async Task<IActionResult> TwitchCallback([FromQuery] string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { message = "Authorization code is required" });
            }

            var accessToken = await oauthService.GetTwitchAccessTokenAsync(code);
            var user = await oauthService.GetOrCreateUserFromTwitchAsync(accessToken);

            if (user == null)
            {
                return BadRequest(new { message = "Failed to authenticate with Twitch" });
            }

            var roles = await userService.GetUserRolesAsync(user);
            var token = jwtService.GenerateJwtToken(user, roles);
            var refreshToken = jwtService.GenerateRefreshToken();

            await userService.UpdateLastLoginAsync(user);

            var response = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = [.. roles],
                },
            };

            // Redirect to frontend with token
            var frontendUrl = "http://localhost:3000/auth-callback";
            var redirectUrl =
                $"{frontendUrl}?token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(refreshToken)}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Twitch callback");
            return BadRequest(new { message = "Authentication failed" });
        }
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { message = "Authorization code is required" });
            }

            var accessToken = await oauthService.GetGoogleAccessTokenAsync(code);
            var user = await oauthService.GetOrCreateUserFromGoogleAsync(accessToken);

            if (user == null)
            {
                return BadRequest(new { message = "Failed to authenticate with Google" });
            }

            var roles = await userService.GetUserRolesAsync(user);
            var token = jwtService.GenerateJwtToken(user, roles);
            var refreshToken = jwtService.GenerateRefreshToken();

            await userService.UpdateLastLoginAsync(user);

            var response = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = [.. roles],
                },
            };

            // Redirect to frontend with token
            var frontendUrl = "http://localhost:3000/auth-callback";
            var redirectUrl =
                $"{frontendUrl}?token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(refreshToken)}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Google callback");
            return BadRequest(new { message = "Authentication failed" });
        }
    }

    [HttpPost("refresh")]
    public Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Здесь можно добавить логику для валидации refresh token
            // Пока что просто возвращаем ошибку
            return Task.FromResult<IActionResult>(
                BadRequest(new { message = "Refresh token functionality not implemented yet" })
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh");
            return Task.FromResult<IActionResult>(
                StatusCode(500, new { message = "Internal server error" })
            );
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var roles = await userService.GetUserRolesAsync(user);

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = [.. roles],
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // В JWT аутентификации logout обычно происходит на клиенте
        // путем удаления токена из localStorage
        return Ok(new { message = "Logged out successfully" });
    }
}
