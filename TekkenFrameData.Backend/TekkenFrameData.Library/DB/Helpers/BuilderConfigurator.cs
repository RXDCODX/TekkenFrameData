using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TekkenFrameData.Library.DB.Helpers;

public static class BuilderConfigurator
{
    public static void ConfigureBuilder(
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
