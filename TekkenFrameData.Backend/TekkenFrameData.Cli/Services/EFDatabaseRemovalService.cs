using Microsoft.Extensions.Logging;
using TekkenFrameData.Cli.Interfaces;
using AppDbContext = TekkenFrameData.Cli.DB.AppDbContext;

namespace TekkenFrameData.Cli.Services;

/// <summary>
/// Реализация сервиса удаления БД на основе EF Core.
/// </summary>
internal class EfDatabaseRemovalService(
    ILogger<EfDatabaseRemovalService> logger,
    AppDbContext dbContext) : IDatabaseRemovalService
{
    /// <inheritdoc />
    public async Task RemoveAsync()
    {
        logger.LogInformation("Delete started...");
        await dbContext.Database.EnsureDeletedAsync();
        logger.LogInformation("Delete completed!");
    }
}
