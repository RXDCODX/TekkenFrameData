using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.DB.Factory;
using TekkenFrameData.Library.DB.Helpers;
using TekkenFrameData.UpdateService.Services.TelegramBotService;
using TekkenFrameData.UpdateService.Services.TelegramBotService.Commands;
using Telegram.Bot;

namespace TekkenFrameData.UpdateService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var services = builder.Services;
        var contextBuilder = new DbContextOptionsBuilder<AppDbContext>();
        BuilderConfigurator.ConfigureBuilder(
            contextBuilder,
            builder.Environment,
            builder.Configuration,
            true
        );
        var appConfiguration = GetAppConfig.GetAppConfiguration(builder, contextBuilder);
        services.AddSingleton<IDbContextFactory<AppDbContext>>(
            (sp) =>
            {
                return new AppDbContextFactory(options =>
                {
                    BuilderConfigurator.ConfigureBuilder(
                        options,
                        builder.Environment,
                        builder.Configuration
                    );
                });
            }
        );

        services
            .AddHttpClient("update_service_client")
            .AddTypedClient<ITelegramBotClient>(
                (client, provider) =>
                    new TelegramBotClient(appConfiguration.UpdateServiceBotToken, client)
            );

        services.AddScoped<Commands>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();

        var app = builder.Build();

        app.Run();
    }
}
