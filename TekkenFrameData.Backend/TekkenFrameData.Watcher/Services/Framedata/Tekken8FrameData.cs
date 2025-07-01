using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using Telegram.Bot;

namespace TekkenFrameData.Watcher.Services.Framedata;

public partial class Tekken8FrameData(
    ILogger<TekkenFrameData.Watcher.Services.Framedata.Tekken8FrameData> logger,
    IDbContextFactory<AppDbContext> dbContextFactory,
    IHostApplicationLifetime lifetime,
    ITelegramBotClient client
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                        stoppingToken
                    );
                    var character = await dbContext.TekkenCharacters.FirstOrDefaultAsync(
                        cancellationToken: stoppingToken
                    );
                    var isDateInCurrentWeek = await IsDateInCurrentWeek(
                        character?.LastUpdateTime ?? DateTime.UnixEpoch
                    );
                    if (character == null || !isDateInCurrentWeek)
                    {
                        await Task.Factory.StartNew(() => StartScrupFrameData(), stoppingToken);
                    }

                    await UpdateMovesForVictorina();
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
            },
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    public async Task<TekkenMove[]?> GetCharMoveList(string charname)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        var character = await dbContext
            .TekkenCharacters.Include(e => e.Movelist)
            .AsNoTracking()
            .FirstAsync(e => e.Name.Equals(charname), cancellationToken: _cancellationToken);

        return character.Movelist?.ToArray();
    }

    public async Task<TekkenMove?> GetMoveAsync(string[]? command)
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
                (await GetMoveFromMovelistByCommandAsync(input, movelist))
                ?? (await GetMoveFromMovelistByTagAsync(input, movelist)).move;

            return move;
        }

        return null;
    }

    private async Task<(TekkenCharacter? character, int length)> FindCharacterByNameAsync(
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

    public async Task<TekkenCharacter?> FindCharacterInDatabaseAsync(
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

    private static Task<(TekkenMoveTag tag, TekkenMove? move)> GetMoveFromMovelistByTagAsync(
        string input,
        ICollection<TekkenMove> movelist
    )
    {
        TekkenMove? move = null;

        var typeWithoutStance = Tekken8FrameData
            .MoveTags.FirstOrDefault(e =>
                e.Value.Any(b => b.Equals(input, StringComparison.InvariantCulture))
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

        return Task.FromResult((typeWithoutStance, move));
    }

    private static Task<TekkenMove?> GetMoveFromMovelistByCommandAsync(
        string movename,
        List<TekkenMove> movelist
    )
    {
        var replaced = Tekken8FrameData.ReplaceCommandCharacters(movename.ToLower());
        var currentMove = movelist.FirstOrDefault(
            move =>
                move != null
                && Tekken8FrameData
                    .ReplaceCommandCharacters(move.Command.ToLower())
                    .Equals(replaced),
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
                    return Task.FromResult<TekkenMove?>(null);
                }
            }
        }

        return Task.FromResult<TekkenMove?>(currentMove);
    }

    public async Task UpdateMovesForVictorina()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);

        var allMoves = dbContext
            .TekkenMoves.Include(e => e.Character)
            .AsNoTracking()
            .AsAsyncEnumerable();
        var list = new List<TekkenMove>();
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

    public async Task<(TekkenMoveTag Tag, TekkenMove[] Moves)?> GetMultipleMovesByTags(string input)
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
        else
        {
            var (tekkenMoveTag, move) = await GetMoveFromMovelistByTagAsync(
                lastSplit,
                characterMovelist
            );

            if (move != null)
            {
                return (Tag: tekkenMoveTag, [move]);
            }
        }

        return null;
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

    private static readonly ValueComparer<TekkenMove> StancesComparer = new(
        (e1, e2) =>
            e1 != null
            && e2 != null
            && e1.StanceCode != null
            && e2.StanceCode != null
            && e1.StanceCode.Contains(e2.StanceCode, StringComparison.OrdinalIgnoreCase),
        e => HashCode.Combine(e.StanceCode)
    );

    public async Task<IDictionary<string, string>> GetCharacterStances(
        TekkenCharacter character,
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

    public async Task<TekkenCharacter?> GetTekkenCharacter(string name, bool isWithMoveList = false)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        var result = isWithMoveList
            ? await dbContext
                .TekkenCharacters.Include(e => e.Movelist)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.Name.Equals(name),
                    cancellationToken: _cancellationToken
                )
            : await dbContext
                .TekkenCharacters.AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.Name.Equals(name),
                    cancellationToken: _cancellationToken
                );

        return result;
    }

    public async Task<TekkenMove[]?> GetCharMoveListAsync(string charname)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        var character = await dbContext
            .TekkenCharacters.Include(e => e.Movelist)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Name.Equals(charname),
                cancellationToken: _cancellationToken
            );

        return character?.Movelist.ToArray();
    }

    private static Task<(TekkenMoveTag tag, TekkenMove[])?> GetMultipleMovesFromMovelistByTagAsync(
        string input,
        ICollection<TekkenMove> movelist
    )
    {
        TekkenMove[] moves = [];

        var typeWithoutStance = MoveTags
            .FirstOrDefault(e =>
                e.Value.Any(b => b.Equals(input, StringComparison.OrdinalIgnoreCase))
            )
            .Key;

        if (typeWithoutStance == TekkenMoveTag.None)
        {
            var list = new List<TekkenMove>();
            foreach (var e in movelist)
            {
                if (
                    (e.StanceName?.Equals(input) ?? false) || (e.StanceCode?.Equals(input) ?? false)
                )
                    list.Add(e);
            }

            moves = [.. list];

            return Task.FromResult<(TekkenMoveTag tag, TekkenMove[])?>((TekkenMoveTag.None, moves));
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

        return Task.FromResult<(TekkenMoveTag tag, TekkenMove[])?>((typeWithoutStance, moves));
    }
}
