using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.Models.Identity;

namespace TekkenFrameData.Library.Services;

public class RoleInitializerService(
    IServiceProvider serviceProvider,
    ILogger<RoleInitializerService> logger
)
{
    public async Task InitializeRolesAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Создаем роли
        foreach (var role in Roles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    logger.LogInformation("Role {Role} created successfully", role);
                }
                else
                {
                    logger.LogError(
                        "Failed to create role {Role}: {Errors}",
                        role,
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
                }
            }
        }

        // Создаем пользователя-владельца, если его нет
        var ownerEmail = "owner@tekkenframedata.com";
        var ownerUser = await userManager.FindByEmailAsync(ownerEmail);

        if (ownerUser == null)
        {
            ownerUser = new ApplicationUser
            {
                UserName = ownerEmail,
                Email = ownerEmail,
                FirstName = "System",
                LastName = "Owner",
                EmailConfirmed = true,
                IsActive = true,
            };

            var result = await userManager.CreateAsync(ownerUser, "Owner123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(ownerUser, Roles.Owner);
                logger.LogInformation("Owner user created successfully");
            }
            else
            {
                logger.LogError(
                    "Failed to create owner user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }
        }
        else
        {
            // Убеждаемся, что владелец имеет роль Owner
            if (!await userManager.IsInRoleAsync(ownerUser, Roles.Owner))
            {
                await userManager.AddToRoleAsync(ownerUser, Roles.Owner);
                logger.LogInformation("Owner role assigned to existing user");
            }
        }
    }
}
