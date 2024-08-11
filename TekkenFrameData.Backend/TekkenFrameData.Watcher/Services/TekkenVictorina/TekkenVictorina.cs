using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Watcher.Services.TekkenVictorina.Entitys;
using TwitchLib.Client.Events;

namespace TekkenFrameData.Watcher.Services.TekkenVictorina;

public class TekkenVictorina(
    TekkenVictorinaLeaderbord tekkenVictorinaLeaderbord,
    TekkenMove randomMove,
    string channelName,
    string channelId,
    Action<string> sendMessage,
    Action removeVictorina
) : ITekkenVictorina
{
    private const string? CommandForStop = "!tekken_victorina_stop";
    private TekkenVictorinaGame? _currentGame;
    private static readonly TimeSpan _awaitTime = TimeSpan.FromSeconds(20);

    public string channelName { get; init; } = channelName;
    public string channelId { get; init; } = channelId;

    public async void TwitchClientOnMessageReceived(object? sender, OnMessageReceivedArgs args)
    {
        await Task.Factory.StartNew(() =>
        {
            var name = args.ChatMessage.Username;
            var message = args.ChatMessage.Message;
            var userId = args.ChatMessage.UserId;
            var channel = args.ChatMessage.Channel;

            if (
                name.Equals(TwitchClientExstension.Channel, StringComparison.OrdinalIgnoreCase)
                || !channel.Equals(channelName, StringComparison.OrdinalIgnoreCase)
            )
            {
                return;
            }

            //Стоп - слово
            if (
                message.Equals(CommandForStop, StringComparison.OrdinalIgnoreCase)
                && (args.ChatMessage.IsBroadcaster || args.ChatMessage.IsModerator)
            )
            {
                if (_currentGame is { Active: true })
                {
                    ClearGame();
                    sendMessage("Теккен викторина была остановлена!");
                }
                else
                {
                    sendMessage("Теккен викторина не была запущена.");
                }

                return;
            }

            if (_currentGame != null)
            {
                ProcessMessage(name, userId, message);
            }
        });
    }

    public async Task GameStart(string userName, string userId)
    {
        if (_currentGame is null)
        {
            var startTime = DateTime.Now;

            var prepare = $"""
                @{userName} начал(а) новую теккен викторину! Нужно назвать фреймдату на блоке для: {string.Concat(
                    randomMove.Character!.Name[0].ToString().ToUpper(),
                    randomMove.Character!.Name.AsSpan(1)
                )} {randomMove.Command} в течении {_awaitTime.TotalSeconds} секунд! Принимается ответ в формате: -14 или -14~-11 если это диапазон
                """;

            sendMessage(prepare);
            var answer = GetAnswer(randomMove);
            _currentGame = new TekkenVictorinaGame(answer);
            var token = _currentGame.CancellationTokenForRightAnswer.Token;

            while (!token.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now - startTime >= _awaitTime)
                {
                    break;
                }

                await Task.Delay(100, CancellationToken.None);
            }

            if (_currentGame.CancellationTokenForRightAnswer.IsCancellationRequested)
            {
                var rightAnswer = _currentGame.GoodAnswers.First();
                sendMessage(
                    $"У нас есть победитель в теккен викторине! Поздравляем {rightAnswer.displayName} с ответом {rightAnswer.answer}."
                );
                await tekkenVictorinaLeaderbord.AddOrUpdateUserLeaderBoard(
                    channelId,
                    userId,
                    userName
                );
                ClearGame();
            }
            else if (_currentGame.GoodAnswers.Count == 0)
            {
                sendMessage(
                    $"Никто не попытался ответить на теккен викторину. Ответ - {_currentGame.Answer}."
                );
                ClearGame();
            }
            else
            {
                if (_currentGame.GoodAnswers.Count == 1)
                {
                    var goodAnswer = _currentGame.GoodAnswers.First();
                    sendMessage(
                        $"Наиболее подходящий ответ на теккен викторине был от {goodAnswer.displayName} с текстом {goodAnswer.answer}. Ответ - {_currentGame.Answer}."
                    );
                    ClearGame();
                }
                else
                {
                    var answers = string.Join(
                        ',',
                        _currentGame.GoodAnswers.Select(e => $" {e.displayName} с {e.answer}")
                    );
                    sendMessage(
                        $"Наиболее подходящие ответы на теккен викторину: {answers}. Ответ - {_currentGame.Answer}"
                    );
                    ClearGame();
                }
            }
        }
        else
        {
            sendMessage("Теккен викторина уже используется!");
        }
    }

    public void ClearGame()
    {
        if (_currentGame != null)
        {
            _currentGame.Active = false;
        }
        _currentGame = null;
        removeVictorina();
    }

    private IntRange GetAnswer(TekkenMove tekkenMove)
    {
        if (int.TryParse(tekkenMove.BlockFrame, out var answer))
        {
            return new IntRange(answer, answer);
        }

        var split = tekkenMove.BlockFrame!.Split('~');
        if (split is { Length: 2 })
        {
            var start = int.Parse(split[0]);
            var end = int.Parse(split[1]);
            return new IntRange(start, end);
        }

        throw new Exception(
            $"Кривой инпут к удара, {tekkenMove.Character?.Name ?? tekkenMove.CharacterName} {tekkenMove.Command}"
        );
    }

    private Task ProcessMessage(string displayName, string userId, string input)
    {
        if (_currentGame is not { Active: true })
        {
            return Task.CompletedTask;
        }

        // Парсим ввод пользователя (может быть число или диапазон)
        var userRange = TryParseInput(input);
        if (!userRange.HasValue)
        {
            return Task.CompletedTask;
        }

        var answerRange = _currentGame.Answer;

        _currentGame.Users.Add(userId);

        // Проверяем пересечение диапазонов
        var isIntersect =
            userRange.Value.Start <= answerRange.End && userRange.Value.End >= answerRange.Start;

        // Вычисляем расстояние между диапазонами
        var distance = CalculateDistance(userRange.Value, answerRange);

        if (distance == 0)
        {
            AddOrUpdateGoodAnswer(displayName, userRange.Value);
            _currentGame.FoundRightAnswerTask();
            return Task.CompletedTask;
        }

        // Если диапазоны пересекаются - это точный ответ (distance = 0)
        if (isIntersect)
        {
            AddOrUpdateGoodAnswer(displayName, userRange.Value);
            return Task.CompletedTask;
        }

        // Получаем текущее минимальное расстояние
        var currentBestDistance = GetCurrentBestDistance();

        // Если список пустой, или новый ответ лучше
        if (_currentGame.GoodAnswers.Count == 0 || distance < currentBestDistance)
        {
            _currentGame.GoodAnswers.Clear();
            AddOrUpdateGoodAnswer(displayName, userRange.Value);
        }
        // Если новый ответ такой же хороший как текущие лучшие
        else if (distance == currentBestDistance)
        {
            AddOrUpdateGoodAnswer(displayName, userRange.Value);
        }

        return Task.CompletedTask;

        // Парсит строку в IntRange (число или диапазон)
        IntRange? TryParseInput(string str)
        {
            str = str.Trim();

            // Пробуем распарсить как единичное число
            if (int.TryParse(str, out var singleNumber))
            {
                return new IntRange(singleNumber, singleNumber);
            }

            // Пробуем распарсить как диапазон
            var parts = str.Split('~');
            if (
                parts.Length == 2
                && int.TryParse(parts[0].Trim(), out var start)
                && int.TryParse(parts[1].Trim(), out var end)
            )
            {
                return new IntRange(Math.Min(start, end), Math.Max(start, end));
            }

            return null;
        }
    }

    // Вычисляет минимальное расстояние между диапазонами
    private int CalculateDistance(IntRange a, IntRange b)
    {
        if (a.Start > b.End)
        {
            return a.Start - b.End;
        }

        if (b.Start > a.End)
        {
            return b.Start - a.End;
        }

        return 0; // если есть пересечение
    }

    // Добавляет или обновляет ответ пользователя
    private void AddOrUpdateGoodAnswer(string displayName, IntRange answer)
    {
        if (_currentGame != null)
        {
            var existingIndex = _currentGame.GoodAnswers.FindIndex(x =>
                x.displayName == displayName
            );
            if (existingIndex >= 0)
            {
                _currentGame.GoodAnswers[existingIndex] = (displayName, answer);
            }
            else
            {
                _currentGame.GoodAnswers.Add((displayName, answer));
            }
        }
    }

    // Получает текущее минимальное расстояние среди лучших ответов
    private int GetCurrentBestDistance()
    {
        if (_currentGame is { GoodAnswers.Count: 0 })
        {
            return int.MaxValue;
        }

        return _currentGame!.GoodAnswers.Min(x => CalculateDistance(x.answer, _currentGame.Answer));
    }
}
