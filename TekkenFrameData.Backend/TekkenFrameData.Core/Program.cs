using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Core.Protos;
using TekkenFrameData.Core.Services;
using TekkenFrameData.Library.CustomLoggers.TelegramLogger;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.DB.Factory;
using TekkenFrameData.Library.DB.Helpers;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Configuration;
using TekkenFrameData.Watcher.Services.TelegramBotService;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;
using Telegram.Bot;

namespace TekkenFrameData.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddHealthChecks();

        // Настройка логирования
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Information);
        }
        else
        {
            builder.Logging.AddConsole(options =>
            {
                options.FormatterName = "custom";
            });
            builder.Logging.AddConsoleFormatter<CustomFormatter, ConsoleFormatterOptions>();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
        }

        // Настройка базы данных
        var contextBuilder = new DbContextOptionsBuilder<AppDbContext>();
        BuilderConfigurator.ConfigureBuilder(
            contextBuilder,
            builder.Environment,
            builder.Configuration
        );

        var configuration = GetAppConfig.GetAppConfiguration(builder, contextBuilder);

        // Настройка Telegram бота
        var token = configuration.BotToken;
        var tclient = new TelegramBotClient(token, new HttpClient());

        builder.Logging.AddTelegramLogger(
            new TelegramLoggerOptionsBase
            {
                SourceName = "TekkenFrameData.Core",
                MinimumLevel = LogLevel.Warning,
                ChatId = configuration.AdminIdsArray,
            },
            () => tclient,
            (s, level) => true
        );

        services
            .AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>(_ => tclient);

        // Настройка Entity Framework
        services.AddSingleton<IDbContextFactory<AppDbContext>>(
            (_) =>
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

        // Регистрация Telegram бот сервисов
        services.AddScoped<Commands>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();

        // Регистрация gRPC сервиса
        services.AddGrpc();

        var app = builder.Build();

        var logger = app.Services.GetService<ILogger<Program>>();

        // Настройка gRPC
        app.MapGrpcService<FrameDataGrpcService>();

        // Настройка HTTP
        app.UseDeveloperExceptionPage();
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.MapHealthChecks("/health");
        app.UseRouting();
        app.UseStaticFiles();
        app.UseStatusCodePages();

        try
        {
            await app.RunAsync();
        }
        catch (Exception e)
        {
            logger?.LogException(e);
        }
    }
}