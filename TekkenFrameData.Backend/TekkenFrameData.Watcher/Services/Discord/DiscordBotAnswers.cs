using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TekkenFrameData.Library.Exceptions;
using TekkenFrameData.Library.Models.Discord;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordBotAnswers
{
    public static async Task OnDiscordServerJoin(DiscordClient sender, GuildCreateEventArgs args)
    {
        var msg = new DiscordMessageBuilder();
        var owner = args.Guild.Owner;
        var guildId = args.Guild.Id;
        var defaultChannel = args.Guild.GetDefaultChannel();
        msg.AddMention(new UserMention(owner));
        msg.WithContent(
            $"Привет <@{owner.Id}>, укажи, пожалуйста, канал, в котором будет работать бот"
        );
        var selectMenu = new DiscordChannelSelectComponent(
            $"guildjoin:{guildId}",
            "Выбери канал для фреймдаты",
            [ChannelType.Text]
        );
        msg.AddComponents(selectMenu);

        await sender.SendMessageAsync(defaultChannel, msg);
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

            if (move != null)
            {
                var embed = new DiscordEmbedBuilder
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

                embed.WithThumbnail(move.Character.LinkToImage, 50, 50);
                embed.WithColor(DiscordColor.Red);
                embed.WithTimestamp(DateTime.Now);

                var msg = new DiscordMessageBuilder();

                var buttons = new List<DiscordComponent>();
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
                        move.StanceName
                    );
                    buttons.Add(button);
                }

                msg.WithReply(args.Message.Id);

                if (buttons.Count > 0)
                    msg.AddComponents(buttons);
                msg.WithEmbed(embed);

                await sender.SendMessageAsync(args.Channel, msg);
            }
        }

        await sender.SendMessageAsync(args.Channel, "Кривой запрос фд");
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
        var movelist = await frameData.GetCharMoveList(charname);

        if (movelist is not { Length: > 0 })
        {
            throw new TekkenCharacterNotFoundException();
        }

        var embed = new DiscordEmbedBuilder { Title = movelist.First().Character!.Name };

        embed.WithAuthor(
            "Presented by RXDCODX",
            "https://twitch.tv/rxdcodx",
            "https://media.discordapp.net/attachments/1370710562497105951/1383709696837292143/canvas-screenshot.png?ex=684fc793&is=684e7613&hm=2a976bc0e73d1f659133dfc965f5b80100f223e40829c4f64bf1f7f89d672a81&=&format=webp&quality=lossless"
        );
        //embed.WithFooter("Presented by RXDCODX");

        embed.WithThumbnail(movelist.First().Character!.LinkToImage, 50, 50);
        embed.WithColor(DiscordColor.Red);
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
        msg.WithEmbed(embed);

        var inter = new DiscordInteractionResponseBuilder(msg);
        inter.AsEphemeral();

        await args.Interaction.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            inter
        );
    }

    public static async Task CharacterOnlyRequest(
        DiscordClient sender,
        TekkenCharacter character,
        MessageCreateEventArgs args
    )
    {
        var embed = new DiscordEmbedBuilder { Title = character.Name };
        var msg = new DiscordMessageBuilder();
        var charName = character.Name.ToLower();
        // TODO: Исправтиь ссылку на изображение с wavu wiki
        embed.WithThumbnail(character.LinkToImage);
        embed.WithDescription(character.Description);
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

        IEnumerable<DiscordActionRowComponent> buttons =
        [
            new(
                [
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:stance",
                        "Stances"
                    ),
                ]
            ),
            new(
                [
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:powercrush",
                        "Power Crushes"
                    ),
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:homing",
                        "Homings"
                    ),
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:tornado",
                        "Tornados"
                    ),
                ]
            ),
            new(
                [
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:heatburst",
                        "Heat Burst"
                    ),
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:heatengage",
                        "Heat Engagers"
                    ),
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:heatsmash",
                        "Heat Smash"
                    ),
                ]
            ),
            new(
                [
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:randomhigh",
                        "Random High Move"
                    ),
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:randommid",
                        "Random Mid Move"
                    ),
                    new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"framedata:{charName}:randomlow",
                        "Random Low Move"
                    ),
                ]
            ),
            new(
                [
                    new TextInputComponent(
                        string.Empty,
                        Guid.NewGuid().ToString(),
                        "Сообщить об ошибке",
                        null,
                        false
                    ),
                ]
            ),
        ];

        msg.WithEmbed(embed);
        msg.AddComponents(buttons);

        await sender.SendMessageAsync(args.Channel, msg);
    }

    public static async Task GuildJoinCallback(
        DiscordClient sender,
        DiscordFramedataChannels discordFramedataChannels,
        ComponentInteractionCreateEventArgs args
    )
    {
        var channel = args.Interaction.Data.Resolved.Channels.First();
        var channelId = channel.Key;
        var channelName = channel.Value.Name;
        await discordFramedataChannels.AddAsync(
            new DiscordFramedataChannel()
            {
                ChannelId = channelId,
                ChannelName = channelName,
                GuildId = channel.Value.Guild.Id,
                GuildName = channel.Value.Guild.Name,
                OwnerName = channel.Value.Guild.Owner.DisplayName,
                OwnerId = channel.Value.Guild.Owner.Id,
            }
        );

        var message = await args.Message.RespondAsync(
            "Ну, вроде готово, хз) Это сообщение удалится через 10 секунд."
        );
        await Task.Delay(TimeSpan.FromSeconds(10));
        await message.DeleteAsync();
    }

    public static Task OnDiscordServerLeave(
        DiscordClient sender,
        DiscordFramedataChannels channels,
        GuildDeleteEventArgs args
    )
    {
        var guildId = args.Guild.Id;
        return channels.RemoveAsync(guildId);
    }
}
