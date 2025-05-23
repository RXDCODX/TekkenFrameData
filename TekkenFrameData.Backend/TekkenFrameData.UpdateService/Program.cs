using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using TekkenFrameData.Library.Exstensions;

namespace TekkenFrameData.UpdateService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        app.MapPost(
            "/update",
            (HttpContext context) =>
            {
                var request = context.Request.Query["cmd"];
                var cmd = request[0];
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    context.Response.StatusCode = 404;
                    return Results.NotFound();
                }

                var result = cmd.Bash();
                return Results.Accepted(result);
            }
        );
        app.MapControllers();

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStarted.Register(() =>
        {
            var sp = app.Services;
            var server = sp.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>();
            var adress = addresses?.Addresses.First() ?? throw new NullReferenceException();
            Console.WriteLine("Сервер запущен по адрессу: " + adress);
        });

        app.Run();
    }
}
