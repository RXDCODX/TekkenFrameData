using System.Collections.Frozen;
using System.Collections.Generic;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public sealed class CharacterNameAutocompleteProvider : IAutocompleteProvider
{
    private static readonly FrozenSet<string> _names =
        Aliases.CharacterNameAliases.Keys.ToFrozenSet();
    private static readonly FrozenSet<string> _namesLower = _names
        .Select(n => n.ToLower())
        .ToFrozenSet();

    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        var query = ctx.OptionValue?.ToString()?.ToLower() ?? string.Empty;
        var filtered = _names
            .Zip(_namesLower, (original, lower) => new { original, lower })
            .Where(x => x.lower.StartsWith(query))
            .Select(x => new DiscordAutoCompleteChoice(x.original, x.original))
            .Take(25);
        return Task.FromResult(filtered);
    }
}
