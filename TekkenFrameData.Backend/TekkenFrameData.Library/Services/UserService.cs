using Microsoft.AspNetCore.Identity;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Library.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager
) : IUserService
{
    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await userManager.FindByIdAsync(userId);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await userManager.FindByEmailAsync(email);
    }

    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        return await userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsInRoleAsync(ApplicationUser user, string role)
    {
        return await userManager.IsInRoleAsync(user, role);
    }

    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password)
    {
        return await userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
    {
        return await userManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role)
    {
        return await userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<bool> RoleExistsAsync(string role)
    {
        return await roleManager.RoleExistsAsync(role);
    }

    public async Task<IdentityResult> CreateRoleAsync(string role)
    {
        return await roleManager.CreateAsync(new IdentityRole(role));
    }

    public async Task UpdateLastLoginAsync(ApplicationUser user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
    }

    public async Task<List<string>> GetUserPermissionsAsync(ApplicationUser user)
    {
        var roles = await GetUserRolesAsync(user);
        var permissions = new HashSet<string>();

        foreach (var role in roles)
        {
            var rolePermissions = Roles.GetRolePermissions(role);
            foreach (var permission in rolePermissions)
            {
                permissions.Add(permission);
            }
        }

        return [.. permissions];
    }

    public async Task<bool> HasPermissionAsync(ApplicationUser user, string permission)
    {
        var permissions = await GetUserPermissionsAsync(user);
        return permissions.Contains(permission);
    }

    public async Task<UserInfo> GetUserInfoAsync(ApplicationUser user)
    {
        var roles = await GetUserRolesAsync(user);
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = [.. roles],
        };
    }

    public async Task<List<UserInfo>> GetAllUsersInfoAsync()
    {
        var users = userManager.Users.ToList();
        var userInfoList = new List<UserInfo>();

        foreach (var user in users)
        {
            var userInfo = await GetUserInfoAsync(user);
            userInfoList.Add(userInfo);
        }

        return userInfoList;
    }
}
