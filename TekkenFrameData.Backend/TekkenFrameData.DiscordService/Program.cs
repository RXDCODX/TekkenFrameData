using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Core.Protos;
using TekkenFrameData.DiscordService.Services;
using TekkenFrameData.Library.CustomLoggers.TelegramLogger;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.DB.Factory;
using TekkenFrameData.Library.DB.Helpers;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Configuration;
using Telegram.Bot;

namespace TekkenFrameData.DiscordService;

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

        // Настройка Telegram логгера
        var token = configuration.BotToken;
        var tclient = new TelegramBotClient(token, new HttpClient());

        builder.Logging.AddTelegramLogger(
            new TelegramLoggerOptionsBase
            {
                SourceName = "TekkenFrameData.DiscordService",
                MinimumLevel = LogLevel.Warning,
                ChatId = configuration.AdminIdsArray,
            },
            () => tclient,
            (s, level) => true
        );

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

        // Регистрация gRPC клиента
        services.AddSingleton<FrameDataClient>();

        // Настройка Discord клиента
        var discordToken = configuration.DiscordConfiguration?.BotToken ??
                          builder.Configuration["Discord:BotToken"];

        if (string.IsNullOrEmpty(discordToken))
        {
            throw new InvalidOperationException("Discord bot token is not configured");
        }

        var discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = discordToken,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
        });

        // Настройка slash команд
        var slashCommands = discord.UseSlashCommands();
        slashCommands.RegisterCommands<DiscordCommands>();

        services.AddSingleton(discord);

        var app = builder.Build();

        var logger = app.Services.GetService<ILogger<Program>>();

        // Подключение к Discord
        await discord.ConnectAsync();

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
        finally
        {
            await discord.DisconnectAsync();
        }
    }
}