using System.Diagnostics.CodeAnalysis;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;

public class DescriptionAttribute([NotNull] string description) : System.Attribute
{
    public string Description { get; init; } = description;
}
