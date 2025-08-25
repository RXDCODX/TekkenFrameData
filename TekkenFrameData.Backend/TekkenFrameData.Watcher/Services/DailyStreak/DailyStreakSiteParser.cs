using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using TekkenFrameData.Library.Models.DailyStreak;
using TekkenFrameData.Library.Models.DailyStreak.structures;

namespace TekkenFrameData.Watcher.Services.DailyStreak;

public class DailyStreakSiteParser(IHttpClientFactory factory)
{
    private readonly HttpClient _httpClient = factory.CreateClient();

    public async Task<WankWavuPlayer> GetWankWavuPlayerAsync(string channelTwitchId, Uri url)
    {
        var result = await _httpClient.GetAsync(url);

        if (!result.IsSuccessStatusCode)
        {
            throw new Exception("Не удалось получить страницу wank.wavu.wiki");
        }

        var htmlString = await result.Content.ReadAsStringAsync();

        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(htmlString);

        var currentNickname =
            doc.DocumentNode.SelectSingleNode("/html/body/main/div[1]/section/div[1]/div[1]")
                ?.InnerText.Trim() ?? throw new NullReferenceException();

        var oldNicknames = doc
            .DocumentNode.SelectNodes(
                "//section[contains(@class, \"card-surface\") and .//h2[contains(text(), \"Name history\")]]//tr"
            )
            ?.Select(e => e.FirstChild.InnerText)
            .Skip(1)
            .ToArray();
        var tekkenId =
            doc.DocumentNode.SelectSingleNode(
                    "/html/body/main/div[1]/section/div[1]/div[2]/span[1]/a"
                )
                ?.InnerText.Trim() ?? throw new NullReferenceException();

        var player = new WankWavuPlayer()
        {
            CurrentNickname = currentNickname,
            TwitchId = channelTwitchId,
            Nicknames = oldNicknames,
            TekkenId = TekkenId.Parse(tekkenId),
        };

        return player;
    }

