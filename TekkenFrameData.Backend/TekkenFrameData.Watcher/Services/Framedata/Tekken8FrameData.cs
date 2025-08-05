﻿using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using Telegram.Bot;

namespace TekkenFrameData.Watcher.Services.Framedata;

public partial class Tekken8FrameData(
    ILogger<Tekken8FrameData> logger,
    IDbContextFactory<AppDbContext> dbContextFactory,
    IHostApplicationLifetime lifetime,
    ITelegramBotClient client
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(stoppingToken);
        var list = dbContext.TekkenCharacters.AsNoTracking().ToList();
        var character = list.FirstOrDefault();
        if (
            character == null
            || !IsDateInCurrentWeek(character.LastUpdateTime).GetAwaiter().GetResult()
        )
        {
            await Task.Factory.StartNew(() => StartScrupFrameData(), stoppingToken);
        }
        else
        {
            await UpdateMovesForVictorina();
            await UpdateAutocompleteDictionary();
        }
    }

    public async Task<(TekkenMoveTag Tag, Move[] Moves)?> GetMultipleMovesByTags(string input)
    {
        var split = input.Split(
            ' ',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        var lastSplit = split.Last();
        var isMultiple = lastSplit.EndsWith('s') || lastSplit.EndsWith("es");
        var characterName = string.Join(' ', split.SkipLast(1));

        var characterMovelist = await GetCharMoveListAsync(characterName);

        if (characterMovelist == null)
        {
            return null;
        }

        if (isMultiple)
        {
            var result = await GetMultipleMovesFromMovelistByTagAsync(lastSplit, characterMovelist);

            return result;
        }

        (var tag, var move) = await GetMoveFromMovelistByTagAsync(lastSplit, characterMovelist);

        return move != null ? (Tag: tag, [move]) : null;
    }

    public async Task<IDictionary<string, string>?> GetCharacterStances(
        string characterName,
        CancellationToken? stoppingToken
    )
    {
        stoppingToken ??= _cancellationToken;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(
            stoppingToken.Value
        );

        var tekkenChar = await FindCharacterInDatabaseAsync(characterName, dbContext);

        if (tekkenChar == null)
        {
            return null;
        }

        var movelist = await GetCharacterStances(tekkenChar, stoppingToken);
        return movelist;
    }

    private static readonly ValueComparer<Move> StancesComparer = new(
        (e1, e2) =>
            e1 != null
            && e1.StanceCode != null
            && e2 != null
            && e2.StanceCode != null
            && e1.StanceCode.Contains(e2.StanceCode, StringComparison.OrdinalIgnoreCase),
        e => HashCode.Combine(e.StanceCode)
    );

    public async Task<IDictionary<string, string>> GetCharacterStances(
        Character character,
        CancellationToken? stoppingToken
    )
    {
        stoppingToken ??= _cancellationToken;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(
            stoppingToken.Value
        );

        var stancesAndMoves = (
            await dbContext
                .TekkenMoves.AsNoTracking()
                .Where(e =>
                    e.CharacterName == character.Name
                    && e.StanceName != null
                    && e.StanceCode != string.Empty
                )
                .ToListAsync(stoppingToken.Value)
        )
            .Distinct(StancesComparer)
            .ToDictionary(e => e.StanceCode!, e => e.StanceName ?? string.Empty);

        return stancesAndMoves;
    }

    public async Task<Character?> GetTekkenCharacter(string name, bool isWithMoveList = false)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        Character result = null!;
        result = isWithMoveList
            ? await dbContext
                .TekkenCharacters.Include(e => e.Movelist)
                .AsNoTracking()
                .FirstAsync(e => e.Name.Equals(name), cancellationToken: _cancellationToken)
            : await dbContext
                .TekkenCharacters.AsNoTracking()
                .FirstAsync(e => e.Name.Equals(name), cancellationToken: _cancellationToken);

        return result;
    }

    public async Task<Move[]?> GetCharMoveListAsync(string charname)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        var character = await dbContext
            .TekkenCharacters.Include(e => e.Movelist)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Name.Equals(charname),
                cancellationToken: _cancellationToken
            );

        return character?.Movelist?.ToArray();
    }

    public async Task<Move?> GetMoveAsync(string[]? command)
    {
        if (command == null || command.Length == 0)
        {
            return null;
        }

        var result = await FindCharacterByNameAsync(command);

        var charnameOut = result.character;
        var length = result.length;

        if (command.Length <= 1 || charnameOut is null)
        {
            return null;
        }

        var input = string.Join(" ", command.TakeLast(command.Length - length)).ToLower();

        if (string.IsNullOrWhiteSpace(charnameOut.Name) || string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);

        var movelist = await dbContext
            .TekkenMoves.AsNoTracking()
            .Where(e => e.Character == charnameOut)
            .Include(e => e.Character)
            .ToListAsync(_cancellationToken);

        if (movelist is { Count: > 0 })
        {
            var move =
                await GetMoveFromMovelistByCommandAsync(input, movelist)
                ?? (await GetMoveFromMovelistByTagAsync(input, movelist)).move;

            return move;
        }

        return null;
    }

    private async Task<(Character? character, int length)> FindCharacterByNameAsync(
        string[]? commandParts
    )
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);

        // Сначала пробуем найти по двум словам
        var charname = string.Join(" ", commandParts?.Take(2) ?? []);
        var character = await FindCharacterInDatabaseAsync(charname, dbContext);

        if (character != null)
        {
            return (character, 2);
        }

        // Если не нашли, пробуем по одному слову
        charname = string.Join(" ", commandParts?.Take(1) ?? []);
        character = await FindCharacterInDatabaseAsync(charname, dbContext);
        return (character, 1);
    }

    public async Task<Character?> FindCharacterInDatabaseAsync(
        string charname,
        AppDbContext dbContext
    )
    {
        foreach (var aliasPair in Aliases.CharacterNameAliases)
        {
            if (aliasPair.Key.Equals(charname) || aliasPair.Value.Any(e => e.Equals(charname)))
            {
                var characterName = aliasPair.Key;

                var characters = dbContext.TekkenCharacters.AsAsyncEnumerable();
                await foreach (var tekkenCharacter in characters)
                {
                    if (
                        tekkenCharacter.Name.Equals(
                            characterName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return tekkenCharacter;
                    }
                }
            }
        }
        return null;
    }

    private static Task<(TekkenMoveTag tag, Move[])?> GetMultipleMovesFromMovelistByTagAsync(
        string input,
        ICollection<Move> movelist
    )
    {
        Move[] moves = [];

        var typeWithoutStance = MoveTags
            .FirstOrDefault(e =>
                e.Value.Any(b => b.Equals(input, StringComparison.OrdinalIgnoreCase))
            )
            .Key;

        if (typeWithoutStance == TekkenMoveTag.None)
        {
            moves =
            [
                .. movelist.Where(e =>
                    (e.StanceName?.Equals(input) ?? false) || (e.StanceCode?.Equals(input) ?? false)
                ),
            ];

            return Task.FromResult<(TekkenMoveTag tag, Move[])?>((TekkenMoveTag.None, moves));
        }

        switch (typeWithoutStance)
        {
            case TekkenMoveTag.HeatBurst:
                moves = [.. movelist.Where(e => e is { HeatBurst: true })];
                break;
            case TekkenMoveTag.HeatEngage:
                moves = [.. movelist.Where(e => e is { HeatEngage: true })];
                break;
            case TekkenMoveTag.HeatSmash:
                moves = [.. movelist.Where(e => e is { HeatSmash: true })];
                break;
            case TekkenMoveTag.Homing:
                moves = [.. movelist.Where(e => e is { Homing: true })];
                break;
            case TekkenMoveTag.PowerCrush:
                moves = [.. movelist.Where(e => e is { PowerCrush: true })];
                break;
            case TekkenMoveTag.Throw:
                moves = [.. movelist.Where(e => e is { Throw: true })];
                break;
            case TekkenMoveTag.Tornado:
                moves = [.. movelist.Where(e => e is { Tornado: true })];
                break;
        }

        return Task.FromResult<(TekkenMoveTag tag, Move[])?>((typeWithoutStance, moves));
    }

    private static Task<(TekkenMoveTag tag, Move? move)> GetMoveFromMovelistByTagAsync(
        string input,
        ICollection<Move> movelist
    )
    {
        Move? move = null;

        var typeWithoutStance = MoveTags
            .FirstOrDefault(e =>
                e.Value.Any(b => b.Equals(input, StringComparison.OrdinalIgnoreCase))
            )
            .Key;

        if (typeWithoutStance == TekkenMoveTag.None)
        {
            move = movelist.FirstOrDefault(e =>
                (e.StanceName?.Equals(input) ?? false) || (e.StanceCode?.Equals(input) ?? false)
            );

            return Task.FromResult((TekkenMoveTag.None, move));
        }

        switch (typeWithoutStance)
        {
            case TekkenMoveTag.HeatBurst:
                move = movelist.LastOrDefault(e => e is { HeatBurst: true }, null);
                break;
            case TekkenMoveTag.HeatEngage:
                move = movelist.LastOrDefault(e => e is { HeatEngage: true }, null);
                break;
            case TekkenMoveTag.HeatSmash:
                move = movelist.LastOrDefault(e => e is { HeatSmash: true }, null);
                break;
            case TekkenMoveTag.Homing:
                move = movelist.LastOrDefault(e => e is { Homing: true }, null);
                break;
            case TekkenMoveTag.PowerCrush:
                move = movelist.LastOrDefault(e => e is { PowerCrush: true });
                break;
            case TekkenMoveTag.Throw:
                move = movelist.LastOrDefault(e => e is { Throw: true }, null);
                break;
            case TekkenMoveTag.Tornado:
                move = movelist.LastOrDefault(e => e is { Tornado: true }, null);
                break;
        }

        return Task.FromResult((TekkenMoveTag.None, move));
    }

    private static Task<Move?> GetMoveFromMovelistByCommandAsync(
        string movename,
        List<Move> movelist
    )
    {
        var replaced = ReplaceCommandCharacters(movename.ToLower());
        var currentMove = movelist.FirstOrDefault(
            move =>
                move != null && ReplaceCommandCharacters(move.Command.ToLower()).Equals(replaced),
            null
        );

        if (currentMove == null)
        {
            currentMove = movelist.FirstOrDefault(
                move =>
                    move != null
                    && ReplaceCommandCharacters(move.Command.ToLower()).StartsWith(replaced),
                null
            );

            if (currentMove == null)
            {
                currentMove = movelist.FirstOrDefault(
                    move =>
                        move != null
                        && ReplaceCommandCharacters(move.Command.ToLower()).Contains(replaced),
                    null
                );

                if (currentMove == null)
                {
                    return Task.FromResult<Move?>(null);
                }
            }
        }

        return Task.FromResult<Move?>(currentMove);
    }

    public async Task UpdateMovesForVictorina()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);

        var allMoves = dbContext
            .TekkenMoves.Include(e => e.Character)
            .AsNoTracking()
            .AsAsyncEnumerable();
        var list = new List<Move>();
        await foreach (var move in allMoves)
        {
            if (int.TryParse(move.BlockFrame, out var frame))
            {
                list.Add(move);
            }
            else if (move.BlockFrame?.Contains('~') ?? false)
            {
                var split = move.BlockFrame.Split('~');
                if (split.All(e => int.TryParse(e, out var _)))
                {
                    list.Add(move);
                }
            }
        }

        VictorinaMoves = list;
    }

    public async Task UpdateAutocompleteDictionary()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);

        var dictionary = new Dictionary<string, Dictionary<string, Move>>();

        await foreach (
            var move in dbContext
                .TekkenMoves.OrderBy(e => e.CharacterName)
                .AsAsyncEnumerable()
                .WithCancellation(_cancellationToken)
        )
        {
            if (dictionary.TryGetValue(move.CharacterName, out var dict))
            {
                dict.Add(move.Command, move);
            }
            else
            {
                dictionary.Add(
                    move.CharacterName,
                    new Dictionary<string, Move>() { { move.Command, move } }
                );
            }
        }

        var bb = dictionary.ToDictionary(tt => tt.Key, tt => tt.Value.ToFrozenDictionary());

        AutocompleteMovesFrozenDictionary = bb.ToFrozenDictionary();
    }
}
