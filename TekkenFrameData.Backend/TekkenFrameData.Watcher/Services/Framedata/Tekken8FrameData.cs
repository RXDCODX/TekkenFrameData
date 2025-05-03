using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(stoppingToken);
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
    }

    public async Task<TekkenMove[]?> GetCharMoveList(string charname)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            _cancellationToken
        );
        var character = await dbContext
            .TekkenCharacters.Include(e => e.Movelist)
            .AsNoTracking()
            .FirstAsync(e => e.Name.Equals(charname), cancellationToken: _cancellationToken);

        return character.Movelist?.ToArray();
    }

    public async Task<TekkenMove?> GetMoveAsync(string[]? command)
    {
        if (command == null || command.Length == 0)
            return null;

        var charnameOut = await FindCharacterByNameAsync(command);

        if (command.Length <= 2 || charnameOut is null)
        {
            return null;
        }

        var input = string.Join(" ", command.Skip(2)).ToLower();

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
                ?? await GetMoveFromMovelistByTagAsync(input, movelist);

            return move;
        }

        return null;
    }

    private async Task<TekkenCharacter?> FindCharacterByNameAsync(string[] commandParts)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            _cancellationToken
        );

        // Сначала пробуем найти по двум словам
        var charname = string.Join(" ", commandParts.Take(2));
        var character = await FindCharacterInDatabaseAsync(charname, dbContext);

        if (character != null)
            return character;

        // Если не нашли, пробуем по одному слову
        charname = string.Join(" ", commandParts.Take(1));
        return await FindCharacterInDatabaseAsync(charname, dbContext);
    }

    private async Task<TekkenCharacter?> FindCharacterInDatabaseAsync(
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
                await foreach (TekkenCharacter tekkenCharacter in characters)
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

    private static Task<TekkenMove?> GetMoveFromMovelistByTagAsync(
        string input,
        List<TekkenMove> movelist
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

            return Task.FromResult(move);
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

        return Task.FromResult(move);
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
                    && Tekken8FrameData
                        .ReplaceCommandCharacters(move.Command.ToLower())
                        .StartsWith(replaced),
                null
            );

            if (currentMove == null)
            {
                currentMove = movelist.FirstOrDefault(
                    move =>
                        move != null
                        && Tekken8FrameData
                            .ReplaceCommandCharacters(move.Command.ToLower())
                            .Contains(replaced),
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
}
