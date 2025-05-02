using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using TekkenFrameData.Library.CustomLoggers.TelegramLogger;
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
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using AppDbContext = TekkenFrameData.Library.DB.AppDbContext;
using Commands = TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Commands;

namespace TekkenFrameData.Watcher;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;

        services
            .AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>(
                (client, sp) =>
                {
                    var factory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();

                    var dbContext = factory.CreateDbContext();

                    var configuration = dbContext.Configuration.Single();

                    var token = configuration.BotToken;

                    var tclient = new TelegramBotClient(token, client);

                    builder.Logging.AddConsole();
                    builder.Logging.AddDebug();
                    builder.Logging.SetMinimumLevel(LogLevel.Trace);

                    builder.Logging.AddTelegramLogger(
                        new TelegramLoggerOptionsBase()
                        {
                            SourceName = "Higemus",
                            MinimumLevel = LogLevel.Warning,
                            ChatId = configuration.AdminIdsArray,
                        },
                        () => tclient,
                        (s, level) => true
                    );

                    return tclient;
                }
            );

        services.AddDbContextFactory<AppDbContext>(optionsBuilder =>
        {
            if (builder.Environment.IsDevelopment())
            {
                optionsBuilder.EnableDetailedErrors();
                optionsBuilder.EnableThreadSafetyChecks();
                optionsBuilder
                    .UseNpgsql(builder.Configuration.GetConnectionString("DB"))
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

                optionsBuilder.EnableDetailedErrors();
                optionsBuilder.EnableThreadSafetyChecks();
                optionsBuilder
                    .UseNpgsql(constring.ToString())
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            }
        });

        services.AddSingleton<ITwitchClient>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();

            var dbContext = factory.CreateDbContext();

            var configuration = dbContext.Configuration.Single();

            var client = new TwitchClient(default, default);
            client.Initialize(new ConnectionCredentials("higemus", configuration.ClientOAuthToken));
            client.AddChatCommandIdentifier('!');
            client.AddChatCommandIdentifier('/');
            return client;
        });

        services.AddSingleton<ITwitchAPI>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();

            var dbContext = factory.CreateDbContext();

            var configuration = dbContext.Configuration.Single();

            var twitchApi = new TwitchAPI { Settings = { ClientId = configuration.ApiClientId } };
            twitchApi.Settings.AccessToken = twitchApi.Auth.GetAccessTokenAsync().Result;
            twitchApi.Settings.Secret = configuration.ApiClientSecret;
            twitchApi.Settings.Scopes = [AuthScopes.Any];

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

        var app = builder.Build();

        app.UseDeveloperExceptionPage();
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseStaticFiles();

        app.UseStatusCodePages();

        app.RunAsync();
    }
}
