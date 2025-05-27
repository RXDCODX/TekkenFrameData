namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class AliasAttribute(params string[] methodAliases) : System.Attribute
{
    public string[] MethodAliases { get; } = methodAliases;
}
