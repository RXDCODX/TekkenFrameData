using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Library.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute(string permission)
    : AuthorizeAttribute,
        IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService =
            context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var hasPermission = await permissionService.HasPermissionAsync(userId, permission);

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireAnyPermissionAttribute(params string[] permissions)
    : AuthorizeAttribute,
        IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService =
            context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var hasPermission = await permissionService.HasAnyPermissionAsync(userId, permissions);

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireAllPermissionsAttribute(params string[] permissions)
    : AuthorizeAttribute,
        IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService =
            context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var hasPermission = await permissionService.HasAllPermissionsAsync(userId, permissions);

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
