using System.Collections.Frozen;
using System.Collections.Generic;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public sealed class CharacterNameAutocompleteProvider : IAutocompleteProvider
{
    private static readonly FrozenSet<string> Names =
        Aliases.CharacterNameAliases.Keys.ToFrozenSet();
    private static readonly FrozenSet<string> NamesLower = Names
        .Select(n => n.ToLower())
        .ToFrozenSet();

    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        var query = ctx.OptionValue?.ToString() ?? string.Empty;
        var filtered = NamesLower
            .Where(x => x.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Zip(Names, (lower, original) => (original, lower))
            .Select(x => new DiscordAutoCompleteChoice(x.original, x.lower));
        return Task.FromResult(filtered);
    }
}

public sealed class MoveCommandAutocompleteProvider : IAutocompleteProvider
{
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        // Получаем имя персонажа из опций контекста
        var characterOption = ctx.Options?[0];
        var characterName = characterOption?.Value?.ToString()?.ToLower();
        var query = ctx.OptionValue?.ToString() ?? string.Empty;

        // Проверяем, что имя персонажа выбрано
        if (
            string.IsNullOrWhiteSpace(characterName)
            || Tekken8FrameData.AutocompleteMovesFrozenDictionary == null
            || !Tekken8FrameData.AutocompleteMovesFrozenDictionary.TryGetValue(
                characterName,
                out var movesDict
            )
        )
        {
            return Task.FromResult(Enumerable.Empty<DiscordAutoCompleteChoice>());
        }

        if (movesDict is not { Count: > 0 })
        {
            return Task.FromResult(Enumerable.Empty<DiscordAutoCompleteChoice>());
        }

        var filtered = movesDict
            .Keys.Where(cmd => cmd.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(cmd => new DiscordAutoCompleteChoice(cmd, cmd));
        return Task.FromResult(filtered);
    }
}
