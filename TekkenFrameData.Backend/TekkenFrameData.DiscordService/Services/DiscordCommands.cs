using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using TekkenFrameData.Core.Protos;

namespace TekkenFrameData.DiscordService.Services;

public class DiscordCommands : ApplicationCommandModule
{
    private readonly FrameDataClient _frameDataClient;
    private readonly ILogger<DiscordCommands> _logger;

    public DiscordCommands(FrameDataClient frameDataClient, ILogger<DiscordCommands> logger)
    {
        _frameDataClient = frameDataClient;
        _logger = logger;
    }

    [SlashCommand("character", "Get information about a Tekken character")]
    public async Task CharacterCommand(InteractionContext ctx, [Option("name", "Character name")] string characterName)
    {
        await ctx.DeferAsync();

        try
        {
            var character = await _frameDataClient.GetCharacterAsync(characterName);

            if (character == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Character '{characterName}' not found."));
                return;
            }

            var moves = await _frameDataClient.GetCharacterMovesAsync(character.Id);
            var moveCount = moves.Count();

            var embed = new DiscordEmbedBuilder()
                .WithTitle(character.Name)
                .WithDescription($"Available moves: {moveCount}")
                .WithColor(DiscordColor.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (!string.IsNullOrEmpty(character.ImageUrl))
            {
                embed.WithThumbnail(character.ImageUrl);
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in character command");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("An error occurred while processing the command."));
        }
    }

    [SlashCommand("move", "Get information about a Tekken move")]
    public async Task MoveCommand(InteractionContext ctx,
        [Option("name", "Move name")] string moveName,
        [Option("character", "Character name (optional)")] string? characterName = null)
    {
        await ctx.DeferAsync();

        try
        {
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
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Move '{moveName}' not found."));
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle(move.CharacterName)
                .WithDescription(move.Input)
                .AddField("Startup", !string.IsNullOrWhiteSpace(move.StartupFrame) ? move.StartupFrame : "null", true)
                .AddField("Block", !string.IsNullOrWhiteSpace(move.BlockFrame) ? move.BlockFrame : "null", true)
                .AddField("Hit", !string.IsNullOrWhiteSpace(move.HitFrame) ? move.HitFrame : "null", true)
                .AddField("CH", !string.IsNullOrWhiteSpace(move.CounterHitFrame) ? move.CounterHitFrame : "null", true)
                .AddField("Target", !string.IsNullOrWhiteSpace(move.HitLevel) ? move.HitLevel : "null", true)
                .AddField("Dmg", !string.IsNullOrWhiteSpace(move.Damage.ToString()) ? move.Damage.ToString() : "null", true)
                .WithColor(DiscordColor.Green)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (!string.IsNullOrEmpty(move.Properties))
            {
                embed.AddField("Notes", move.Properties);
            }

            var msg = new DiscordWebhookBuilder().AddEmbed(embed);

            var buttons = new List<DiscordButtonComponent>();

            if (move.HeatEngage)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:heatengage",
                    "Heat Engager"
                );
                buttons.Add(button);
            }

            if (move.Tornado)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:tornado",
                    "Tornado"
                );
                buttons.Add(button);
            }

            if (move.HeatSmash)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    $"framedata:{move.CharacterName}:heatsmash",
                    "Heat Smash"
                );
                buttons.Add(button);
            }

            if (move.PowerCrush)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Danger,
                    $"framedata:{move.CharacterName}:powercrush",
                    "Power Crush"
                );
                buttons.Add(button);
            }

            if (move.HeatBurst)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Success,
                    $"framedata:{move.CharacterName}:heatburst",
                    "Heat Burst"
                );
                buttons.Add(button);
            }

            if (move.Homing)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:homing",
                    "Homing"
                );
                buttons.Add(button);
            }

            if (move.ThrowMove)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Primary,
                    $"framedata:{move.CharacterName}:throw",
                    "Throw"
                );
                buttons.Add(button);
            }

            if (move.IsFromStance)
            {
                var button = new DiscordButtonComponent(
                    DiscordButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:stance:{move.StanceCode}",
                    move.StanceName
                );
                buttons.Add(button);
            }

            if (buttons.Count > 0)
            {
                msg.AddActionRowComponent(buttons);
            }

            await ctx.EditResponseAsync(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in move command");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("An error occurred while processing the command."));
        }
    }

    [SlashCommand("search", "Search for Tekken moves")]
    public async Task SearchCommand(InteractionContext ctx,
        [Option("query", "Search query")] string query,
        [Option("character", "Character name (optional)")] string? characterName = null)
    {
        await ctx.DeferAsync();

        try
        {
            int characterId = 0;
            if (!string.IsNullOrEmpty(characterName))
            {
                var character = await _frameDataClient.GetCharacterAsync(characterName);
                if (character != null)
                {
                    characterId = character.Id;
                }
            }

            var moves = await _frameDataClient.SearchMovesAsync(query, characterId, 10);
            var moveList = moves.ToList();

            if (!moveList.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"No moves found for '{query}'."));
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Search Results for '{query}'")
                .WithColor(DiscordColor.Yellow)
                .WithTimestamp(DateTimeOffset.UtcNow);

            var moveDescriptions = moveList.Select(m =>
                $"**{m.CharacterName} - {m.Name}** ({m.Input}) | Startup: {m.Startup} | Damage: {m.Damage}");

            var description = string.Join("\n", moveDescriptions);
            if (description.Length > 4000)
            {
                description = description.Substring(0, 4000) + "...";
            }

            embed.WithDescription(description);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in search command");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("An error occurred while processing the command."));
        }
    }

    [SlashCommand("characters", "List all available Tekken characters")]
    public async Task CharactersCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        try
        {
            var characters = await _frameDataClient.GetCharactersAsync();
            var characterList = characters.ToList();

            if (!characterList.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("No characters available."));
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Available Characters")
                .WithColor(DiscordColor.Purple)
                .WithTimestamp(DateTimeOffset.UtcNow);

            var characterNames = string.Join(", ", characterList.Select(c => c.Name));
            if (characterNames.Length > 4000)
            {
                characterNames = characterNames.Substring(0, 4000) + "...";
            }

            embed.WithDescription(characterNames);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in characters command");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("An error occurred while processing the command."));
        }
    }
}