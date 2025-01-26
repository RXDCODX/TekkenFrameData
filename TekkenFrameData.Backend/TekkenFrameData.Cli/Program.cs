using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Cli.DB;
using TekkenFrameData.Cli.Interfaces;
using TekkenFrameData.Cli.Services;

namespace TekkenFrameData.Cli;

public class Program
{
    public static async Task Main()
    {
        var configuration = BuildConfiguration();
        var serviceProvider = BuildServiceProvider(configuration);

        var dataMigrationService = serviceProvider.GetRequiredService<IDataMigrationService>();
        await dataMigrationService.SchemaMigrateAsync();

        await serviceProvider.DisposeAsync();
        Environment.Exit(0);
    }

    /// <summary>
    /// Настраивает конфигурацию приложения.
    /// </summary>
    /// <returns>Настроенная конфигурация.</returns>
    private static IConfiguration BuildConfiguration()
    {
        ConfigurationBuilder configBuilder = new();

        var otherAppsettings = Environment.GetEnvironmentVariable("APPSETTINGS");
        if (!string.IsNullOrWhiteSpace(otherAppsettings))
        {
            configBuilder.AddJsonFile(otherAppsettings);
        }

        configBuilder.AddCommandLine(Environment.GetCommandLineArgs());
        configBuilder.AddEnvironmentVariables();

        IConfiguration config = configBuilder.Build();

        return config;
    }

    /// <summary>
    /// Настраивает контейнер DI.
    /// </summary>
    /// <param name="configuration">Текушая конфигурация приложения.</param>
    /// <returns>Настроенный контейнер DI.</returns>
    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging(c =>
        {
            c.AddConsole();
            c.AddFilter(categoryLevelFilter: (category, level) => category?.StartsWith("Microsoft.EntityFrameworkCore") != true);
        });

        //services.ConfigureDbContext<AppDbContext>(builder =>
        //{
        //    builder.UseNpgsql(configuration.GetConnectionString("DB")).EnableThreadSafetyChecks();
        //});

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Host=localhost;Port=5623;Database=postgres;Username=postgres;Password=postgres;"));
        services.AddTransient<IDatabaseRemovalService, EfDatabaseRemovalService>();
        services.AddTransient<IDataMigrationService, EfDataMigrationService>();

        var serviceProvider = services.BuildServiceProvider()
            ?? throw new Exception("Unable to create service provider");

        return serviceProvider;
    }
}
