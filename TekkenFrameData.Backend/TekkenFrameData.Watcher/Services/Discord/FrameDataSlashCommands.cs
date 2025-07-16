using System.Collections.Generic;
using System.IO;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Options;
using SteamKit2.CDN;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.TelegramBotService;
using Telegram.Bot;

namespace TekkenFrameData.Watcher.Services.Discord;

public class FrameDataSlashCommands(
    Tekken8FrameData frameData,
    ITelegramBotClient telegramBotClient,
    IDbContextFactory<AppDbContext> appFactory
) : ApplicationCommandModule
{
    public static readonly DiscordEmbedBuilder DefaultEmbed = new()
    {
        Color = DiscordColor.Red,
        Author = new DiscordEmbedBuilder.EmbedAuthor()
        {
            IconUrl =
                "https://media.discordapp.net/attachments/1394393334474211491/1394586588004094084/download20250603195457.png?ex=68775978&is=687607f8&hm=844cba7b7cd8988058b098751ea99763876ab06351951edd578c0301a05e3795&=&format=webp&quality=lossless",
            Name = "By RXDCODX",
            Url = "https://twitch.tv/RXDCODX",
        },
    };

    [
        SlashCommand("fd", "Получить информацию о муве или персонаже Tekken 8"),
        Aliases(["framedata", "frame"])
    ]
    public async Task Framedata(
        InteractionContext ctx,
        [
            Option("character", "Имя персонажа"),
            Autocomplete(typeof(CharacterNameAutocompleteProvider))
        ]
            string character,
        [
            Option("move", "Команда/удар (опционально)"),
            Autocomplete(typeof(MoveCommandAutocompleteProvider))
        ]
            string? move = null
    )
    {
        if (string.IsNullOrWhiteSpace(move))
        {
            var charObj = await frameData.GetTekkenCharacter(character);
            if (charObj is { } c)
            {
                var embed = new DiscordEmbedBuilder(DefaultEmbed)
                {
                    Title = c.Name.FirstCharToUpper(),
                    Url = c.PageUrl,
                    Color = DiscordColor.Red,
                    Timestamp = DateTime.Now,
                    Description = c.Description,
                };
                var charName = c.Name.ToLower();

                if (c.Strengths is { Length: > 0 })
                {
                    embed.AddField(
                        "Strengths",
                        string.Join(Environment.NewLine, c.Strengths.Select(a => '·' + " " + a)),
                        true
                    );
                }

                if (c.Weaknesess is { Length: > 0 })
                {
                    embed.AddField(
                        "Weaknesses",
                        string.Join(Environment.NewLine, c.Weaknesess.Select(a => '·' + " " + a)),
                        true
                    );
                }

                var msg = new DiscordMessageBuilder();
                msg.AddEmbed(embed);
                msg.AddComponents(
                    new DiscordComponent[]
                    {
                        new DiscordButtonComponent(
                            ButtonStyle.Secondary,
                            $"framedata:{charName}:stance",
                            "Stances"
                        ),
                    }
                );
                msg.AddComponents(
                    new DiscordComponent[]
                    {
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
                    }
                );
                msg.AddComponents(
                    new DiscordComponent[]
                    {
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
                    }
                );
                msg.AddComponents(
                    new DiscordComponent[]
                    {
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
                    }
                );

                var link = await GetImageUrl(ctx, charObj);

                if (!string.IsNullOrWhiteSpace(link))
                {
                    embed.WithThumbnail(link, 50, 50);
                }

                await ctx.CreateResponseAsync(
                    InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder(msg).AsEphemeral()
                );
                return;
            }
            else
            {
                await ctx.CreateResponseAsync(
                    InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Персонаж '{character}' не найден")
                        .AsEphemeral()
                );
                return;
            }
        }
        else
        {
            var moveObj = await frameData.GetMoveAsync([character, move]);
            if (moveObj is { } m)
            {
                var embed = new DiscordEmbedBuilder(DefaultEmbed)
                {
                    Title = m.Character?.Name.FirstCharToUpper() ?? character.FirstCharToUpper(),
                    Url = m.Character?.PageUrl,
                    Description = m.Command,
                    Timestamp = DateTime.Now,
                };

                embed.AddField(
                    "Startup",
                    !string.IsNullOrWhiteSpace(m.StartUpFrame) ? m.StartUpFrame : "null",
                    true
                );
                embed.AddField(
                    "Block",
                    !string.IsNullOrWhiteSpace(m.BlockFrame) ? m.BlockFrame : "null",
                    true
                );
                embed.AddField(
                    "Hit",
                    !string.IsNullOrWhiteSpace(m.HitFrame) ? m.HitFrame : "null",
                    true
                );
                embed.AddField(
                    "CH",
                    !string.IsNullOrWhiteSpace(m.CounterHitFrame) ? m.CounterHitFrame : "null",
                    true
                );
                embed.AddField(
                    "Target",
                    !string.IsNullOrWhiteSpace(m.HitLevel) ? m.HitLevel : "null",
                    true
                );
                embed.AddField(
                    "Dmg",
                    !string.IsNullOrWhiteSpace(m.Damage) ? m.Damage : "null",
                    true
                );

                embed.AddField("Notes", !string.IsNullOrWhiteSpace(m.Notes) ? m.Notes : "null");

                var link = await GetImageUrl(ctx, m.Character!);

                if (!string.IsNullOrWhiteSpace(link))
                {
                    embed.WithThumbnail(link, 50, 50);
                }

                var buttons = new List<DiscordButtonComponent>();
                if (m.HeatEngage)
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Secondary,
                            $"framedata:{character}:heatengage",
                            "Heat Engager"
                        )
                    );
                if (m.Tornado)
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Secondary,
                            $"framedata:{character}:tornado",
                            "Tornado"
                        )
                    );
                if (m.HeatSmash)
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Primary,
                            $"framedata:{character}:heatsmash",
                            "Heat Smash"
                        )
                    );
                if (m.PowerCrush)
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Danger,
                            $"framedata:{character}:powercrush",
                            "Power Crush"
                        )
                    );
                if (m.HeatBurst)
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Success,
                            $"framedata:{character}:heatburst",
                            "Heat Burst"
                        )
                    );
                if (m.Homing)
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Secondary,
                            $"framedata:{character}:homing",
                            "Homing"
                        )
                    );
                if (m.Throw)
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Primary,
                            $"framedata:{character}:throw",
                            "Throw"
                        )
                    );
                if (m.IsFromStance && !string.IsNullOrWhiteSpace(m.StanceCode))
                    buttons.Add(
                        new DiscordButtonComponent(
                            ButtonStyle.Secondary,
                            $"framedata:{character}:stance:{m.StanceCode}",
                            m.StanceName ?? "Stance"
                        )
                    );
                var msg = new DiscordMessageBuilder().AddEmbed(embed);
                if (buttons.Count > 0)
                    msg.AddComponents(buttons);
                await ctx.CreateResponseAsync(
                    InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder(msg).AsEphemeral()
                );
                return;
            }
            else
            {
                await ctx.CreateResponseAsync(
                    InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            $"Мув для '{character.FirstCharToUpper()}' и '{move}' не найден"
                        )
                        .AsEphemeral()
                );
                return;
            }
        }
    }

    [SlashCommand("character", "Показать список персонажей Tekken 8")]
    public async Task CharacterList(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent(
                    "Вот доступный список персонажей:"
                        + Environment.NewLine
                        + Environment.NewLine
                        + string.Join(
                            Environment.NewLine,
                            Aliases.CharacterNameAliases.Keys.Select(e => e)
                        )
                )
                .AsEphemeral()
        );
    }

    [SlashCommand("feedback", "Отправить фидбек / сообщить об ошибке", true)]
    public async Task SendFeedback(
        InteractionContext ctx,
        [Option("message", "Само сообщение")] string text
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            await ctx.CreateResponseAsync("Сообщение было пустой", true);
            return;
        }

        await Task.Factory.StartNew(async () =>
        {
            var message = $"""
            Дискорд-фидбек от
                {ctx.User.Username} (id: {ctx.User.Id})
            С сервера 
                {ctx.Guild.Name} (id: {ctx.Guild.OwnerId})

            Сообщение:
                {text}

            {DateTime.Now:F}
            """;

            foreach (var adminka in UpdateHandler.AdminLongs)
            {
                await telegramBotClient.SendMessage(adminka, message);
            }
        });

        await ctx.CreateResponseAsync("Фидбек отправлен!", true);
    }

    private async Task<string?> GetImageUrl(BaseContext context, Character character)
    {
        if (!string.IsNullOrWhiteSpace(character.LinkToImage))
        {
            return character.LinkToImage;
        }

        string? url;

        if (character.Image is { Length: > 0 } && DiscordBotAnswers.TechChannel != null)
        {
            var response = await context.Client.SendMessageAsync(
                DiscordBotAnswers.TechChannel,
                builder =>
                {
                    builder.AddFile(character.Name + ".webp", new MemoryStream(character.Image));
                }
            );
            url = response.Attachments.First().Url;

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
}
