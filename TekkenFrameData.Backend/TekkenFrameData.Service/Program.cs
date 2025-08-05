using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Identity;
using TekkenFrameData.Library.Services;
using TekkenFrameData.Library.Services.Interfaces;

namespace TekkenFrameData.Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;

        // Добавляем контроллеры
        services.AddControllers();

        builder.Services.AddOpenApi();

        // Настройка Identity
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Настройка JWT аутентификации
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(
            jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured")
        );

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        // Настройка авторизации
        services
            .AddAuthorizationBuilder()
            // Настройка авторизации
            .AddPolicy("RequireOwnerRole", policy => policy.RequireRole(Roles.Owner))
            // Настройка авторизации
            .AddPolicy(
                "RequireAdminRole",
                policy => policy.RequireRole(Roles.Owner, Roles.Administrator)
            )
            // Настройка авторизации
            .AddPolicy(
                "RequireModeratorRole",
                policy => policy.RequireRole(Roles.Owner, Roles.Administrator, Roles.Moderator)
            )
            // Настройка авторизации
            .AddPolicy(
                "RequireEditorRole",
                policy =>
                    policy.RequireRole(
                        Roles.Owner,
                        Roles.Administrator,
                        Roles.Moderator,
                        Roles.Editor
                    )
            );

        services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            var environmentVariable = Environment.GetEnvironmentVariable(
                "ASPNETCORE_DOCKER_LAUNCH"
            );
            if (environmentVariable == "TRUE")
            {
                optionsBuilder
                    .UseNpgsql(builder.Configuration.GetConnectionString("docker_pg"))
                    .EnableThreadSafetyChecks();
            }
            else
            {
                optionsBuilder
                    .UseNpgsql(builder.Configuration.GetConnectionString("local_pg"))
                    .EnableThreadSafetyChecks();
            }
        });

        // Регистрация сервисов
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<RoleInitializerService>();

        // Регистрация HttpClient для OAuth
        services.AddHttpClient();

        services.AddLogging();

        var app = builder.Build();
        app.Migrate();

        // Инициализация ролей
        using (var scope = app.Services.CreateScope())
        {
            var roleInitializer =
                scope.ServiceProvider.GetRequiredService<RoleInitializerService>();
            await roleInitializer.InitializeRolesAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        // Добавляем middleware для аутентификации и авторизации
        app.UseAuthentication();
        app.UseAuthorization();

        // Настройка маршрутов
        app.MapControllers();

        app.MapFallbackToFile("index.html");

        await app.RunAsync();
    }
}
