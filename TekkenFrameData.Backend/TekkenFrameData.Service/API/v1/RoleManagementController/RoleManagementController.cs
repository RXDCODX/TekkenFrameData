using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.Attributes;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Service.API.v1.RoleManagementController;

[ApiController]
[Route("api/v1/[controller]")]
[RequirePermission(RolePermissions.ViewRoles)]
public class RoleManagementController(
    IRoleService roleService,
    IPermissionService permissionService,
    ILogger<RoleManagementController> logger
) : ControllerBase
{
    [HttpGet("roles")]
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

    [HttpGet("roles/{roleName}")]
    public async Task<IActionResult> GetRoleByName(string roleName)
    {
        try
        {
            var role = await roleService.GetRoleByNameAsync(roleName);
            return role == null ? NotFound(new { message = "Role not found" }) : Ok(role);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting role {RoleName}", roleName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("roles")]
    [RequirePermission(RolePermissions.CreateRoles)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Role name is required" });
            }

            var success = await roleService.CreateRoleAsync(
                request.Name,
                request.DisplayName,
                request.Description
            );
            if (!success)
            {
                return BadRequest(new { message = "Failed to create role" });
            }

            return Ok(new { message = $"Role '{request.Name}' created successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating role {RoleName}", request.Name);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("roles/{roleName}")]
    [RequirePermission(RolePermissions.EditRoles)]
    public async Task<IActionResult> UpdateRole(
        string roleName,
        [FromBody] UpdateRoleRequest request
    )
    {
        try
        {
            var success = await roleService.UpdateRoleAsync(
                roleName,
                request.DisplayName,
                request.Description
            );
            if (!success)
            {
                return BadRequest(new { message = "Failed to update role" });
            }

            return Ok(new { message = $"Role '{roleName}' updated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating role {RoleName}", roleName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("roles/{roleName}")]
    [RequirePermission(RolePermissions.DeleteRoles)]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        try
        {
            var success = await roleService.DeleteRoleAsync(roleName);
            if (!success)
            {
                return BadRequest(new { message = "Failed to delete role" });
            }

            return Ok(new { message = $"Role '{roleName}' deleted successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting role {RoleName}", roleName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("roles/{roleName}/permissions")]
    public async Task<IActionResult> GetRolePermissions(string roleName)
    {
        try
        {
            var permissions = await roleService.GetRolePermissionsAsync(roleName);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting permissions for role {RoleName}", roleName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("roles/{roleName}/permissions")]
    [RequirePermission(RolePermissions.EditRoles)]
    public async Task<IActionResult> AddPermissionToRole(
        string roleName,
        [FromBody] string permission
    )
    {
        try
        {
            var success = await roleService.AddPermissionToRoleAsync(roleName, permission);
            if (!success)
            {
                return BadRequest(new { message = "Failed to add permission to role" });
            }

            return Ok(
                new
                {
                    message = $"Permission '{permission}' added to role '{roleName}' successfully",
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error adding permission {Permission} to role {RoleName}",
                permission,
                roleName
            );
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("roles/{roleName}/permissions")]
    [RequirePermission(RolePermissions.EditRoles)]
    public async Task<IActionResult> RemovePermissionFromRole(
        string roleName,
        [FromBody] string permission
    )
    {
        try
        {
            var success = await roleService.RemovePermissionFromRoleAsync(roleName, permission);
            if (!success)
            {
                return BadRequest(new { message = "Failed to remove permission from role" });
            }

            return Ok(
                new
                {
                    message = $"Permission '{permission}' removed from role '{roleName}' successfully",
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error removing permission {Permission} from role {RoleName}",
                permission,
                roleName
            );
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("permissions")]
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
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
