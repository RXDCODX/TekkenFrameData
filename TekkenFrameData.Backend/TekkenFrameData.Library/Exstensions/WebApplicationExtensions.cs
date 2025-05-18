using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;

namespace TekkenFrameData.Library.Exstensions;

public static class WebApplicationExtensions
{
    public static IApplicationBuilder Migrate(this IApplicationBuilder app)
    {
        var context = GetAppDbContext(app.ApplicationServices);
        var logger = GetAppLogger(app.ApplicationServices);

        logger.LogInformation("Начало миграции базы данных.");

        logger.LogInformation("Проверка что база данных существует.");
        context.Database.EnsureCreated();
        logger.LogInformation("База данных существует.");

        var pendingMigrations = context.Database.GetPendingMigrations();

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Было найдены новые миграции. Применяем.");
            context.Database.Migrate();
        }
        else
        {
            logger.LogInformation("Новые миграции отсуствуют.");
        }

        return app;
    }

    private static ILogger GetAppLogger(IServiceProvider appApplicationServices)
    {
        var logger = appApplicationServices.GetRequiredService<ILogger<AppDbContext>>();
        return logger;
    }

    private static AppDbContext GetAppDbContext(IServiceProvider appApplicationServices)
    {
        var scopeFactory = appApplicationServices.GetRequiredService<IServiceScopeFactory>();
        var serviceScope = scopeFactory.CreateScope();
        var appDbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

        return appDbContext;
    }
}
