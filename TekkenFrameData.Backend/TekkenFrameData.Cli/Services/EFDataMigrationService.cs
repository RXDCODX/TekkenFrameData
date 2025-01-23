using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TekkenFrameData.Cli.Interfaces;
using TekkenFrameData.Cli.Models;
using AppDbContext = TekkenFrameData.Cli.DB.AppDbContext;

namespace TekkenFrameData.Cli.Services;

/// <summary>
/// Реализация сервиса миграции схемы БД на основе EF Core.
/// </summary>
internal class EfDataMigrationService(
    ILogger<EfDatabaseRemovalService> logger,
    AppDbContext dbContext,
    IOptions<CommonOptions> commonOptions)
    : IDataMigrationService
{
    private readonly CommonOptions _commonOptions = commonOptions.Value;

    /// <inheritdoc />
    public async Task SchemaMigrateAsync()
    {
        logger.LogInformation("Migration starting...");
        dbContext.Database.SetCommandTimeout(TimeSpan.FromHours(1));

        logger.LogInformation(".. Total migraions: {totalMigrations}", dbContext.Database.GetMigrations().Count());
        logger.LogInformation(".. Applied migraions: {appliedMigrations}", (await dbContext.Database.GetAppliedMigrationsAsync()).Count());
        logger.LogInformation(".. Pending migraions: {pendingMigrations}", (await dbContext.Database.GetPendingMigrationsAsync()).Count());

        await dbContext.Database.MigrateAsync();

        logger.LogInformation("Migration has been completed!");
    }

    /// <inheritdoc />
    public async Task RunPostMigrationScriptsAsync()
    {
        var scriptsPath = _commonOptions.Scripts;
        if (string.IsNullOrEmpty(scriptsPath))
        {
            logger.LogWarning("There if no scripts path data. Skip!");
            return;
        }

        foreach (var scriptPath in scriptsPath.Split(';'))
        {
            await RunMigrationScriptsAsync(dbContext, scriptPath);
        }
    }

    /// <summary>
    /// Запускает скрипты миграции на БД.
    /// </summary>
    /// <param name="context">Контекст данных.</param>
    /// <param name="path">Путь до папки со скриптами.</param>
    private async Task RunMigrationScriptsAsync(DbContext context, string path)
    {
        var dir = new DirectoryInfo(path);
        logger.LogInformation("Try to applying migration scripts from dir {dirPath}", dir.FullName);
        if (!dir.Exists)
        {
            throw new Exception("Migration path not exists");
        }
        var scriptFiles = Directory.GetFiles(dir.FullName).OrderBy(i => i).ToArray();
        logger.LogInformation(".. found {scriptsCount} file(s)", scriptFiles.Length);
        foreach (var scriptFile in scriptFiles)
        {
            logger.LogInformation(".. .. applying {scriptFile}...", Path.GetFileName(scriptFile));
            var scriptText = File.ReadAllText(scriptFile);
            await context.Database.ExecuteSqlRawAsync(scriptText);
        }
        logger.LogInformation("Completed!");
    }
}
