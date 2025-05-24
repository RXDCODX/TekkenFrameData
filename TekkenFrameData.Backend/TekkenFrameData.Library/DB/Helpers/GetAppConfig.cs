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
}
