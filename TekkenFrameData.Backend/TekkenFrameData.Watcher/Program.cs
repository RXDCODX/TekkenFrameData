using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using TekkenFrameData.Library.CustomLoggers.TelegramLogger;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Configuration;
using TekkenFrameData.Watcher.Hubs;
using TekkenFrameData.Watcher.Services.Contractor;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.Manager;
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
using AppDbContext = TekkenFrameData.Library.DB.AppDbContext;
using Commands = TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Commands;

namespace TekkenFrameData.Watcher;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddHealthChecks();

        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
        }
        else
        {
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
        }

        var contextBuilder = new DbContextOptionsBuilder<AppDbContext>();
        ConfigureBuilder(contextBuilder, builder.Environment, builder.Configuration);

        Configuration configuration = GetAppConfiguration(builder, contextBuilder);
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

        services.AddDbContextFactory<AppDbContext>(optionsBuilder =>
            ConfigureBuilder(optionsBuilder, builder.Environment, builder.Configuration)
        );

        services.AddSingleton<ITwitchClient>(sp =>
        {
            // Add robust reconnection settings
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
            };

            var client = new TwitchClient(
                new WebSocketClient(clientOptions),
                ClientProtocol.WebSocket,
                sp.GetRequiredService<ILogger<TwitchClient>>()
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
            client.AddChatCommandIdentifier('/');
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

        services.AddScoped<Commands>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
        services.AddSingleton<TwitchFramedateChannelConnecter>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchFramedateChannelConnecter>());
        services.AddSingleton<Tekken8FrameData>();
        services.AddHostedService(sp => sp.GetRequiredService<Tekken8FrameData>());

        services.AddSingleton<ContractorService>();
        services.AddHostedService(sp => sp.GetRequiredService<ContractorService>());

        services.AddSingleton<TwitchAuthService>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchAuthService>());
        services.AddSingleton<TokenService>();
        services.AddSingleton<TelegramTokenNotification>();

        services.AddSingleton<CrossChannelManager>();
        services.AddHostedService(sp => sp.GetRequiredService<CrossChannelManager>());

        services.AddSingleton<TekkenVictorinaLeaderbord>();
        services.AddHostedService(sp => sp.GetRequiredService<TekkenVictorinaLeaderbord>());

        services.AddSignalR();

        var app = builder.Build();

        app.MapHub<MainHub>("/mainhub");

        app.UseDeveloperExceptionPage();
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
        app.MapHealthChecks("/health");
        app.UseRouting();
        app.UseStaticFiles();

        app.UseStatusCodePages();

        app.Run();
    }

    private static Configuration GetAppConfiguration(
        WebApplicationBuilder builder,
        DbContextOptionsBuilder<AppDbContext> contextBuilder
    )
    {
        var dbContext = new AppDbContext(contextBuilder.Options);
        var configuration = dbContext.Configuration.SingleOrDefault();

        if (configuration == null)
        {
            configuration = builder.Configuration.GetSection("Configuration").Get<Configuration>();

            if (configuration == default)
            {
                throw new NullReferenceException();
            }
        }

        return configuration;
    }

    private static void ConfigureBuilder(
        DbContextOptionsBuilder builder,
        IWebHostEnvironment environment,
        IConfiguration configuration
    )
    {
        if (environment.IsDevelopment())
        {
            builder.EnableDetailedErrors();
            builder.EnableThreadSafetyChecks();
            builder
                .UseNpgsql(configuration.GetConnectionString("DB"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        }
        else
        {
            var constring = new NpgsqlConnectionStringBuilder
            {
                { "Host", Environment.GetEnvironmentVariable("DB_HOST")! },
                { "Port", Environment.GetEnvironmentVariable("DB_PORT")! },
                { "Database", Environment.GetEnvironmentVariable("DB_NAME")! },
                { "Username", Environment.GetEnvironmentVariable("DB_USER")! },
                { "Password", Environment.GetEnvironmentVariable("DB_PASSWORD")! },
            };

            builder.EnableDetailedErrors();
            builder.EnableThreadSafetyChecks();
            builder
                .UseNpgsql(constring.ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        }
    }
}
