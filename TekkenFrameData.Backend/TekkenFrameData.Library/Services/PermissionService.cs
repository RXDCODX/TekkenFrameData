using Microsoft.AspNetCore.Identity;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Library.Services;

public class PermissionService(
    UserManager<ApplicationUser> userManager,
    ILogger<PermissionService> logger
) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var roles = await userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                if (Roles.HasPermission(role, permission))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error checking permission {Permission} for user {UserId}",
                permission,
                userId
            );
            return false;
        }
    }

    public async Task<bool> HasAnyPermissionAsync(string userId, params string[] permissions)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var roles = await userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                foreach (var permission in permissions)
                {
                    if (Roles.HasPermission(role, permission))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasAllPermissionsAsync(string userId, params string[] permissions)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var roles = await userManager.GetRolesAsync(user);
            var userPermissions = new HashSet<string>();

            foreach (var role in roles)
            {
                var rolePermissions = Roles.GetRolePermissions(role);
                foreach (var permission in rolePermissions)
                {
                    userPermissions.Add(permission);
                }
            }

            return permissions.All(permission => userPermissions.Contains(permission));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking all permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return [];
            }

            var roles = await userManager.GetRolesAsync(user);
            var permissions = new HashSet<string>();

            foreach (var role in roles)
            {
                var rolePermissions = Roles.GetRolePermissions(role);
                foreach (var permission in rolePermissions)
                {
                    permissions.Add(permission);
                }
            }

            return [.. permissions.OrderBy(p => p)];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return [];
        }
    }

    public Task<List<string>> GetRolePermissionsAsync(string roleName)
    {
        return Task.FromResult(Roles.GetRolePermissions(roleName));
    }

    public async Task<bool> ValidatePermissionAsync(string permission)
    {
        var allPermissions = await GetAllAvailablePermissionsAsync();
        return allPermissions.Contains(permission);
    }

    public Task<List<string>> GetAllAvailablePermissionsAsync()
    {
        var permissions = new HashSet<string>();

        foreach (var rolePermissions in RolePermissions.RolePermissionMap.Values)
        {
            foreach (var permission in rolePermissions)
            {
                permissions.Add(permission);
            }
        }

        return Task.FromResult(permissions.OrderBy(p => p).ToList());
    }

    public Task<Dictionary<string, List<string>>> GetPermissionsByCategoryAsync()
    {
        var categories = new Dictionary<string, List<string>>
        {
            ["User Management"] =
            [
                RolePermissions.ViewUsers,
                RolePermissions.CreateUsers,
                RolePermissions.EditUsers,
                RolePermissions.DeleteUsers,
                RolePermissions.ManageUserRoles,
                RolePermissions.ActivateUsers,
                RolePermissions.DeactivateUsers,
            ],
            ["Role Management"] =
            [
                RolePermissions.ViewRoles,
                RolePermissions.CreateRoles,
                RolePermissions.EditRoles,
                RolePermissions.DeleteRoles,
            ],
            ["Frame Data Management"] =
            [
                RolePermissions.ViewFrameData,
                RolePermissions.CreateFrameData,
                RolePermissions.EditFrameData,
                RolePermissions.DeleteFrameData,
                RolePermissions.ApproveFrameData,
            ],
            ["System Management"] =
            [
                RolePermissions.ViewSystemInfo,
                RolePermissions.ManageSystem,
                RolePermissions.ViewLogs,
                RolePermissions.ManageConfiguration,
            ],
            ["Content Management"] =
            [
                RolePermissions.ViewContent,
                RolePermissions.CreateContent,
                RolePermissions.EditContent,
                RolePermissions.DeleteContent,
                RolePermissions.PublishContent,
            ],
            ["Analytics"] = [RolePermissions.ViewAnalytics, RolePermissions.ExportAnalytics],
        };

        return Task.FromResult(categories);
    }
}
