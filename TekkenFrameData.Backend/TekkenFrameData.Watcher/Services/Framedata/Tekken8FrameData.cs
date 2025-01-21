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
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TekkenFrameData.Watcher.Services.Framedata;

public class Tekken8FrameData 
{
    private static readonly KeyValuePair<string, string> _defaultValuePair = new(string.Empty, string.Empty);

    private readonly Uri _basePath = new("https://tekkendocs.com");
    private readonly ILogger<Tekken8FrameData> _logger;

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    private readonly object _lock = new object();

    public Tekken8FrameData(ILogger<Tekken8FrameData> logger, IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;

        Init();
        GC.Collect(0, GCCollectionMode.Forced);
    }

    private bool IsDateInCurrentWeek(DateTime date)
    {
        var currentDate = DateTime.Now;

        // Определяем первый день текущей недели (предполагается, что неделя начинается с понедельника)
        var daysToSubtract = (int)currentDate.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToSubtract < 0) daysToSubtract += 7; // Если сегодня воскресенье (DayOfWeek.Sunday = 0)

        var startOfWeek = currentDate.AddDays(-daysToSubtract).Date;

        // Определяем последний день текущей недели
        var endOfWeek = startOfWeek.AddDays(7);

        // Сравниваем дату с началом и концом недели
        return date >= startOfWeek && date <= endOfWeek;
    }

    internal async void Init()
    {
        var docW = new HtmlWeb();
        var doc = docW.Load(_basePath.AbsoluteUri);

        var ulNode = doc.DocumentNode.SelectSingleNode("//ul");

        if (ulNode != null)
        {
            IEnumerable<HtmlNode> liNodes = ulNode.SelectNodes(".//li[@class='cursor-pointer']");

            if (liNodes != null)
            {
                foreach (var liNode in liNodes)
                {
                    var aNode = liNode.SelectSingleNode(".//a[@class='cursor-pointer']");
                    var href = aNode.GetAttributeValue("href", "");

                    var nameNode = liNode.SelectSingleNode(
                        ".//div[@class='overflow-hidden text-ellipsis whitespace-nowrap text-center capitalize text-text-primary max-xs:text-xs']");
                    var name = nameNode.InnerText.Trim();

                    var imgNode = liNode.SelectSingleNode(".//img");
                    var imageUrl = imgNode.GetAttributeValue("src", "");
                    var imagePath = new Uri(_basePath, imageUrl);

                    var character = new TekkenCharacter
                    {
                        LinkToImage = imagePath.AbsoluteUri,
                        Name = name
                    };

                    try
                    {
                        var movelist = GetMoveList(character, "https://tekkendocs.com" + href);

                        var sortedMovelist = ConsolidateMoveGroups(movelist);

                        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                        {
                            foreach (var move in sortedMovelist)
                            {
                                var dbmove = dbContext.TekkenMoves.Find(move.CharacterName, move.Command);
                                if (dbmove?.IsUserChanged == true)
                                {
                                    continue;
                                }

                                if (dbmove != null)
                                {
                                    dbContext.TekkenMoves.Update(move);
                                }
                                else
                                {
                                    dbContext.TekkenMoves.Add(move);
                                }
                            }


                            if (dbContext.TekkenCharacters.Any(e => e.Equals(character)))
                            {
                                dbContext.TekkenCharacters.Update(character);
                            }
                            else
                            {
                                dbContext.TekkenCharacters.Add(character);
                            }


                            await dbContext.SaveChangesAsync();
                        }

                        movelist.Clear();
                        sortedMovelist.Clear();
                        movelist = null;
                        sortedMovelist = null;
                        character = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical("Не удалось добавить персонажа - {0}. Ошибка - {1} # {2}", name, ex.Message, ex.StackTrace);
                    }
                }
            }
            else
                _logger.LogError("Не удалось найти узлы li.");
        }
        else
            _logger.LogError("Не удалось найти узел ul.");
    }

    private List<TekkenMove> GetMoveList(TekkenCharacter character, string url)
    {
        var movelist = new List<TekkenMove>();

        // Загрузка HTML страницы
        var web = new HtmlWeb();
        var doc = web.Load(url);

        // Находим таблицу с мувлистом
        var tableNode = doc.DocumentNode.SelectSingleNode("//tbody");

        // Проверяем, что таблица найдена
        if (tableNode != null)
        {
            IEnumerable<HtmlNode> rowNodes = tableNode.SelectNodes(".//tr[@class='rt-TableRow']");

            // Проверяем, что строки таблицы найдены
            if (rowNodes != null)
                // Перебираем каждую строку таблицы
                foreach (var rowNode in rowNodes)
                {
                    // Создаем новый объект Move
                    var move = new TekkenMove
                    {
                        CharacterName = character.Name
                    };

                    //character.Movelist = movelist; !important

                    // Получаем ячейки (столбцы) текущей строки
                    var cellNodes = rowNode.SelectNodes(".//td[@class='rt-TableCell']");

                    // Извлекаем текст из тега <a> в ячейке command
                    move.Command = rowNode.SelectSingleNode(".//a[@data-discover='true']")?.InnerText.Trim().ToLower() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(move.Command))
                    {
                        continue;
                    }

                    move.Command = move.Command.Replace(".", " ");

                    var noteDivs = cellNodes[7].SelectNodes(".//div");
                    if (noteDivs != null &&
                        noteDivs.Count > 0) move.Notes = string.Join(Environment.NewLine, noteDivs.Select(div => div.InnerText.Trim().ToLower()));

                    // Заполняем остальные свойства объекта Move данными из остальных ячеек
                    move.HitLevel = cellNodes[1].InnerText.Trim().ToLower();
                    move.Damage = cellNodes[2].InnerText.Trim().ToLower();
                    move.StartUpFrame = cellNodes[3].InnerText.Trim().ToLower();
                    move.BlockFrame = cellNodes[4].InnerText.Trim().ToLower();
                    move.HitFrame = cellNodes[5].InnerText.Trim().ToLower();
                    move.CounterHitFrame = cellNodes[6].InnerText.Trim().ToLower();

                    var notes = move.Notes;
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        if (notes.Contains("power crush")) move.PowerCrush = true;

                        if (notes.Contains("heat burst")) move.HeatBurst = true;

                        if (notes.Contains("heat engager")) move.HeatEngage = true;

                        if (notes.Contains("heat smash")) move.HeatSmash = true;

                        if (move.Command.StartsWith("h")) move.RequiresHeat = true;

                        if (notes.Contains("tornado")) move.Tornado = true;

                        if (notes.Contains("homing")) move.Homing = true;
                    }

                    if (move.HitLevel.Contains("th") ||
                        move.HitLevel.Contains("t")) move.Throw = true;

                    var pair = Aliases.Stances.FirstOrDefault(e => move.Command.StartsWith(e.Key), _defaultValuePair);

                    if (!string.IsNullOrWhiteSpace(pair.Key))
                    {
                        move.StanceCode = pair.Key;
                        move.StanceName = pair.Value;
                    }

                    // Добавляем объект Move в список
                    movelist.Add(move);
                }
            else
                _logger.LogError("Не удалось найти строки таблицы. Персонаж - {0}", character.Name);
        }
        else
            _logger.LogError("Не удалось найти строки таблицы. Персонаж - {0}", character.Name);

        return movelist;
    }

    public static List<TekkenMove> ConsolidateMoveGroups(List<TekkenMove> moves)
    {
        var groupedMoves = moves
            .GroupBy(m => new { m.CharacterName, m.Command });

        var consolidatedMoves = new List<TekkenMove>();
        var uniqueMoves = new List<TekkenMove>();

        foreach (var group in groupedMoves)
            if (group.Count() > 1)
            {
                // Consolidate the duplicate moves
                var consolidatedMove = new TekkenMove
                {
                    CharacterName = group.Key.CharacterName,
                    Command = group.Key.Command,
                    HeatEngage = group.Any(m => m.HeatEngage),
                    HeatSmash = group.Any(m => m.HeatSmash),
                    PowerCrush = group.Any(m => m.PowerCrush),
                    Throw = group.Any(m => m.Throw),
                    Homing = group.Any(m => m.Homing),
                    Tornado = group.Any(m => m.Tornado),
                    HeatBurst = group.Any(m => m.HeatBurst),
                    RequiresHeat = group.Any(m => m.RequiresHeat),
                    StanceCode = group.First().StanceCode,
                    HitLevel = group.First().HitLevel,
                    Damage = group.First().Damage,
                    StartUpFrame = group.First().StartUpFrame,
                    BlockFrame = group.First().BlockFrame,
                    HitFrame = group.First().HitFrame,
                    CounterHitFrame = group.First().CounterHitFrame,
                    Notes = string.Join(Environment.NewLine, group.Select(m => m.Notes))
                };

                consolidatedMoves.Add(consolidatedMove);
            }
            else
                // Add unique move to the list of unique moves
                uniqueMoves.Add(group.First());

        // Combine unique moves and consolidated moves into an array
        var result = new TekkenMove[uniqueMoves.Count + consolidatedMoves.Count];
        uniqueMoves.CopyTo(result, 0);
        consolidatedMoves.CopyTo(result, uniqueMoves.Count);

        return result.ToList();
    }
    public async Task HandAlert(ITelegramBotClient client, Update update)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            var data = update.CallbackQuery;
            var split = data.Data.Split(':');

            if (split[0].Equals("framedata"))
            {
                var charname = split[1];
                var type = split[2];

                var movelist = GetCharMoveList(charname);

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

                var chatid = new ChatId(data.Message.Chat.Id);

                await client.AnswerCallbackQueryAsync(data.Id);
                await client.SendTextMessageAsync(chatid, text.ToString(), parseMode: ParseMode.Html);
            }
        }
    }

    public IEnumerable<TekkenMove>? GetCharMoveList(string charname)
    {
        var func = new Func<TekkenCharacter, bool>(e => e.Name.Equals(charname));

        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.TekkenCharacters.Include(e => e.Movelist).AsNoTracking().FirstOrDefault(func, null)?.Movelist.ToList();
    }

    public TekkenMove? GetMove(string[] command)
    {
        var charnameOut = new TekkenCharacter();

        var length = 2;

        var charname = string.Join(" ", command.Take(length));

        var isCharFounded = false;

        foreach (var aliasPair in Aliases.CharacterNameAliases)
            if (aliasPair.Key.Equals(charname) ||
                aliasPair.Value.Any(e => e.Equals(charname)))
            {
                var character = aliasPair.Key;

                using var dbContext = _dbContextFactory.CreateDbContext();
                var characters = dbContext.TekkenCharacters;
                foreach (var tekkenCharacter in characters)
                    if (tekkenCharacter.Name.Equals(character))
                    {
                        charnameOut = tekkenCharacter;
                        isCharFounded = true;
                        break;
                    }
            }


        if (!isCharFounded)
        {
            length--;
            charname = string.Join(" ", command.Take(length));

            foreach (var pair in Aliases.CharacterNameAliases)
                if (pair.Key.Equals(charname) ||
                    pair.Value.Any(e => e.Equals(charname)))
                {
                    var character = pair.Key;

                    using var dbContext = _dbContextFactory.CreateDbContext();
                    var characters = dbContext.TekkenCharacters;
                    foreach (var pairCharacter in characters)
                        if (pairCharacter.Name.Equals(character))
                        {
                            charnameOut = pairCharacter;
                            break;
                        }
                }
        }

        if (command.Length - length == 0) return null;

        var input = string.Join(" ", command.Skip(length)).ToLower();

        if (string.IsNullOrWhiteSpace(charnameOut.Name) ||
            string.IsNullOrWhiteSpace(input)) return null;

        using (var context = _dbContextFactory.CreateDbContext())
        {
            var movelist = context.TekkenMoves.Where(e => e.Character == charnameOut).Include(e => e.Character).ToList();

            if (movelist is { Count: > 0 })
            {
                var move = GetMoveFromMovelistByCommand(input, movelist);

                if (move is null)
                {
                    move = GetMoveFromMovelistByTag(input, movelist);
                }

                return move;
            }

            return null;
        }
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

    private TekkenMove? GetMoveFromMovelistByTag(string input, List<TekkenMove> movelist)
    {
        TekkenMove? move = null;

        var typeWithoutStance = MoveTags.FirstOrDefault(e => e.Value.Any(e => e.Equals(input, StringComparison.InvariantCulture))).Key;

        if (typeWithoutStance == TekkenMoveTag.None)
        {
            move = movelist.FirstOrDefault(e => e.StanceName.Equals(input) || e.StanceCode.Equals(input));

            return move;
        }


        switch (typeWithoutStance)
        {
            case TekkenMoveTag.HeatBurst:
                move = movelist.LastOrDefault(e => e.HeatBurst, null);
                break;
            case TekkenMoveTag.HeatEngage:
                move = movelist.LastOrDefault(e => e.HeatEngage, null);
                break;
            case TekkenMoveTag.HeatSmash:
                move = movelist.LastOrDefault(e => e.HeatSmash, null);
                break;
            case TekkenMoveTag.Homing:
                move = movelist.LastOrDefault(e => e.Homing, null);
                break;
            case TekkenMoveTag.PowerCrush:
                move = movelist.LastOrDefault(e => e.PowerCrush);
                break;
            case TekkenMoveTag.Throw:
                move = movelist.LastOrDefault(e => e.Throw, null);
                break;
            case TekkenMoveTag.Tornado:
                move = movelist.LastOrDefault(e => e.Tornado, null);
                break;
        }

        return move;
    }

    private TekkenMove? GetMoveFromMovelistByCommand(string movename, IEnumerable<TekkenMove> movelist)
    {
        var tekkenMoves = movelist as TekkenMove[] ?? movelist.ToArray();
        var currentMove = GetMoveFromMovelistByCommandWithoutReplace(movename, tekkenMoves);

        if (currentMove == null)
        {
            var replaced = ReplaceCommandCharacters(movename.ToLower());
            currentMove = tekkenMoves
                .FirstOrDefault(move =>
                    ReplaceCommandCharacters(move.Command.ToLower()).Equals(replaced), null);

            if (currentMove == null)
            {
                currentMove = tekkenMoves.FirstOrDefault(move => ReplaceCommandCharacters(move.Command.ToLower())
                    .StartsWith(replaced), null);

                if (currentMove == null)
                {
                    currentMove = tekkenMoves.FirstOrDefault(move => ReplaceCommandCharacters(move.Command.ToLower())
                        .Contains(replaced), null);

                    if (currentMove == null)
                        return null;
                }
            }
        }

        return currentMove;
    }

    private TekkenMove? GetMoveFromMovelistByCommandWithoutReplace(string movename, IEnumerable<TekkenMove> movelist)
    {
        var enumerable = movelist as TekkenMove[] ?? movelist.ToArray();
        var currentMove = enumerable
            .FirstOrDefault(move => move.Command.ToLower().Equals(movename), null);

        if (currentMove == null)
        {
            currentMove = enumerable.FirstOrDefault(move => move.Command.ToLower().StartsWith(movename), null);

            if (currentMove == null)
            {
                currentMove = enumerable.FirstOrDefault(move => move.Command.ToLower().Contains(movename), null);

                if (currentMove == null)
                    return null;
            }
        }

        return currentMove;
    }

    private string ReplaceCommandCharacters(string command)
    {
        if (string.IsNullOrEmpty(command)) return string.Empty;

        foreach (var r in Aliases.MoveInputReplacer) command = command.Replace(r.Key, r.Value);

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

        if (!string.IsNullOrWhiteSpace(move.StanceCode)) tags.Add(move.StanceName);

        return string.Join(",", tags);
    }
}