using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Watcher.DB;
using TekkenFrameData.Watcher.Domains.FrameData;
using TekkenFrameData.Watcher.Exstensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TekkenFrameData.Watcher.Services.Framedata;

public class Tekken8FrameData(ILogger<Tekken8FrameData> logger, IDbContextFactory<AppDbContext> dbContextFactory)
{
    // TODO: https://github.com/RXDCODX/TekkenFrameData/issues/8

    public async Task HandAlert(ITelegramBotClient client, Update update)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            try
            {
                var data = update.CallbackQuery;

                if (data?.Data == null)
                {
                    throw new NullReferenceException();
                }

                var split = data.Data.Split(':');

                if (split[0].Equals("framedata"))
                {
                    var charname = split[1];
                    var type = split[2];

                    var movelist = GetCharMoveList(charname);

                    ArgumentNullException.ThrowIfNull(movelist);

                    var text = new StringBuilder();
                    switch (type)
                    {
                        case "homing":
                            text.AppendLine("<b>Homings</b>");
                            text.AppendLine();

                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.Homing).Select(e => e.Command));
                            break;
                        case "heatengage":
                            text.AppendLine("<b>Heat Engagers</b>");
                            text.AppendLine();

                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.HeatEngage).Select(e => e.Command));
                            break;
                        case "tornado":
                            text.AppendLine("<b>Tornados</b>");
                            text.AppendLine();

                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.Tornado).Select(e => e.Command));
                            break;
                        case "heatsmash":
                            text.AppendLine("<b>Heat Smashes</b>");
                            text.AppendLine();

                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.HeatSmash).Select(e => e.Command));
                            break;
                        case "heatburst":
                            text.AppendLine("<b>Heat Bursts</b>");
                            text.AppendLine();

                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.HeatBurst).Select(e => e.Command));
                            break;
                        case "powercrush":
                            text.AppendLine("<b>Power Crushes</b>");
                            text.AppendLine();
                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.PowerCrush).Select(e => e.Command));
                            break;
                        case "stance":
                            var stanceCode = split[3];
                            var pair = Aliases.Stances.First(e => e.Key == stanceCode);
                            text.AppendLine($"<b>{pair.Value}</b>");
                            text.AppendLine();
                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.StanceCode == stanceCode).Select(e => e.Command));
                            break;
                        case "throw":
                            text.AppendLine("<b>Throws</b>");
                            text.AppendLine();
                            text.AppendJoin(Environment.NewLine, movelist.Where(e => e.Throw).Select(e => e.Command));
                            break;
                    }

                    if (data.Message?.Chat.Id == null)
                    {
                        throw new NullReferenceException();
                    }

                    await client.AnswerCallbackQuery(data.Id);
                    await client.SendMessage(data.Message.Chat.Id, text.ToString(), parseMode: ParseMode.Html);
                }
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }
    }

    public IEnumerable<TekkenMove>? GetCharMoveList(string charname)
    {
        var func = new Func<TekkenCharacter?, bool>(e => e?.Name?.Equals(charname) ?? false);

        using var dbContext = dbContextFactory.CreateDbContext();
        return dbContext.TekkenCharacters.Include(e => e.Movelist).AsNoTracking().FirstOrDefault(func, null)?.Movelist.ToList();
    }

    public TekkenMove? GetMove(string[] command)
    {
        TekkenCharacter? charnameOut = null;

        var length = 2;

        var charname = string.Join(" ", command.Take(length));

        var isCharFounded = false;

        foreach (var aliasPair in Aliases.CharacterNameAliases)
        {
            if (aliasPair.Key.Equals(charname) ||
                aliasPair.Value.Any(e => e.Equals(charname)))
            {
                var character = aliasPair.Key;

                using var dbContext = dbContextFactory.CreateDbContext();
                var characters = dbContext.TekkenCharacters;
                foreach (var tekkenCharacter in characters)
                {
                    if (tekkenCharacter.Name.Equals(character))
                    {
                        charnameOut = tekkenCharacter;
                        isCharFounded = true;
                        break;
                    }
                }
            }
        }


        if (!isCharFounded)
        {
            length--;
            charname = string.Join(" ", command.Take(length));

            foreach (var pair in Aliases.CharacterNameAliases)
            {
                if (pair.Key.Equals(charname) ||
                    pair.Value.Any(e => e.Equals(charname)))
                {
                    var character = pair.Key;

                    using var dbContext = dbContextFactory.CreateDbContext();
                    var characters = dbContext.TekkenCharacters;
                    foreach (var pairCharacter in characters)
                    {
                        if (pairCharacter.Name.Equals(character))
                        {
                            charnameOut = pairCharacter;
                            break;
                        }
                    }
                }
            }
        }

        if (command.Length - length == 0) return null;

        var input = string.Join(" ", command.Skip(length)).ToLower();

        if (string.IsNullOrWhiteSpace(charnameOut?.Name) ||
            string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        using var context = dbContextFactory.CreateDbContext();
        var movelist = context.TekkenMoves.Where(e => e.Character == charnameOut).Include(e => e.Character).ToList();

        if (movelist is { Count: > 0 })
        {
            var move = GetMoveFromMovelistByCommand(input, movelist) ?? GetMoveFromMovelistByTag(input, movelist);

            return move;
        }

        return null;
    }

    private enum TekkenMoveTag : byte
    {
        None,
        HeatEngage,
        HeatSmash,
        PowerCrush,
        Throw,
        Homing,
        Tornado,
        HeatBurst,
    }

    private static readonly Dictionary<TekkenMoveTag, string[]> MoveTags = new()
    {
        { TekkenMoveTag.HeatEngage, new[] { "engage", "enga", "enggg", "engg", "heatengage", "heatengagage", "he" } },
        { TekkenMoveTag.HeatSmash, new[] { "smash", "heatsmash", "smsh", "heatsmsh", "hs" } },
        { TekkenMoveTag.PowerCrush, new[] { "crush", "powercrush", "pc", "power", "armor", "armori", "power_crush", "power crush" } },
        { TekkenMoveTag.Throw, new[] { "throw", "throws", "throwbrow", "grab", "grabs" } },
        { TekkenMoveTag.Homing, new [] { "homing", "homari" } },
        { TekkenMoveTag.Tornado, new [] { "tornado", "trnd", "wind", "taifun", "ts", "tail_spin", "tailspin", "screw", "s!", "s", "screws" } },
        { TekkenMoveTag.HeatBurst, new [] { "hb", "heatburst", "heat burst", "hear_burst", "burst" } }
    };

    private static TekkenMove? GetMoveFromMovelistByTag(string input, List<TekkenMove> movelist)
    {
        TekkenMove? move = null;

        var typeWithoutStance = MoveTags.FirstOrDefault(e => e.Value.Any(e => e.Equals(input, StringComparison.InvariantCulture))).Key;

        if (typeWithoutStance == TekkenMoveTag.None)
        {
            move = movelist.FirstOrDefault(e => (e.StanceName?.Equals(input) ?? false) || (e.StanceCode?.Equals(input) ?? false));

            return move;
        }


        switch (typeWithoutStance)
        {
            case TekkenMoveTag.HeatBurst:
                move = movelist.LastOrDefault(e => e.HeatBurst);
                break;
            case TekkenMoveTag.HeatEngage:
                move = movelist.LastOrDefault(e => e.HeatEngage);
                break;
            case TekkenMoveTag.HeatSmash:
                move = movelist.LastOrDefault(e => e.HeatSmash);
                break;
            case TekkenMoveTag.Homing:
                move = movelist.LastOrDefault(e => e.Homing);
                break;
            case TekkenMoveTag.PowerCrush:
                move = movelist.LastOrDefault(e => e.PowerCrush);
                break;
            case TekkenMoveTag.Throw:
                move = movelist.LastOrDefault(e => e.Throw);
                break;
            case TekkenMoveTag.Tornado:
                move = movelist.LastOrDefault(e => e.Tornado);
                break;
        }

        return move;
    }

    private static TekkenMove? GetMoveFromMovelistByCommand(string movename, IEnumerable<TekkenMove> movelist)
    {
        var tekkenMoves = movelist as TekkenMove[] ?? movelist.ToArray();
        var currentMove = GetMoveFromMovelistByCommandWithoutReplace(movename, tekkenMoves);

        if (currentMove is null)
        {
            var replaced = ReplaceCommandCharacters(movename.ToLower());
            currentMove = tekkenMoves
                .FirstOrDefault(move =>
                    ReplaceCommandCharacters(move.Command.ToLower()).Equals(replaced));

            if (currentMove == null)
            {
                currentMove = tekkenMoves.FirstOrDefault(move => ReplaceCommandCharacters(move.Command.ToLower())
                    .StartsWith(replaced));

                if (currentMove == null)
                {
                    currentMove = tekkenMoves.FirstOrDefault(move => ReplaceCommandCharacters(move.Command.ToLower())
                        .Contains(replaced));

                    if (currentMove == null)
                        return null;
                }
            }
        }

        return currentMove;
    }

    private static TekkenMove? GetMoveFromMovelistByCommandWithoutReplace(string movename, IEnumerable<TekkenMove> movelist)
    {
        var enumerable = movelist as TekkenMove[] ?? movelist.ToArray();
        var currentMove = enumerable
            .FirstOrDefault(move => move.Command.ToLower().Equals(movename));

        if (currentMove == null)
        {
            currentMove = enumerable.FirstOrDefault(move => move.Command.StartsWith(movename, StringComparison.OrdinalIgnoreCase));

            if (currentMove == null)
            {
                currentMove = enumerable.FirstOrDefault(move => move.Command.Contains(movename, StringComparison.OrdinalIgnoreCase));

                if (currentMove == null)
                    return null;
            }
        }

        return currentMove;
    }

    private static string ReplaceCommandCharacters(string command)
    {
        if (string.IsNullOrEmpty(command)) return string.Empty;

        foreach (var r in Aliases.MoveInputReplacer)
        {
            command = command.Replace(r.Key, r.Value);
        }

        return command;
    }

    public string GetMoveTags(TekkenMove move)
    {
        var tags = new List<string>();

        if (move.Tornado) tags.Add("Tornado");

        if (move.HeatEngage) tags.Add("Heat Engage");

        if (move.HeatSmash) tags.Add("Heat Smash");

        if (move.PowerCrush) tags.Add("Power crush");

        if (move.Homing) tags.Add("Homing");

        if (move.RequiresHeat) tags.Add("Heat");

        if (move.Throw) tags.Add("Throw");

        if (!string.IsNullOrWhiteSpace(move.StanceCode) &&
            !string.IsNullOrWhiteSpace(move.StanceName))
        {
            tags.Add(move.StanceName);
        }

        return string.Join(",", tags);
    }
}