    public static bool TryParseWankWavuUrl(string url, out string tekkenId)
    {
        tekkenId = string.Empty;

        // 1. Проверяем что это валидный URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // 2. Проверяем нужный домен (без учета регистра)
        if (!uri.Host.Equals("wank.wavu.wiki", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 3. Проверяем структуру пути /player/{ID}
        if (
            uri.Segments.Length < 3
            || !uri.Segments[1].Equals("player/", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        // 4. Извлекаем ID (убираем возможные слеши)
        tekkenId = uri.Segments[2].Trim('/');

        // 5. Дополнительные проверки ID (минимум 5 символов, только буквы/цифры)
        return tekkenId.Length >= 5 && Regex.IsMatch(tekkenId, @"^[a-zA-Z0-9]+$");
    }

    public async Task<WankWavuPlayerStats> GetDailyStats(WankWavuPlayer player)
    {
        var uri = new Uri(
            "https://wank.wavu.wiki/player/" + player.TekkenId.ToStringWithoutDashes()
        );

        var result = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

        if (!result.IsSuccessStatusCode)
        {
            throw new Exception("Не удалось получить страницу wank.wavu.wiki");
        }

        var htmlString = await result.Content.ReadAsStringAsync();

        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(htmlString);

        var matches =
            doc.DocumentNode.SelectNodes("//div[@class='game-list']//tr")
            ?? throw new NullReferenceException();

        var dailyDifference = new Dictionary<string, DailyStatsChanges>();
        var totalMatchesCount = 0;
        var wins = 0;
        var losses = 0;

        foreach (var match in matches)
        {
            var cells = match
                .ChildNodes.Where(node =>
                    node is { NodeType: HtmlAgilityPack.HtmlNodeType.Element, Name: "td" }
                )
                .ToList();

            if (cells.Count >= 5)
            {
                var matchStat = ParseMatchRow(cells);
                if (matchStat != null)
                {
                    if (matchStat.DateTime.Day == DateTime.Today.Day)
                    {
                        totalMatchesCount++;
                        if (matchStat.IsWin)
                        {
                            wins++;
                        }
                        else
                        {
                            losses++;
                        }

                        if (dailyDifference.TryGetValue(matchStat.Player1Character, out var value))
                        {
                            value.PtsDifference += matchStat.Player1RatingChange;
                            value.TotalMatchesCount++;
                            if (matchStat.IsWin)
                            {
                                value.Wins++;
                            }
                        }
                        else
                        {
                            dailyDifference.Add(
                                matchStat.Player1Character,
                                new DailyStatsChanges()
                                {
                                    PtsDifference = matchStat.Player1RatingChange,
                                    Wins = matchStat.IsWin ? 1 : 0,
                                    TotalMatchesCount = 1,
                                }
                            );
                        }
                    }
                }
            }
        }

        return new WankWavuPlayerStats
        {
            TwitchId = player.TwitchId,
            TotalMatches = totalMatchesCount,
            Wins = wins,
            Losses = losses,
            Date = DateTime.Today,
            StatsChanges = dailyDifference,
        };
    }

    private static MatchStat? ParseMatchRow(List<HtmlAgilityPack.HtmlNode> cells)
    {
        try
        {
            // Дата и время матча (первая ячейка)
            var dateTimeText = cells[0].SelectSingleNode(".//time")?.InnerText?.Trim();
            var matchDateTime = ParseDateTime(dateTimeText);

            // Левая сторона (игрок 1)
            var leftPlayer = ParsePlayerSide(cells[1]);

            // Результат матча
            var resultText = cells[2].InnerText?.Trim();
            var matchResult = ParseMatchResult(resultText);

            // Правая сторона (игрок 2)
            var rightPlayer = ParsePlayerSide(cells[3]);

            return new MatchStat
            {
                DateTime = matchDateTime,
                Player1Name = leftPlayer.Name,
                Player1Character = leftPlayer.Character,
                Player1Rating = leftPlayer.Rating,
                Player1RatingChange = leftPlayer.RatingChange,
                Player2Name = rightPlayer.Name,
                Player2Character = rightPlayer.Character,
                Player2Rating = rightPlayer.Rating,
                Player2RatingChange = rightPlayer.RatingChange,
                Player1Score = matchResult.Player1Score,
                Player2Score = matchResult.Player2Score,
                IsWin = matchResult.Player1Score > matchResult.Player2Score,
            };
        }
        catch (Exception ex)
        {
            // Логируем ошибку парсинга, но продолжаем обработку других строк
            Console.WriteLine($"Ошибка парсинга строки матча: {ex.Message}");
            return null;
        }
    }

    private static PlayerMatchInfo ParsePlayerSide(HtmlAgilityPack.HtmlNode cell)
    {
        var playerName = cell.SelectSingleNode(".//span[@class='player']//a")?.InnerText?.Trim();
        var character = cell.SelectSingleNode(".//span[@class='char']")?.InnerText?.Trim();
        var ratingSpan = cell.SelectSingleNode(".//span[@class='rating']");
        var ratingText = ratingSpan?.InnerText?.Trim();
        var ratingChangeText = ratingSpan?.SelectSingleNode($".//span")?.InnerText?.Trim();

        var rating = ParseRating(ratingText);
        var ratingChange = ParseRatingChange(ratingChangeText);

        return new PlayerMatchInfo
        {
            Name = playerName ?? "Unknown",
            Character = character ?? "Unknown",
            Rating = rating,
            RatingChange = ratingChange,
        };
    }

    private static DateTime ParseDateTime(string? dateTimeText)
    {
        if (string.IsNullOrWhiteSpace(dateTimeText))
        {
            return DateTime.Now;
        }

        // Удаляем лишние пробелы и "г.", если есть
        dateTimeText = dateTimeText
            .Trim()
            .Replace("  ", " ") // Заменяем двойные пробелы на одинарные
            .Replace("г.,", ""); // Удаляем "г.," для упрощения парсинга

        // Форматы для en-US (английская культура)
        var enUsFormats = new[]
        {
            "d MMM yy H:mm", // 1 Jul 25 9:46
            "d MMM yy HH:mm", // 1 Jul 25 09:46
            "dd MMM yyyy H:mm", // 01 Jul 2025 9:46
            "MMMM d, yyyy H:mm", // July 1, 2025 9:46
        };

        // Форматы для ru-RU (русская культура)
        var ruRuFormats = new[]
        {
            "d MMM yy H:mm", // 1 июл 25 9:46
            "dd.MM.yyyy H:mm", // 01.07.2025 9:46
            "d MMM yyyy H:mm", // 1 июл 2025 9:46
        };

        // Сначала пробуем en-US (так как "Jul" — английский)
        if (
            DateTime.TryParseExact(
                dateTimeText,
                enUsFormats,
                CultureInfo.GetCultureInfo("en-US"),
                DateTimeStyles.None,
                out var enUsResult
            )
        )
        {
            return enUsResult;
        }

        // Потом пробуем ru-RU (на случай, если месяц русский)
        if (
            DateTime.TryParseExact(
                dateTimeText,
                ruRuFormats,
                CultureInfo.GetCultureInfo("ru-RU"),
                DateTimeStyles.None,
                out var ruRuResult
            )
        )
        {
            return ruRuResult;
        }

        // Fallback 1: Стандартный парсинг (на случай нестандартного, но валидного формата)
        if (
            DateTime.TryParse(
                dateTimeText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fallbackResult
            )
        )
        {
            return fallbackResult;
        }

        // Fallback 2: Возвращаем текущее время (если вообще не распарсилось)
        return DateTime.Now;
    }

    private static MatchResult ParseMatchResult(string? resultText)
    {
        if (string.IsNullOrWhiteSpace(resultText))
        {
            return new MatchResult { Player1Score = 0, Player2Score = 0 };
        }

        var parts = resultText.Split('-');
        if (parts.Length == 2)
        {
            if (
                int.TryParse(parts[0].Trim(), out var score1)
                && int.TryParse(parts[1].Trim(), out var score2)
            )
            {
                return new MatchResult { Player1Score = score1, Player2Score = score2 };
            }
        }

        return new MatchResult { Player1Score = 0, Player2Score = 0 };
    }

    private static int ParseRating(string? ratingText)
    {
        if (string.IsNullOrWhiteSpace(ratingText))
        {
            return 0;
        }

        var cleanText = new string([.. ratingText.Where(char.IsDigit)]);
        return int.TryParse(cleanText, out var rating) ? rating : 0;
    }

    private static int ParseRatingChange(string? changeText)
    {
        if (string.IsNullOrWhiteSpace(changeText))
        {
            return 0;
        }

        var cleanText = changeText.Trim();
        var isNegative = cleanText.StartsWith('-');
        var digits = new string([.. cleanText.Where(char.IsDigit)]);

        return int.TryParse(digits, out var change)
            ? isNegative
                ? -change
                : change
            : 0;
    }
}

// Вспомогательные классы для парсинга
public class PlayerMatchInfo
{
    public string Name { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public int Rating { get; set; }
    public int RatingChange { get; set; }
}

public class MatchResult
{
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
}
