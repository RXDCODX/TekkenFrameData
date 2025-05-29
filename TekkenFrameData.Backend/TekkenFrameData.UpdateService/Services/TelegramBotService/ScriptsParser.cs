using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.OpenApi.Expressions;
using Newtonsoft.Json.Linq;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService;

public static class ScriptsParser
{
    public static FrozenDictionary<string, string> ScriptsDictionary { get; private set; } =
        new Dictionary<string, string>().ToFrozenDictionary();

    public static readonly string ScriptsFolder = Path.Combine(
        Directory.GetCurrentDirectory(),
        "scripts"
    );
    public static readonly string IniFilePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "config.ini"
    );

    public static void UpdateScripts()
    {
        var scipts = File.ReadAllLines(IniFilePath)
            .Select(e =>
            {
                var keyValueSplit = e.Split('=');
                return new KeyValuePair<string, string>(keyValueSplit[0], keyValueSplit[1]);
            })
            .ToDictionary();

        if (scipts.Any(pair => !File.Exists(Path.Combine(ScriptsFolder, pair.Value))))
        {
            throw new NullReferenceException();
        }

        ScriptsDictionary = scipts.ToFrozenDictionary();
    }
}
