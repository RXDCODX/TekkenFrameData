namespace TekkenFrameData.Cli.Interfaces;

/// <summary>
/// Сервис миграции схемы БД.
/// </summary>
internal interface IDataMigrationService
{
    /// <summary>
    /// Мигрирует схему БД.
    /// </summary>
    Task SchemaMigrateAsync();

    /// <summary>
    /// Выполняет сценарии, исполняемые после миграции схемы БД.
    /// </summary>
    Task RunPostMigrationScriptsAsync();
}
