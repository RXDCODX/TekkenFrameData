using TekkenFrameData.Library.Models.Identity;

namespace TekkenFrameData.Library.Services.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<bool> HasAnyPermissionAsync(string userId, params string[] permissions);
    Task<bool> HasAllPermissionsAsync(string userId, params string[] permissions);
    Task<List<string>> GetUserPermissionsAsync(string userId);
    Task<List<string>> GetRolePermissionsAsync(string roleName);
    Task<bool> ValidatePermissionAsync(string permission);
    Task<List<string>> GetAllAvailablePermissionsAsync();
    Task<Dictionary<string, List<string>>> GetPermissionsByCategoryAsync();
} 