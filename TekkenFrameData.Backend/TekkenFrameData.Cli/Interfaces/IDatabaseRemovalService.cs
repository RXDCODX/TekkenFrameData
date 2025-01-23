namespace TekkenFrameData.Cli.Interfaces;

/// <summary>
/// Сервис удаления БД.
/// </summary>
internal interface IDatabaseRemovalService
{
    /// <summary>
    /// Удаляет БД со всеми потрохами.
    /// </summary>
    Task RemoveAsync();
}
