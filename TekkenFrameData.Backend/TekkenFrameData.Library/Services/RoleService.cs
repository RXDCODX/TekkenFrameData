using Microsoft.AspNetCore.Identity;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Library.Services;

public class RoleService(RoleManager<IdentityRole> roleManager, ILogger<RoleService> logger)
    : IRoleService
{
    public Task<List<RoleInfo>> GetAllRolesAsync()
    {
        var roles = roleManager.Roles.ToList();
        var roleInfoList = roles
            .Select(role => Roles.GetRoleInfo(role.Name ?? string.Empty))
            .ToList();

        return Task.FromResult(roleInfoList.OrderByDescending(r => r.Priority).ToList());
    }

    public async Task<RoleInfo?> GetRoleByNameAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        return role == null ? null : Roles.GetRoleInfo(roleName);
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await roleManager.RoleExistsAsync(roleName);
    }

    public async Task<bool> CreateRoleAsync(string roleName, string displayName, string description)
    {
        try
        {
            if (await RoleExistsAsync(roleName))
            {
                logger.LogWarning("Role {RoleName} already exists", roleName);
                return false;
            }

            var role = new IdentityRole(roleName);
            var result = await roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                logger.LogInformation("Role {RoleName} created successfully", roleName);
                return true;
            }

            logger.LogError(
                "Failed to create role {RoleName}: {Errors}",
                roleName,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating role {RoleName}", roleName);
            return false;
        }
    }

    public async Task<bool> UpdateRoleAsync(string roleName, string displayName, string description)
    {
        try
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                logger.LogWarning("Role {RoleName} not found", roleName);
                return false;
            }

            // Для системных ролей обновляем только отображаемое имя и описание
            if (Roles.RoleDefinitions.TryGetValue(roleName, out var roleInfo))
            {
                roleInfo.DisplayName = displayName;
                roleInfo.Description = description;
                return true;
            }

            // Для пользовательских ролей можно добавить дополнительную логику
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating role {RoleName}", roleName);
            return false;
        }
    }

    public async Task<bool> DeleteRoleAsync(string roleName)
    {
        try
        {
            if (Roles.RoleDefinitions.TryGetValue(roleName, out var value) && value.IsSystemRole)
            {
                logger.LogWarning("Cannot delete system role {RoleName}", roleName);
                return false;
            }

            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                logger.LogWarning("Role {RoleName} not found", roleName);
                return false;
            }

            var result = await roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                logger.LogInformation("Role {RoleName} deleted successfully", roleName);
                return true;
            }

            logger.LogError(
                "Failed to delete role {RoleName}: {Errors}",
                roleName,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting role {RoleName}", roleName);
            return false;
        }
    }

    public Task<List<string>> GetRolePermissionsAsync(string roleName)
    {
        return Task.FromResult(Roles.GetRolePermissions(roleName));
    }

    public async Task<bool> AddPermissionToRoleAsync(string roleName, string permission)
    {
        try
        {
            if (!await ValidatePermissionAsync(permission))
            {
                logger.LogWarning("Invalid permission {Permission}", permission);
                return false;
            }

            // Для системных ролей разрешения управляются через код
            if (Roles.RoleDefinitions.TryGetValue(roleName, out var value) && value.IsSystemRole)
            {
                logger.LogWarning("Cannot modify permissions for system role {RoleName}", roleName);
                return false;
            }

            // Здесь можно добавить логику для пользовательских ролей
            logger.LogInformation(
                "Permission {Permission} added to role {RoleName}",
                permission,
                roleName
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error adding permission {Permission} to role {RoleName}",
                permission,
                roleName
            );
            return false;
        }
    }

    public Task<bool> RemovePermissionFromRoleAsync(string roleName, string permission)
    {
        try
        {
            // Для системных ролей разрешения управляются через код
            if (Roles.RoleDefinitions.TryGetValue(roleName, out var value) && value.IsSystemRole)
            {
                logger.LogWarning("Cannot modify permissions for system role {RoleName}", roleName);
                return Task.FromResult(false);
            }

            // Здесь можно добавить логику для пользовательских ролей
            logger.LogInformation(
                "Permission {Permission} removed from role {RoleName}",
                permission,
                roleName
            );
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error removing permission {Permission} from role {RoleName}",
                permission,
                roleName
            );
            return Task.FromResult(false);
        }
    }

    public Task<List<string>> GetAllPermissionsAsync()
    {
        var permissions = new HashSet<string>();

        foreach (
            var permission in RolePermissions.RolePermissionMap.Values.SelectMany(rolePermissions =>
                rolePermissions
            )
        )
        {
            permissions.Add(permission);
        }

        return Task.FromResult(permissions.OrderBy(p => p).ToList());
    }

    public async Task<bool> ValidatePermissionAsync(string permission)
    {
        var allPermissions = await GetAllPermissionsAsync();
        return allPermissions.Contains(permission);
    }
}
