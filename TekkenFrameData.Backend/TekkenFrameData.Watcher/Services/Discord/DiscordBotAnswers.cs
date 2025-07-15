using System.Collections.Generic;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TekkenFrameData.Library.Exceptions;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Discord;
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

    private static readonly Dictionary<string, string?> _imageUrlCache = new();

    public static async Task<string?> GetImageUrlAsync(DiscordClient client, Character character)
    {
        if (_imageUrlCache.TryGetValue(character.Name, out var cachedUrl))
            return cachedUrl;

        string? url = null;

        if (character.Image is { Length: > 0 } && TechChannel != null)
        {
            var response = await client.SendMessageAsync(
                TechChannel,
                builder =>
                {
                    builder.AddFile(character.Name + ".webp", new MemoryStream(character.Image));
                }
            );
            url = response.Attachments.First().Url;
        }
        else
        {
            url = character.LinkToImage;
        }

        _imageUrlCache[character.Name] = url;
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
            Logger?.LogError(ex, ex.Message);
        }
    }

    // TODO: Переделать под вызовы мувов с тегом и прочее
    public static async Task FramedataRequest(
        DiscordClient sender,
        Tekken8FrameData frameData,
        MessageCreateEventArgs args
    )
    {
        var textMsg = args.Message.Content;

        var split = textMsg.Split(' ');

        if (split.Length >= 3)
        {
            split = [.. split.Skip(1)];
            var move = await frameData.GetMoveAsync(split);

            if (move is { })
            {
                var embed = new DiscordEmbedBuilder(DefaultEmbed)
                {
                    Title = move.Character!.Name.FirstCharToUpper(),
                    Url = move.Character!.PageUrl,
                    Description = move.Command,
                    Timestamp = DateTime.Now,
                };

                var link = await GetImageUrlAsync(sender, move.Character!);

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
                    !string.IsNullOrWhiteSpace(move.CounterHitFrame)
                        ? move.CounterHitFrame
                        : "null",
                    true
                );
                embed.AddField(
                    "Target",
                    !string.IsNullOrWhiteSpace(move.HitLevel) ? move.HitLevel : "null",
                    true
                );
                embed.AddField(
                    "Dmg",
                    !string.IsNullOrWhiteSpace(move.Damage) ? move.Damage : "null",
                    true
                );
                embed.AddField(
                    "Notes",
                    !string.IsNullOrWhiteSpace(move.Notes) ? move.Notes : "null"
                );

                if (move.Character.LinkToImage != null)
                {
                    embed.WithThumbnail(move.Character.LinkToImage, 50, 50);
                }

                var msg = new DiscordMessageBuilder();

                var buttons = new List<DiscordButtonComponent>();
                // TODO: Добавить выход на стойки и захваты
                if (move.HeatEngage)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:heatengage",
                        "Heat Engager"
                    );
                    buttons.Add(button);
                }

                if (move.Tornado)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:tornado",
                        "Tornado"
                    );
                    buttons.Add(button);
                }

                if (move.HeatSmash)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{move.Character.Name}:heatsmash",
                        "Heat Smash"
                    );
                    buttons.Add(button);
                }

                if (move.PowerCrush)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Danger,
                        $"framedata:{move.Character.Name}:powercrush",
                        "Power Crush"
                    );
                    buttons.Add(button);
                }

                if (move.HeatBurst)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Success,
                        $"framedata:{move.Character.Name}:heatburst",
                        "Heat Burst"
                    );
                    buttons.Add(button);
                }

                if (move.Homing)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:homing",
                        "Homing"
                    );
                    buttons.Add(button);
                }

                if (move.Throw)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{move.Character.Name}:throw",
                        "Throw"
                    );
                    buttons.Add(button);
                }

                if (move.IsFromStance)
                {
                    var button = new DiscordButtonComponent(
                        ButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:stance:{move.StanceCode}",
                        move.StanceName!
                    );
                    buttons.Add(button);
                }

                msg.WithReply(args.Message.Id);

                if (buttons.Count > 0)
                {
                    msg.AddComponents(buttons);
                }

                msg.AddEmbed(embed);

                // TODO: Для обычных сообщений Discord не поддерживает ephemeral, но для application commands это будет реализовано
                await sender.SendMessageAsync(args.Channel, msg);

                return;
            }
        }

        try
        {
            // TODO: Для обычных сообщений Discord не поддерживает ephemeral, но для application commands это будет реализовано
            await sender.SendMessageAsync(args.Channel, "Кривой запрос фд");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, ex.Message);
        }
    }

    private static async Task<DiscordMessageBuilder> BuildMoveDiscordMessage(
        DiscordClient sender,
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

        var link = await GetImageUrlAsync(sender, move.Character!);

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

        var link = await GetImageUrlAsync(sender, character);

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
                await HandleRandomMoveCase(RandomMoveType.High, sender, movelist, args);
                return;
            case "randommid":
                await HandleRandomMoveCase(RandomMoveType.Mid, sender, movelist, args);
                return;
            case "randomlow":
                await HandleRandomMoveCase(RandomMoveType.Low, sender, movelist, args);
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
            Logger?.LogError(ex, ex.Message);
        }
    }

    public static async Task CharacterOnlyRequest(Character character, MessageCreateEventArgs args)
    {
        var embed = new DiscordEmbedBuilder(DefaultEmbed)
        {
            Title = character.Name.FirstCharToUpper(),
            Url = character.PageUrl,
            Timestamp = DateTime.Now,
        };
        var msg = new DiscordMessageBuilder();
        var charName = character.Name.ToLower();
        // TODO: Исправтиь ссылку на изображение с wavu wiki
        if (character.LinkToImage != null)
        {
            embed.WithThumbnail(character.LinkToImage);
        }

        if (character.Description != null)
        {
            embed.WithDescription(character.Description);
        }

        if (character is { Strengths: not null, Weaknesess: not null })
        {
            embed.AddField(
                nameof(Character.Strengths),
                string.Join(Environment.NewLine, character.Strengths),
                true
            );
            embed.AddField(
                nameof(Character.Weaknesess),
                string.Join(Environment.NewLine, character.Weaknesess),
                true
            );
        }

        msg.AddComponents(
            new DiscordActionRowComponent(
                [
                    new DiscordButtonComponent(
                        ButtonStyle.Secondary,
                        $"framedata:{charName}:stance",
                        "Stances"
                    ),
                ]
            )
        );

        msg.AddComponents(
            [
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{charName}:powercrush",
                    "Power Crushes"
                ),
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{charName}:homing",
                    "Homings"
                ),
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{charName}:tornado",
                    "Tornados"
                ),
            ]
        );

        msg.AddComponents(
            [
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{charName}:heatburst",
                    "Heat Burst"
                ),
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{charName}:heatengage",
                    "Heat Engagers"
                ),
                new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"framedata:{charName}:heatsmash",
                    "Heat Smash"
                ),
            ]
        );

        msg.AddComponents(
            [
                new DiscordButtonComponent(
                    ButtonStyle.Danger,
                    $"framedata:{charName}:randomhigh",
                    "Random High Move"
                ),
                new DiscordButtonComponent(
                    ButtonStyle.Danger,
                    $"framedata:{charName}:randommid",
                    "Random Mid Move"
                ),
                new DiscordButtonComponent(
                    ButtonStyle.Danger,
                    $"framedata:{charName}:randomlow",
                    "Random Low Move"
                ),
            ]
        );

        //msg.AddActionRowComponent(
        //    new DiscordActionRowComponent(
        //        [
        //            new DiscordTextInputComponent(
        //                "Feedback",
        //                Guid.NewGuid().ToString(),
        //                "Сообщить об ошибке",
        //                "null",
        //                false,
        //                DiscordTextInputStyle.Short,
        //                0,
        //                null
        //            ),
        //        ]
        //    )
        //);

        msg.AddEmbed(embed);

        try
        {
            await args.Message.RespondAsync(msg);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, e.Message);
        }
    }

    public static async Task GuildJoinCallback(
        DiscordFramedataChannels discordFramedataChannels,
        ComponentInteractionCreateEventArgs args
    )
    {
        var channel = args.Interaction.Data.Resolved.Channels.First();
        var channelId = channel.Key;
        var channelName = channel.Value.Name;
        var owner = channel.Value.Guild.Owner;
        await discordFramedataChannels.AddAsync(
            new DiscordFramedataChannel()
            {
                ChannelId = channelId,
                ChannelName = channelName,
                GuildId = channel.Value.Guild.Id,
                GuildName = channel.Value.Guild.Name,
                OwnerName = owner.DisplayName,
                OwnerId = owner.Id,
            }
        );

        try
        {
            var builder = new DiscordInteractionResponseBuilder(
                new DiscordMessageBuilder() { Content = "Ну, вроде готово, хз)" }
            );
            builder.AsEphemeral();
            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.UpdateMessage,
                builder
            );
        }
        catch (Exception e)
        {
            Logger?.LogError(e, e.Message);
        }
    }

    public static Task OnDiscordServerLeave(
        DiscordFramedataChannels channels,
        GuildDeleteEventArgs args
    )
    {
        var guildId = args.Guild.Id;

        try
        {
            return channels.RemoveAsync(guildId);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, ex.Message);
            throw new Exception("a", ex);
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
        Move[] movelist,
        ComponentInteractionCreateEventArgs args
    )
    {
        // Определяем символ для поиска по HitLevel
        char hitLevelChar = type switch
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
        var msg = await BuildMoveDiscordMessage(sender, randomMove);
        try
        {
            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(msg).AsEphemeral()
            );
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, ex.Message);
        }
    }
}
