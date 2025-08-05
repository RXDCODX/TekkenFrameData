using System.Net.Http;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TekkenFrameData.Library.CustomLoggers.TelegramLogger;
using TekkenFrameData.Library.DB.Factory;
using TekkenFrameData.Library.DB.Helpers;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Configuration;
using TekkenFrameData.Watcher.Hubs;
using TekkenFrameData.Watcher.Services.AlisaService;
using TekkenFrameData.Watcher.Services.Contractor;
using TekkenFrameData.Watcher.Services.DailyStreak;
using TekkenFrameData.Watcher.Services.Discord;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.Manager;
using TekkenFrameData.Watcher.Services.StreamersNotificationsService;
using TekkenFrameData.Watcher.Services.TekkenVictorina;
using TekkenFrameData.Watcher.Services.TelegramBotService;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using Telegram.Bot;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using Commands = TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Commands;

namespace TekkenFrameData.Watcher;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddHealthChecks();

        if (builder.Environment.IsDevelopment())
        {
            // Регистрируем кастомный форматтер
            builder.Logging.AddDebug();

            builder.Logging.SetMinimumLevel(LogLevel.Information);
        }
        else
        {
            builder.Logging.AddConsole(options =>
            {
                options.FormatterName = "custom"; // Указываем имя форматтера
            });
            // Регистрируем кастомный форматтер
            builder.Logging.AddConsoleFormatter<CustomFormatter, ConsoleFormatterOptions>();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
        }

        var contextBuilder = new DbContextOptionsBuilder<AppDbContext>();
        BuilderConfigurator.ConfigureBuilder(
            contextBuilder,
            builder.Environment,
            builder.Configuration
        );

        var configuration = GetAppConfig.GetAppConfiguration(builder, contextBuilder);
        var token = configuration.BotToken;
        var tclient = new TelegramBotClient(token, new HttpClient());
        builder.Logging.AddTelegramLogger(
            new TelegramLoggerOptionsBase
            {
                SourceName = TwitchClientExstension.Channel,
                MinimumLevel = LogLevel.Warning,
                ChatId = configuration.AdminIdsArray,
            },
            () => tclient,
            (s, level) => true
        );

        services
            .AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>(_ => tclient);

        services.AddTwitchEvents(configuration);
        services.AddDiscordServices(configuration);

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

        services.AddScoped<Commands>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
        services.AddSingleton<TwitchFramedateChannelConnecter>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchFramedateChannelConnecter>());

        services.AddSingleton<Tekken8FrameData>();
        services.AddHostedService(sp => sp.GetRequiredService<Tekken8FrameData>());

        services.AddSingleton<TwitchAuthService>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchAuthService>());
        services.AddSingleton<TokenService>();
        services.AddSingleton<TelegramTokenNotification>();

        services.AddSingleton<TekkenVictorinaLeaderbord>();
        services.AddHostedService(sp => sp.GetRequiredService<TekkenVictorinaLeaderbord>());

        services.AddSingleton<StreamersNotificationWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<StreamersNotificationWorker>());
        services.AddSingleton<MessagesHandler>();

        services.AddSingleton<DailyStreakService>();
        services.AddSingleton<DailyStreakSiteParser>();
        services.AddSingleton<DailyStreakHandler>();
        services.AddHostedService(sp => sp.GetRequiredService<DailyStreakService>());
        services.AddHostedService(sp => sp.GetRequiredService<DailyStreakHandler>());

        services.AddSingleton<AlisaBlocklist>();
        services.AddHostedService(sp => sp.GetRequiredService<AlisaBlocklist>());
        services.AddSingleton<AlisaCall>();
        services.AddHostedService(sp => sp.GetRequiredService<AlisaCall>());

        services.AddSignalR();

        var app = builder.Build();
        var logger = app.Services.GetService<ILogger<Program>>();
        app.MapHub<MainHub>("/mainhub");

        app.UseDeveloperExceptionPage();
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.MapHealthChecks("/health");
        app.UseRouting();
        app.UseStaticFiles();

        app.UseStatusCodePages();

        var aa = app.Services.GetRequiredService<DailyStreakService>();

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

