namespace TekkenFrameData.Library.Models.Configuration;

/// <summary>
/// Основной класс для конфигурации
/// Тут должны быть только конструктор и partial методы.
///
/// Состоит из:
/// <include file='TwitchConfiguration.cs' path='[@name="TwitchConfiguration.cs"]'/>
/// </summary>
public partial class Configuration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SSH_Login { get; set; } = null!;
    public string SSH_Password { get; set; } = null!;
}
