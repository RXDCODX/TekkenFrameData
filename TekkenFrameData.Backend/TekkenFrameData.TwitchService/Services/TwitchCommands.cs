using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.TwitchService.Services;

public class TwitchCommands
{
    private readonly ITwitchClient _client;
    private readonly FrameDataClient _frameDataClient;
    private readonly ILogger<TwitchCommands> _logger;

    public TwitchCommands(
        ITwitchClient client,
        FrameDataClient frameDataClient,
        ILogger<TwitchCommands> logger
    )
    {
        _client = client;
        _frameDataClient = frameDataClient;
        _logger = logger;

        SetupCommands();
    }

    private void SetupCommands()
    {
        _client.OnChatCommandReceived += OnChatCommandReceived;
    }

    private async void OnChatCommandReceived(
        object? sender,
        TwitchLib.Client.Events.OnChatCommandReceivedArgs e
    )
    {
        try
        {
            switch (e.Command.CommandText.ToLower())
            {
                case "character":
                case "char":
                    await HandleCharacterCommand(e);
                    break;
                case "move":
                    await HandleMoveCommand(e);
                    break;
                case "search":
                    await HandleSearchCommand(e);
                    break;
                case "characters":
                case "chars":
                    await HandleCharactersCommand(e);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch command: {Command}", e.Command.CommandText);
        }
    }

    private async Task HandleCharacterCommand(TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
    {
        if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
        {
            _client.SendMessage(
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.Username} Usage: !character <character_name>"
            );
            return;
        }

        var characterName = e.Command.ArgumentsAsString.Trim();
        var character = await _frameDataClient.GetCharacterAsync(characterName);

        if (character == null)
        {
            _client.SendMessage(
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.Username} Character '{characterName}' not found."
            );
            return;
        }

        var moves = await _frameDataClient.GetCharacterMovesAsync(character.Id);
        var moveCount = moves.Count();

        _client.SendMessage(
            e.Command.ChatMessage.Channel,
            $"@{e.Command.ChatMessage.Username} {character.Name} - {moveCount} moves available. Use !move <move_name> to see specific moves."
        );
    }

    private async Task HandleMoveCommand(TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
    {
        if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
        {
            _client.SendMessage(
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.Username} Usage: !move <move_name> [character_name]"
            );
            return;
        }

        var args = e.Command.ArgumentsAsString.Split(' ', 2);
        var moveName = args[0].Trim();
        var characterName = args.Length > 1 ? args[1].Trim() : string.Empty;

        TekkenMove? move = null;
        if (!string.IsNullOrEmpty(characterName))
        {
            var character = await _frameDataClient.GetCharacterAsync(characterName);
            if (character != null)
            {
                move = await _frameDataClient.GetMoveAsync(moveName, character.Id);
            }
        }
        else
        {
            move = await _frameDataClient.GetMoveAsync(moveName);
        }

        if (move == null)
        {
            _client.SendMessage(
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.Username} Move '{moveName}' not found."
            );
            return;
        }

        // Формат как в TwitchFrameData.cs
        var tags = new List<string>();
        if (move.HeatEngage)
        {
            tags.Add("Heat Engager");
        }
        if (move.Tornado)
        {
            tags.Add("Tornado");
        }
        if (move.HeatSmash)
        {
            tags.Add("Heat Smash");
        }
        if (move.PowerCrush)
        {
            tags.Add("Power Crush");
        }
        if (move.HeatBurst)
        {
            tags.Add("Heat Burst");
        }
        if (move.Homing)
        {
            tags.Add("Homing");
        }
        if (move.ThrowMove)
        {
            tags.Add("Throw");
        }

        var stanceInfo = !string.IsNullOrWhiteSpace(move.StanceCode)
            ? $" | Стойка: {move.StanceName} ({move.StanceCode})"
            : "";

        var tagsInfo = tags.Count > 0 ? $" | Теги: {string.Join(", ", tags)}" : "";

        var response = $"\u2705 {move.CharacterName} > {move.Input} \u2705 "
            + $"Старт: {move.StartupFrame} | Блок: {move.BlockFrame} | Хит: {move.HitFrame} | "
            + $"CH: {move.CounterHitFrame} | Уровень: {move.HitLevel} | Урон: {move.Damage}"
            + stanceInfo
            + tagsInfo;

        _client.SendMessage(
            e.Command.ChatMessage.Channel,
            $"@{e.Command.ChatMessage.Username} {response}"
        );
    }

    private async Task HandleSearchCommand(TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
    {
        if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
        {
            _client.SendMessage(
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.Username} Usage: !search <query> [character_name]"
            );
            return;
        }

        var args = e.Command.ArgumentsAsString.Split(' ', 2);
        var query = args[0].Trim();
        var characterName = args.Length > 1 ? args[1].Trim() : string.Empty;

        int characterId = 0;
        if (!string.IsNullOrEmpty(characterName))
        {
            var character = await _frameDataClient.GetCharacterAsync(characterName);
            if (character != null)
            {
                characterId = character.Id;
            }
        }

        var moves = await _frameDataClient.SearchMovesAsync(query, characterId, 5);
        var moveList = moves.ToList();

        if (!moveList.Any())
        {
            _client.SendMessage(
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.Username} No moves found for '{query}'."
            );
            return;
        }

        var moveNames = string.Join(", ", moveList.Select(m => $"{m.CharacterName} - {m.Name}"));
        _client.SendMessage(
            e.Command.ChatMessage.Channel,
            $"@{e.Command.ChatMessage.Username} Found: {moveNames}"
        );
    }

    private async Task HandleCharactersCommand(TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
    {
        var characters = await _frameDataClient.GetCharactersAsync();
        var characterList = characters.ToList();

        if (!characterList.Any())
        {
            _client.SendMessage(
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.Username} No characters available."
            );
            return;
        }

        var characterNames = string.Join(", ", characterList.Take(10).Select(c => c.Name));
        var message =
            characterList.Count > 10
                ? $"@{e.Command.ChatMessage.Username} Characters: {characterNames}... (and {characterList.Count - 10} more)"
                : $"@{e.Command.ChatMessage.Username} Characters: {characterNames}";

        _client.SendMessage(e.Command.ChatMessage.Channel, message);
    }
}
