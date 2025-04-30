using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.CustomLoggers.TelegramLogger;
using TekkenFrameData.Watcher.Services.Framedata;
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
    public static async Task Main(string[] args)
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
            optionsBuilder
                .UseNpgsql(builder.Configuration.GetConnectionString("DB"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            if (builder.Environment.IsDevelopment())
            {
                optionsBuilder.EnableDetailedErrors();
                optionsBuilder.EnableThreadSafetyChecks();
            }
        });

        services.AddSingleton<ITwitchClient>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();

            var dbContext = factory.CreateDbContext();

            var configuration = dbContext
                .Configuration.Include(configuration => configuration.Token)
                .Single();

            var token = configuration.Token;

            var client = new TwitchClient(default, default);
            client.Initialize(new ConnectionCredentials("higemus", token.AccessToken));
            return client;
        });

        services.AddScoped<Commands>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
        services.AddSingleton<TwitchFramedateChannelConnecter>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchFramedateChannelConnecter>());
        services.AddSingleton<Tekken8FrameData>();
        services.AddSingleton<TwitchReconector>();
        services.AddHostedService(sp => sp.GetRequiredService<TwitchReconector>());

        var app = builder.Build();

        app.UseDeveloperExceptionPage();
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseStaticFiles();

        app.UseStatusCodePages();

        await app.RunAsync();
    }
}
