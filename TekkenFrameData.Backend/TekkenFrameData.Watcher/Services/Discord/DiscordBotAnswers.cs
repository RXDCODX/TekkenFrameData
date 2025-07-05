using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TekkenFrameData.Library.Exceptions;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.Discord;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordBotAnswers
{
    public static ILogger<DiscordBotAnswers>? Logger { get; set; }

    private static readonly DiscordEmbedBuilder DefaultEmbed = new() { Color = DiscordColor.Red };

    public static async Task OnDiscordServerJoin(DiscordClient sender, GuildCreatedEventArgs args)
    {
        var msg = new DiscordMessageBuilder();
        var owner = await args.Guild.GetGuildOwnerAsync();
        var guildId = args.Guild.Id;
        var defaultChannel = args.Guild.GetDefaultChannel();
        msg.AddMention(new UserMention(owner));
        msg.WithContent(
            $"Привет <@{owner.Id}>, укажи, пожалуйста, канал, в котором будет работать бот"
        );
        var selectMenu = new DiscordChannelSelectComponent(
            $"guildjoin:{guildId}",
            "Выбери канал для фреймдаты",
            [DiscordChannelType.Text]
        );
        msg.AddActionRowComponent(selectMenu);

        try
        {
            await sender.SendMessageAsync(defaultChannel!, msg);
        }
        catch (Exception ex)
        {
            Logger?.LogException(ex);
        }
    }

    // TODO: Переделать под вызовы мувов с тегом и прочее
    public static async Task FramedataRequest(
        DiscordClient sender,
        Tekken8FrameData frameData,
        MessageCreatedEventArgs args
    )
    {
        var textMsg = args.Message.Content;

        var split = textMsg.Split(' ');

        if (split.Length >= 3)
        {
            split = [.. split.Skip(1)];
            var move = await frameData.GetMoveAsync(split);

            if (move != null)
            {
                var embed = new DiscordEmbedBuilder(DefaultEmbed)
                {
                    Title = move.Character!.Name,
                    Description = move.Command,
                };

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

                // TODO: Починить
                embed.WithFooter(
                    "Presented by Phoenix",
                    "https://cdn.discordapp.com/avatars/1230230891654156421/8b65a4611a360228bf7ed68b0322ac5f.webp?size=240"
                );

                if (move.Character.LinkToImage != null)
                {
                    embed.WithThumbnail(move.Character.LinkToImage, 50, 50);
                }
                embed.WithTimestamp(DateTime.Now);

                var msg = new DiscordMessageBuilder();

                var buttons = new List<DiscordButtonComponent>();
                // TODO: Добавить выход на стойки и захваты
                if (move.HeatEngage)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:heatengage",
                        "Heat Engager"
                    );
                    buttons.Add(button);
                }

                if (move.Tornado)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:tornado",
                        "Tornado"
                    );
                    buttons.Add(button);
                }

                if (move.HeatSmash)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Primary,
                        $"framedata:{move.Character.Name}:heatsmash",
                        "Heat Smash"
                    );
                    buttons.Add(button);
                }

                if (move.PowerCrush)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Danger,
                        $"framedata:{move.Character.Name}:powercrush",
                        "Power Crush"
                    );
                    buttons.Add(button);
                }

                if (move.HeatBurst)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Success,
                        $"framedata:{move.Character.Name}:heatburst",
                        "Heat Burst"
                    );
                    buttons.Add(button);
                }

                if (move.Homing)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:homing",
                        "Homing"
                    );
                    buttons.Add(button);
                }

                if (move.Throw)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Primary,
                        $"framedata:{move.Character.Name}:throw",
                        "Throw"
                    );
                    buttons.Add(button);
                }

                if (move.IsFromStance)
                {
                    var button = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{move.Character.Name}:stance:{move.StanceCode}",
                        move.StanceName!
                    );
                    buttons.Add(button);
                }

                msg.WithReply(args.Message.Id);

                if (buttons.Count > 0)
                {
                    msg.AddActionRowComponent(buttons);
                }

                msg.AddEmbed(embed);

                await sender.SendMessageAsync(args.Channel, msg);
            }
        }

        try
        {
            await sender.SendMessageAsync(args.Channel, "Кривой запрос фд");
        }
        catch (Exception ex)
        {
            Logger?.LogException(ex);
        }
    }

    public static async Task FramedataCallback(
        Tekken8FrameData frameData,
        ComponentInteractionCreatedEventArgs args
    )
    {
        var split = args.Interaction.Data.CustomId.Split(':');
        var charname = split[1];
        var type = split[2];
        var stanceCode = split.ElementAtOrDefault(3);
        var movelist = await frameData.GetCharMoveList(charname);

        if (movelist is not { Length: > 0 })
        {
            throw new TekkenCharacterNotFoundException();
        }

        var embed = new DiscordEmbedBuilder(DefaultEmbed)
        {
            Title = movelist.First().Character!.Name,
        };

        embed.WithAuthor(
            "Presented by RXDCODX",
            "https://twitch.tv/rxdcodx",
            "https://media.discordapp.net/attachments/1370710562497105951/1383709696837292143/canvas-screenshot.png?ex=684fc793&is=684e7613&hm=2a976bc0e73d1f659133dfc965f5b80100f223e40829c4f64bf1f7f89d672a81&=&format=webp&quality=lossless"
        );
        //embed.WithFooter("Presented by RXDCODX");

        var linkToImage = movelist.First().Character?.LinkToImage;
        if (linkToImage != null)
        {
            embed.WithThumbnail(linkToImage, 50, 50);
        }
        embed.WithTimestamp(DateTime.Now);

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
                            .Where(e => e.StanceCode != null)
                            .DistinctBy(e => e.StanceCode)
                            .Select(e => e.StanceCode + " - " + e.StanceName)
                    );
                    embed.AddField("Stances", text.ToString());
                }
                break;
        }

        var msg = new DiscordMessageBuilder();
        msg.AddEmbed(embed);

        var inter = new DiscordInteractionResponseBuilder(msg);
        inter.AsEphemeral();

        try
        {
            await args.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                inter
            );
        }
        catch (Exception ex)
        {
            Logger?.LogException(ex);
        }
    }

    public static async Task CharacterOnlyRequest(
        TekkenCharacter character,
        MessageCreatedEventArgs args
    )
    {
        var embed = new DiscordEmbedBuilder(DefaultEmbed) { Title = character.Name };
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
        embed.WithTitle(charName);
        if (character is { Strengths: not null, Weaknesess: not null })
        {
            embed.AddField(
                nameof(TekkenCharacter.Strengths),
                string.Join(Environment.NewLine, character.Strengths),
                true
            );
            embed.AddField(
                nameof(TekkenCharacter.Weaknesess),
                string.Join(Environment.NewLine, character.Weaknesess),
                true
            );
        }

        msg.AddActionRowComponent(
            new DiscordActionRowComponent(
                [
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:stance",
                        "Stances"
                    ),
                ]
            )
        );

        msg.AddActionRowComponent(
            new DiscordActionRowComponent(
                [
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:powercrush",
                        "Power Crushes"
                    ),
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:homing",
                        "Homings"
                    ),
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:tornado",
                        "Tornados"
                    ),
                ]
            )
        );

        msg.AddActionRowComponent(
            new DiscordActionRowComponent(
                [
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:heatburst",
                        "Heat Burst"
                    ),
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:heatengage",
                        "Heat Engagers"
                    ),
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:heatsmash",
                        "Heat Smash"
                    ),
                ]
            )
        );

        msg.AddActionRowComponent(
            new DiscordActionRowComponent(
                [
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:randomhigh",
                        "Random High Move"
                    ),
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:randommid",
                        "Random Mid Move"
                    ),
                    new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        $"framedata:{charName}:randomlow",
                        "Random Low Move"
                    ),
                ]
            )
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
            Logger?.LogException(e);
        }
    }

    public static async Task GuildJoinCallback(
        DiscordFramedataChannels discordFramedataChannels,
        ComponentInteractionCreatedEventArgs args
    )
    {
        var channel = args.Interaction.Data.Resolved.Channels.First();
        var channelId = channel.Key;
        var channelName = channel.Value.Name;
        var owner = await channel.Value.Guild.GetGuildOwnerAsync();
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
            await args.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder(
                    new DiscordMessageBuilder() { Content = "Ну, вроде готово, хз)" }
                )
            );
        }
        catch (Exception e)
        {
            Logger?.LogException(e);
        }
    }

    public static Task OnDiscordServerLeave(
        DiscordFramedataChannels channels,
        GuildDeletedEventArgs args
    )
    {
        var guildId = args.Guild.Id;

        try
        {
            return channels.RemoveAsync(guildId);
        }
        catch (Exception ex)
        {
            Logger?.LogException(ex);
            throw new Exception("a", ex);
        }
    }
}
