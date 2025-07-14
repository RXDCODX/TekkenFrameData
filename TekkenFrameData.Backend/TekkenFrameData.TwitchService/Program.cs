using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.CustomLoggers.TelegramLogger;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.DB.Factory;
using TekkenFrameData.Library.DB.Helpers;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Configuration;
using TekkenFrameData.TwitchService.Services;
using Telegram.Bot;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TekkenFrameData.TwitchService;

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
                SourceName = "TekkenFrameData.TwitchService",
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

        // Настройка Twitch клиента
        services.AddSingleton<ITwitchClient>(sp =>
        {
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
            };
            var logger = sp.GetRequiredService<ILogger<TwitchClient>>();
            var client = new TwitchClient(
                new WebSocketClient(clientOptions),
                ClientProtocol.WebSocket,
                logger
            );

            // Здесь нужно будет настроить токен для Twitch
            // Пока используем заглушку
            client.Initialize(
                new ConnectionCredentials(
                    "your_twitch_username",
                    "your_oauth_token"
                ),
                "your_channel_name"
            );
            client.AutoReListenOnException = true;
            client.AddChatCommandIdentifier('!');
            client.Connect();
            return client;
        });

        services.AddSingleton<ITwitchAPI>(sp =>
        {
            var twitchApi = new TwitchAPI { Settings = { ClientId = configuration.ApiClientId } };
            twitchApi.Settings.AccessToken = twitchApi
                .Auth.GetAccessTokenAsync()
                .GetAwaiter()
                .GetResult();
            twitchApi.Settings.Secret = configuration.ApiClientSecret;
            twitchApi.Settings.Scopes = [AuthScopes.Any];
            return twitchApi;
        });

        // Регистрация Twitch команд
        services.AddSingleton<TwitchCommands>();

        var app = builder.Build();

        var logger = app.Services.GetService<ILogger<Program>>();

        // Инициализация Twitch команд
        var twitchCommands = app.Services.GetRequiredService<TwitchCommands>();

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