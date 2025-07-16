using System.Collections.Frozen;
using System.Collections.Generic;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TekkenFrameData.Library.Exstensions;
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
            .Select(x => new DiscordAutoCompleteChoice(x.original.FirstCharToUpper(), x.original))
            .Take(25);
        return Task.FromResult(filtered);
    }
}

public sealed class MoveCommandAutocompleteProvider : IAutocompleteProvider
{
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        // Получаем имя персонажа из опций контекста
        var characterOption = ctx.Options?.FirstOrDefault(o => o.Name == "character");
        var characterName = characterOption?.Value?.ToString()?.ToLower();
        var query = ctx.OptionValue?.ToString()?.ToLower() ?? string.Empty;

        // Проверяем, что имя персонажа выбрано
        if (
            string.IsNullOrWhiteSpace(characterName)
            || Tekken8FrameData.AutocompleteMovesFrozenDictionary == null
            || !Tekken8FrameData.AutocompleteMovesFrozenDictionary.ContainsKey(characterName)
        )
        {
            return Task.FromResult(Enumerable.Empty<DiscordAutoCompleteChoice>());
        }

        var movesDict = Tekken8FrameData.AutocompleteMovesFrozenDictionary[characterName];
        var filtered = movesDict
            .Keys.Where(cmd => cmd.ToLower().StartsWith(query))
            .Select(cmd => new DiscordAutoCompleteChoice(cmd.FirstCharToUpper(), cmd))
            .Take(25);
        return Task.FromResult(filtered);
    }
}
