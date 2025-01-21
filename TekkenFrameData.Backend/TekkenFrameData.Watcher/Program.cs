using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Watcher.TelegramLogger;
using TekkenFrameData.Watcher.DB;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.TelegramBotService;
using TekkenFrameData.Watcher.Services.TelegramBotService.Commands;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using Telegram.Bot;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Environment.EnvironmentName = "Development";

        if (builder.Environment.IsDevelopment())
        {
            Environment.SetEnvironmentVariable("token", "sburxpcmdt2anh791ooxcq1bgefgyxmbskc6rlx651k8yokdwb");
        }

        var services = builder.Services;

        services.AddHttpClient("telegram_bot_client").AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            TelegramBotClientOptions options = new("7320382686:AAE_nK-vnSlnVGQuUUkQCBev9zWUU9DAhJw");

            return new TelegramBotClient(options, httpClient);
        });

        services.AddDbContextFactory<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlite("Data Source=DATABASE.db;").
                UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            if (builder.Environment.IsDevelopment())
            {
                optionsBuilder.EnableDetailedErrors();
                optionsBuilder.EnableThreadSafetyChecks();
            }
        });

        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        builder.Logging.AddTelegramLogger(options =>
        {
            options.BotToken = "7320382686:AAE_nK-vnSlnVGQuUUkQCBev9zWUU9DAhJw";
            options.ChatId = [402763435, 1917524881];
            options.SourceName = "Higemus";
            options.MinimumLevel = LogLevel.Warning;
        });

        var twitchApi = new TwitchAPI();
        twitchApi.Settings.ClientId = "zp4lacics0o2j0l3huzw1gtcp64ck7";
        twitchApi.Settings.AccessToken = await twitchApi.Auth.GetAccessTokenAsync();
        twitchApi.Settings.Secret = "vx3i8o1egpo4zssseu70jqg8hbkgnw";
        twitchApi.Settings.Scopes = [AuthScopes.Any];

        services.AddSingleton<ITwitchAPI>(twitchApi);

        var client = new TwitchClient(default, default);

        services.AddSingleton<ITwitchClient>(client);

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