internal static class ProgramInitExstension
{
    public static IServiceCollection AddTwitchEvents(
        this IServiceCollection services,
        Configuration configuration
    )
    {
        services.AddSingleton<ITwitchClient>(sp =>
        {
            // Add robust reconnection settings
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
            var tokenService = sp.GetRequiredService<TokenService>();
            var twitchTokenInfo =
                tokenService.GetTokenAsync(CancellationToken.None).GetAwaiter().GetResult()
                ?? throw new NullReferenceException();
            tokenService.RefreshTokenAsync(twitchTokenInfo).GetAwaiter().GetResult();

            client.Initialize(
                new ConnectionCredentials(
                    TwitchClientExstension.Channel,
                    tokenService.Token!.AccessToken
                ),
                TwitchClientExstension.Channel
            );
            client.AutoReListenOnException = true;
            client.AddChatCommandIdentifier('!');
            client.Connect();
            return client;
        });

        services.AddSingleton<ITwitchAPI>(sp =>
        {
            var twitchApi = new TwitchAPI { Settings = { ClientId = configuration.ApiClientId } };
#pragma warning disable CA2012
            twitchApi.Settings.AccessToken = twitchApi
                .Auth.GetAccessTokenAsync()
                .GetAwaiter()
                .GetResult();
            twitchApi.Settings.Secret = configuration.ApiClientSecret;
            twitchApi.Settings.Scopes = [AuthScopes.Any];
#pragma warning restore CA2012
            return twitchApi;
        });

        services.AddSingleton<CrossChannelManager>();
        services.AddHostedService(sp => sp.GetRequiredService<CrossChannelManager>());
        services.AddSingleton<TwitchFramedate>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchFramedate>());
        services.AddSingleton<TwitchFramedateChannelConnecter>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchFramedateChannelConnecter>());
        services.AddSingleton<TwitchFramedataChannelsEvents>();

        services.AddSingleton<ContractorService>();
        services.AddHostedService(sp => sp.GetRequiredService<ContractorService>());

        services.AddSingleton<AlisaAssistantCollab>();
        services.AddHostedService(sp => sp.GetRequiredService<AlisaAssistantCollab>());

        services.AddSingleton<TwitchRulesCommand>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchRulesCommand>());

        return services;
    }

    public static IServiceCollection AddDiscordServices(
        this IServiceCollection services,
        Configuration config
    )
    {
        services.AddSingleton<DiscordClient>(
            (sp) =>
            {
                var discordConfig = new DiscordConfiguration
                {
                    Token = config.DiscordToken,
                    TokenType = TokenType.Bot,
                    Intents =
                        DiscordIntents.AllUnprivileged
                        | DiscordIntents.MessageContents
                        | DiscordIntents.GuildMessages,
                    MinimumLogLevel = LogLevel.Warning,
                    AutoReconnect = true,
                };
                var client = new DiscordClient(discordConfig);
                var commands = client.UseSlashCommands(
                    new SlashCommandsConfiguration() { Services = sp }
                );
                client.GuildAvailable += async (sender, args) =>
                {
                    DiscordBotAnswers.TechChannel = await sender.GetChannelAsync(
                        1394393334474211491
                    );
                    await client.UpdateStatusAsync(
                        new DiscordActivity(
                            "задницу твоей мамаши на весь твич",
                            ActivityType.Streaming
                        )
                    );
                };
                client.SocketClosed += ClientOnSocketClosed;

                client.SocketErrored += ClientOnSocketErrored;

                commands.RegisterCommands<FrameDataSlashCommands>();

                Task.Factory.StartNew(() => client.ConnectAsync());
                return client;

                async Task ClientOnSocketErrored(DiscordClient sender, SocketErrorEventArgs args)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    var logger = sp.GetRequiredService<ILogger<DiscordClient>>();
                    logger.LogException(args.Exception);
                    await sender.ReconnectAsync(true);
                }

                async Task ClientOnSocketClosed(DiscordClient sender, SocketCloseEventArgs args)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    var logger = sp.GetRequiredService<ILogger<DiscordClient>>();
                    logger.LogError("{Message}", args.CloseMessage);
                    await sender.ReconnectAsync(true);
                }
            }
        );

        services.AddSingleton<DiscordManager>();
        services.AddHostedService<DiscordManager>();

        return services;
    }
}
