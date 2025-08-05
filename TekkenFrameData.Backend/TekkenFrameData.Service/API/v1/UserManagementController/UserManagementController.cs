using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.Attributes;
using TekkenFrameData.Library.Exceptions;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Service.API.v1.UserManagementController;

[ApiController]
[Route("api/v1/[controller]")]
[RequirePermission(RolePermissions.ViewUsers)]
public class UserManagementController(
    IUserService userService,
    IRoleService roleService,
    IPermissionService permissionService,
    ILogger<UserManagementController> logger
) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await userService.GetAllUsersInfoAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserById(string userId)
    {
        try
        {
            var user = await userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userInfo = await userService.GetUserInfoAsync(user);
            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("users/{userId}/roles")]
    [RequirePermission(RolePermissions.ManageUserRoles)]
    public async Task<IActionResult> AddUserToRole(
        string userId,
        [FromBody] UserRoleRequest request
    )
    {
        try
        {
            var user = await userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!Roles.AllRoles.Contains(request.Role))
            {
                return BadRequest(new { message = "Invalid role" });
            }

            var result = await userService.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
            {
                return BadRequest(
                    new
                    {
                        message = "Failed to add user to role",
                        errors = result.Errors.Select(e => e.Description),
                    }
                );
            }

            return Ok(new { message = $"User added to role {request.Role} successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding user {UserId} to role {Role}", userId, request.Role);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("users/{userId}/roles")]
    [RequirePermission(RolePermissions.ManageUserRoles)]
    public async Task<IActionResult> RemoveUserFromRole(
        string userId,
        [FromBody] UserRoleRequest request
    )
    {
        try
        {
            var user = await userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Не позволяем удалять роль Owner у пользователя-владельца
            if (request.Role == Roles.Owner && user.Email == "owner@tekkenframedata.com")
            {
                return BadRequest(new { message = "Cannot remove Owner role from system owner" });
            }

            var result = await userService.RemoveFromRoleAsync(user, request.Role);
            if (!result.Succeeded)
            {
                return BadRequest(
                    new
                    {
                        message = "Failed to remove user from role",
                        errors = result.Errors.Select(e => e.Description),
                    }
                );
            }

            return Ok(new { message = $"User removed from role {request.Role} successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error removing user {UserId} from role {Role}",
                userId,
                request.Role
            );
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("users/{userId}/activate")]
    [RequirePermission(RolePermissions.ActivateUsers)]
    public async Task<IActionResult> ActivateUser(string userId)
    {
        try
        {
            var user = await userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsActive = true;
            var userManager = HttpContext.RequestServices.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(
                    new
                    {
                        message = "Failed to activate user",
                        errors = result.Errors.Select(e => e.Description),
                    }
                );
            }

            return Ok(new { message = "User activated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("users/{userId}/deactivate")]
    [RequirePermission(RolePermissions.DeactivateUsers)]
    public async Task<IActionResult> DeactivateUser(string userId)
    {
        try
        {
            var user = await userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Не позволяем деактивировать пользователя-владельца
            if (user.Email == "owner@tekkenframedata.com")
            {
                return BadRequest(new { message = "Cannot deactivate system owner" });
            }

            user.IsActive = false;
            var userManager = HttpContext.RequestServices.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(
                    new
                    {
                        message = "Failed to deactivate user",
                        errors = result.Errors.Select(e => e.Description),
                    }
                );
            }

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("roles")]
    [RequirePermission(RolePermissions.ViewRoles)]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            var roles = await roleService.GetAllRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all roles");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("permissions")]
    [RequirePermission(RolePermissions.ViewRoles)]
    public async Task<IActionResult> GetAllPermissions()
    {
        try
        {
            var permissions = await permissionService.GetAllAvailablePermissionsAsync();
            var categories = await permissionService.GetPermissionsByCategoryAsync();

            return Ok(new { Permissions = permissions, Categories = categories });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all permissions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("users/{userId}/permissions")]
    [RequirePermission(RolePermissions.ViewUsers)]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        try
        {
            var permissions = await permissionService.GetUserPermissionsAsync(userId);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
