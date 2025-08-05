using TekkenFrameData.Library.Models.Identity;

namespace TekkenFrameData.Library.Services.Interfaces;

public interface IRoleService
{
    Task<List<RoleInfo>> GetAllRolesAsync();
    Task<RoleInfo?> GetRoleByNameAsync(string roleName);
    Task<bool> RoleExistsAsync(string roleName);
    Task<bool> CreateRoleAsync(string roleName, string displayName, string description);
    Task<bool> UpdateRoleAsync(string roleName, string displayName, string description);
    Task<bool> DeleteRoleAsync(string roleName);
    Task<List<string>> GetRolePermissionsAsync(string roleName);
    Task<bool> AddPermissionToRoleAsync(string roleName, string permission);
    Task<bool> RemovePermissionFromRoleAsync(string roleName, string permission);
    Task<List<string>> GetAllPermissionsAsync();
    Task<bool> ValidatePermissionAsync(string permission);
} 