using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TekkenFrameData.Cli.DB;
using TekkenFrameData.Cli.Services;

namespace TekkenFrameData.Cli;

public class Program
{
    public static async Task Main()
    {
        var configuration = BuildConfiguration();
        var serviceProvider = BuildServiceProvider(configuration);

        var commonOptions = serviceProvider.GetRequiredService<IOptions<Models.CommonOptions>>()?.Value
            ?? throw new Exception("Wrong config");

        if (commonOptions.Action == Models.CommonOptions.ActionEnum.Migrate)
        {
            var dataMigrationService = serviceProvider.GetRequiredService<Interfaces.IDataMigrationService>();
            await dataMigrationService.SchemaMigrateAsync();
            if (!string.IsNullOrEmpty(commonOptions.Scripts))
            {
                await dataMigrationService.RunPostMigrationScriptsAsync();
            }
        }
        else if (commonOptions.Action == Models.CommonOptions.ActionEnum.Delete)
        {
            var databaseRemovalService = serviceProvider.GetRequiredService<Interfaces.IDatabaseRemovalService>();
            await databaseRemovalService.RemoveAsync();
        }
        else
        {
            throw new Exception($"Unknown action: {commonOptions.Action}");
        }
        serviceProvider.Dispose();
        Environment.Exit(0);
    }

    /// <summary>
    /// Настраивает конфигурацию приложения.
    /// </summary>
    /// <returns>Настроенная конфигурация.</returns>
    private static IConfiguration BuildConfiguration()
    {
        ConfigurationBuilder configBuilder = new();

        configBuilder.AddJsonFile("appsettings.json");
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

        services.Configure<Models.CommonOptions>(configuration.GetSection("Migrator"));
        services.PostConfigure<Models.CommonOptions>(c =>
        {
            c.ConnectionString = configuration.GetConnectionString("DB")
                ?? throw new ArgumentNullException(nameof(configuration));
        });

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DB")));
        services.AddTransient<Interfaces.IDatabaseRemovalService, EfDatabaseRemovalService>();
        services.AddTransient<Interfaces.IDataMigrationService, EfDataMigrationService>();

        var serviceProvider = services.BuildServiceProvider()
            ?? throw new Exception("Unable to create service provider");

        return serviceProvider;
    }
}
