using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TekkenFrameData.Library.DB;

namespace TekkenFrameData.Service;

class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql("Server=tekken_db;Port=5432;Database=tekken_db;User Id=postgres;Password=postgres;")
                .EnableThreadSafetyChecks();
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        await app.RunAsync();
    }
}
