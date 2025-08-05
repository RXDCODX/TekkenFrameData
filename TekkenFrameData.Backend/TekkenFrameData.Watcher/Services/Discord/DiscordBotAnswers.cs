using System.Collections.Generic;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TekkenFrameData.Library.Exceptions;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public static class DiscordBotAnswers
{
    public static ILogger? Logger { get; set; }

    private static readonly DiscordEmbedBuilder DefaultEmbed = new()
    {
        Color = DiscordColor.Red,
        Author = new DiscordEmbedBuilder.EmbedAuthor()
        {
            IconUrl =
                "https://media.discordapp.net/attachments/1394393334474211491/1394586588004094084/download20250603195457.png?ex=68775978&is=687607f8&hm=844cba7b7cd8988058b098751ea99763876ab06351951edd578c0301a05e3795&=&format=webp&quality=lossless",
            Name = "By RXCODX",
            Url = "https://twitch.tv/rxdcodx",
        },
    };

    public static DiscordChannel? TechChannel { get; set; }

    public static async Task<string?> GetImageUrlAsync(
        DiscordClient client,
        IDbContextFactory<AppDbContext> appFactory,
        Character character
    )
    {
        if (!string.IsNullOrWhiteSpace(character.LinkToImage))
        {
            return character.LinkToImage;
        }

        string? url;

        if (character.Image is { Length: > 0 } && TechChannel != null)
        {
            var response = await client.SendMessageAsync(
                TechChannel,
                builder =>
                {
                    builder.AddFile(character.Name + ".webp", new MemoryStream(character.Image));
                }
            );
            url = response.Attachments[0].Url;

            await using var dbContext = await appFactory.CreateDbContextAsync();
            character.LinkToImage = url;
            dbContext.Update(character);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            url = character.LinkToImage;
        }

        return url;
    }

    public static async Task OnDiscordServerJoin(DiscordClient sender, GuildCreateEventArgs args)
    {
        var defaultChannel = args.Guild.GetDefaultChannel();
        var embed = new DiscordEmbedBuilder(DefaultEmbed)
            .WithTitle("Доступные команды Discord-бота")
            .WithColor(DiscordColor.Azure)
            .WithDescription(DiscordHelpFormatter.HelpText)
            .WithTimestamp(DateTime.Now);
        ;

        var msg = new DiscordMessageBuilder().AddEmbed(embed);
        try
        {
            await sender.SendMessageAsync(defaultChannel!, msg);
        }
        catch (Exception ex)
        {
            Logger?.LogException(ex);
        }
    }

    private static async Task<DiscordMessageBuilder> BuildMoveDiscordMessage(
        DiscordClient sender,
        IDbContextFactory<AppDbContext> appFactory,
        Move move
    )
    {
        var embed = new DiscordEmbedBuilder(DefaultEmbed)
        {
            Title =
                move.Character?.Name.FirstCharToUpper() ?? move.CharacterName.FirstCharToUpper(),
            Url = move.Character?.PageUrl,
            Description = move.Command,
            Timestamp = DateTime.Now,
        };

        var link = await GetImageUrlAsync(sender, appFactory, move.Character!);

        if (!string.IsNullOrWhiteSpace(link))
        {
            embed.WithThumbnail(link, 50, 50);
        }

        embed.AddField(
            "Startup",
            !string.IsNullOrWhiteSpace(move.StartUpFrame) ? move.StartUpFrame : "null",
            true
        );
        embed.AddField(
            "Block",
            !string.IsNullOrWhiteSpace(move.BlockFrame) ? move.BlockFrame : "null",
            true
        );
        embed.AddField(
            "Hit",
            !string.IsNullOrWhiteSpace(move.HitFrame) ? move.HitFrame : "null",
            true
        );
        embed.AddField(
            "CH",
            !string.IsNullOrWhiteSpace(move.CounterHitFrame) ? move.CounterHitFrame : "null",
            true
        );
        embed.AddField(
            "Target",
            !string.IsNullOrWhiteSpace(move.HitLevel) ? move.HitLevel : "null",
            true
        );
        embed.AddField("Dmg", !string.IsNullOrWhiteSpace(move.Damage) ? move.Damage : "null", true);
        embed.AddField("Notes", !string.IsNullOrWhiteSpace(move.Notes) ? move.Notes : "null");

        if (move.Character?.LinkToImage != null)
        {
            embed.WithThumbnail(move.Character.LinkToImage, 50, 50);
        }

        var msg = new DiscordMessageBuilder();
        var buttons = new List<DiscordButtonComponent>();
        if (move.HeatEngage)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:heatengage",
                    "Heat Engager"
                )
            );
        }
        if (move.Tornado)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:tornado",
                    "Tornado"
                )
            );
        }
        if (move.HeatSmash)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Primary,
                    $"framedata:{move.CharacterName}:heatsmash",
                    "Heat Smash"
                )
            );
        }
        if (move.PowerCrush)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Danger,
                    $"framedata:{move.CharacterName}:powercrush",
                    "Power Crush"
                )
            );
        }
        if (move.HeatBurst)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Success,
                    $"framedata:{move.CharacterName}:heatburst",
                    "Heat Burst"
                )
            );
        }
        if (move.Homing)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:homing",
                    "Homing"
                )
            );
        }
        if (move.Throw)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Primary,
                    $"framedata:{move.CharacterName}:throw",
                    "Throw"
                )
            );
        }
        if (move.IsFromStance)
        {
            buttons.Add(
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{move.CharacterName}:stance:{move.StanceCode}",
                    move.StanceName ?? move.StanceCode ?? "Stance"
                )
            );
        }
        if (buttons.Count > 0)
        {
            msg.AddComponents(buttons);
        }
        msg.AddEmbed(embed);
        return msg;
    }

    public static async Task FramedataCallback(
        DiscordClient sender,
        IDbContextFactory<AppDbContext> appFactory,
        Tekken8FrameData frameData,
        ComponentInteractionCreateEventArgs args
    )
    {
        var split = args.Interaction.Data.CustomId.Split(':');
        var charname = split[1];
        var type = split[2];
        var stanceCode = split.ElementAtOrDefault(3);
        var movelist = await frameData.GetCharMoveListAsync(charname);

        if (movelist is not { Length: > 0 })
        {
            throw new TekkenCharacterNotFoundException();
        }

        var character = movelist.First().Character!;

        var embed = new DiscordEmbedBuilder(DefaultEmbed)
        {
            Title = character.Name.FirstCharToUpper(),
            Url = character.PageUrl,
            Timestamp = DateTime.Now,
        };

        var link = await GetImageUrlAsync(sender, appFactory, character);

        if (!string.IsNullOrWhiteSpace(link))
        {
            embed.WithThumbnail(link, 50, 50);
        }

        var text = new StringBuilder();
        switch (type)
        {
            case "homing":
                text.AppendJoin(
                    Environment.NewLine,
                    movelist.Where(e => e.Homing).Select(e => e.Command)
                );

                embed.AddField("Homings", text.ToString());
                break;
            case "heatengage":
                text.AppendJoin(
                    Environment.NewLine,
                    movelist.Where(e => e.HeatEngage).Select(e => e.Command)
                );

                embed.AddField("Heat Engagers", text.ToString());
                break;
            case "tornado":
                text.AppendJoin(
                    Environment.NewLine,
                    movelist.Where(e => e.Tornado).Select(e => e.Command)
                );

                embed.AddField("Tornados", text.ToString());
                break;
            case "heatsmash":
                text.AppendJoin(
                    Environment.NewLine,
                    movelist.Where(e => e.HeatSmash).Select(e => e.Command)
                );

                embed.AddField("Heat Smashes", text.ToString());
                break;
            case "heatburst":
                text.AppendJoin(
                    Environment.NewLine,
                    movelist.Where(e => e.HeatBurst).Select(e => e.Command)
                );

                embed.AddField("Heat Bursts", text.ToString());
                break;
            case "powercrush":
                text.AppendJoin(
                    Environment.NewLine,
                    movelist.Where(e => e.PowerCrush).Select(e => e.Command)
                );

                embed.AddField("Power Crushes", text.ToString());
                break;
            case "throw":
                text.AppendJoin(
                    Environment.NewLine,
                    movelist.Where(e => e.Throw).Select(e => e.Command)
                );
                embed.AddField("Throws", text.ToString());
                break;
            case "stance":
                if (!string.IsNullOrWhiteSpace(stanceCode))
                {
                    text.AppendJoin(
                        Environment.NewLine,
                        movelist
                            .Where(e => e.StanceCode?.Equals(stanceCode) ?? false)
                            .Select(e => e.Command)
                    );
                    embed.AddField(stanceCode, text.ToString());
                }
                else
                {
                    text.AppendJoin(
                        Environment.NewLine,
                        movelist
                            .Where(e => !string.IsNullOrWhiteSpace(e.StanceCode))
                            .DistinctBy(e => e.StanceCode)
                            .Select(e => e.StanceCode + " - " + e.StanceName)
                    );
                    embed.AddField("Stances", text.ToString());
                }
                break;
            case "randomhigh":
                await HandleRandomMoveCase(RandomMoveType.High, sender, appFactory, movelist, args);
                return;
            case "randommid":
                await HandleRandomMoveCase(RandomMoveType.Mid, sender, appFactory, movelist, args);
                return;
            case "randomlow":
                await HandleRandomMoveCase(RandomMoveType.Low, sender, appFactory, movelist, args);
                return;
        }

        var msg = new DiscordMessageBuilder();
        msg.AddEmbed(embed);

        var inter = new DiscordInteractionResponseBuilder(msg);
        inter.AsEphemeral();

        try
        {
            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                inter
            );
        }
        catch (Exception ex)
        {
            Logger?.LogException(ex);
        }
    }

    public enum RandomMoveType
    {
        None,
        High,
        Mid,
        Low,
    }

    private static async Task HandleRandomMoveCase(
        RandomMoveType type,
        DiscordClient sender,
        IDbContextFactory<AppDbContext> appFactory,
        Move[] movelist,
        ComponentInteractionCreateEventArgs args
    )
    {
        // Определяем символ для поиска по HitLevel
        var hitLevelChar = type switch
        {
            RandomMoveType.High => 'h',
            RandomMoveType.Mid => 'm',
            RandomMoveType.Low => 'l',
            _ => 'm',
        };
        // Фильтруем только те мувы, у которых HitLevel содержит нужный символ
        var filtered = movelist
            .Where(e =>
                !string.IsNullOrWhiteSpace(e.HitLevel) && e.HitLevel!.Contains(hitLevelChar)
            )
            .ToArray();
        if (filtered.Length == 0)
        {
            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"Нет подходящих мувов для типа {type}")
                    .AsEphemeral()
            );
            return;
        }
        // Быстрый выбор случайного мува
        var randomMove = filtered[Random.Shared.Next(filtered.Length)];
        var msg = await BuildMoveDiscordMessage(sender, appFactory, randomMove);
        try
        {
            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(msg).AsEphemeral()
            );
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "{msg}", ex.Message);
        }
    }
}
