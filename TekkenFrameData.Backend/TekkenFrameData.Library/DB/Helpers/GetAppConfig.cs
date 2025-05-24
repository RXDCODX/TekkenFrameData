using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.Configuration;

namespace TekkenFrameData.Library.DB.Helpers;

public static class GetAppConfig
{
    public static Configuration GetAppConfiguration(
        WebApplicationBuilder builder,
        DbContextOptionsBuilder<AppDbContext> contextBuilder
    )
    {
        Configuration configuration = null!;
        try
        {
            var dbContext = new AppDbContext(contextBuilder.Options);
            configuration = dbContext.Configuration.Single();
        }
        catch (Exception ex)
        {
            configuration =
                builder.Configuration.GetSection("Configuration").Get<Configuration>()
                ?? throw new InvalidOperationException();
        }

        return configuration;
    }
}
