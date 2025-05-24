namespace TekkenFrameData.UpdateService.Services.TelegramBotService.Commands.Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class AliasAttribute(params string[] methodAliases) : System.Attribute
{
    public string[] MethodAliases { get; } = methodAliases;
}
