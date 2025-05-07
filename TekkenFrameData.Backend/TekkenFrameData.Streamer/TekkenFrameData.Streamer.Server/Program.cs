using Microsoft.AspNetCore.SignalR.Client;
using TekkenFrameData.Library.Models.SignalRInterfaces;
using TekkenFrameData.Streamer.Server.Services.StreamControl;

namespace TekkenFrameData.Streamer.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton<HubConnection>(sp =>
        {
            var _connection = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithStatefulReconnect()
                .WithUrl(
                    Environment.GetEnvironmentVariable("SIGNALR_URL")
                        ?? throw new NullReferenceException()
                )
                .Build();

            _connection.On(
                nameof(IMainHubCommands.StartStream),
                () =>
                {
                    var control = sp.GetRequiredService<StreamControlService>();

                    control.StartStream();
                }
            );

            _connection.On(
                nameof(IMainHubCommands.StopStream),
                () =>
                {
                    var control = sp.GetRequiredService<StreamControlService>();

                    control.StopStream();
                }
            );

            _connection.StartAsync();

            return _connection;
        });

        var app = builder.Build();

        app.UseDefaultFiles();
        app.MapStaticAssets();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
