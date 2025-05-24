using Microsoft.EntityFrameworkCore;
using Npgsql;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.DB.Helpers;
using Telegram.Bot;

namespace TekkenFrameData.UpdateService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        var contextBuilder = new DbContextOptionsBuilder<AppDbContext>();
        BuilderConfigurator.ConfigureBuilder(
            contextBuilder,
            builder.Environment,
            builder.Configuration
        );
        var configuration = GetAppConfig.GetAppConfiguration(builder, contextBuilder);

        services.AddDbContextFactory<AppDbContext>(optionsBuilder =>
            BuilderConfigurator.ConfigureBuilder(
                optionsBuilder,
                builder.Environment,
                builder.Configuration
            )
        );

        services
            .AddHttpClient("update_service_client")
            .AddTypedClient<ITelegramBotClient>(
                (client, provider) =>
                    new TelegramBotClient(configuration.UpdateServiceBotToken, client)
            );

        var app = builder.Build();

        app.Run();
    }
}
