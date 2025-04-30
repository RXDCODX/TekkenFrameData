using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;

namespace TekkenFrameData.Service;

class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;

        builder.Services.AddOpenApi();

        services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            var environmentVariable = Environment.GetEnvironmentVariable(
                "ASPNETCORE_DOCKER_LAUNCH"
            );
            if (environmentVariable == "TRUE")
            {
                optionsBuilder
                    .UseNpgsql(builder.Configuration.GetConnectionString("docker_pg"))
                    .EnableThreadSafetyChecks();
            }
            else
            {
                optionsBuilder
                    .UseNpgsql(builder.Configuration.GetConnectionString("local_pg"))
                    .EnableThreadSafetyChecks();
            }
        });

        services.AddLogging();

        var app = builder.Build();
        app.Migrate();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
