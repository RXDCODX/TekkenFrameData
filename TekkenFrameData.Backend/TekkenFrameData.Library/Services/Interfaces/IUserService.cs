using Microsoft.AspNetCore.Identity;
using TekkenFrameData.Library.Models.Identity;

namespace TekkenFrameData.Library.Services.Interfaces;

public interface IUserService
{
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    Task<bool> IsInRoleAsync(ApplicationUser user, string role);
    Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password);
    Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role);
    Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role);
    Task<bool> RoleExistsAsync(string role);
    Task<IdentityResult> CreateRoleAsync(string role);
    Task UpdateLastLoginAsync(ApplicationUser user);
    Task<List<string>> GetUserPermissionsAsync(ApplicationUser user);
    Task<bool> HasPermissionAsync(ApplicationUser user, string permission);
    Task<UserInfo> GetUserInfoAsync(ApplicationUser user);
    Task<List<UserInfo>> GetAllUsersInfoAsync();
} 