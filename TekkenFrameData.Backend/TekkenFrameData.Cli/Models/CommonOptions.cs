namespace TekkenFrameData.Cli.Models;

/// <summary>
/// Основные настройки приложения.
/// </summary>
internal class CommonOptions
{
    /// <summary>
    /// Строка подключения до БД.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Тип действий с БД.
    /// </summary>
    public ActionEnum Action { get; set; }

    /// <summary>
    /// Пути до папок со скриптами.
    /// Указываются через ';'.
    /// </summary>
    public string? Scripts { get; set; }

    /// <summary>
    /// Путь до папки с функциями БД, которые надо синхронизировать.
    /// </summary>
    public string? Routines { get; set; }

    /// <summary>
    /// Действие.
    /// </summary>
    public enum ActionEnum
    {
        /// <summary>
        /// Миграция схемы данных и синхронизация функций БД.
        /// </summary>
        Migrate = 0,

        /// <summary>
        /// Удаление базы данных.
        /// </summary>
        Delete = 1,
    }
}
