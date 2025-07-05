using System.Diagnostics.CodeAnalysis;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class DescriptionAttribute([NotNull] string description) : System.Attribute
{
    public string Description { get; init; } = description;
}
