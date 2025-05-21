using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.Framedata;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class TwitchFramedate(
    ILogger<TwitchFramedate> logger,
    ITwitchClient client,
    Tekken8FrameData frameData,
    IHostApplicationLifetime lifetime,
    IDbContextFactory<AppDbContext> factory
) : BackgroundService
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private static readonly Regex Regex = new(@"\p{C}+");
    private readonly List<string> _approvedChannels = [];

    public async void FrameDateMessage(object? sender, OnMessageReceivedArgs args)
    {
        var channel = args.ChatMessage.RoomId;

        if (IsChannelApproved(channel))
        {
            await Task.Run(
                async () =>
                {
                    var message = args.ChatMessage.Message;

                    if (message.StartsWith("!fd ", StringComparison.OrdinalIgnoreCase))
                    {
                        var keyWords = Regex
                            .Replace(message, "")
                            .Split(
                                ' ',
                                StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries
                            )
                            .Skip(1)
                            .ToArray();

                        if (keyWords.Length < 2)
                        {
                            await SendResponse(
                                channel,
                                "@{user}, плохие параметры запроса фреймдаты"
                            );
                            return;
                        }

                        var response =
                            await HandleTagMoves(keyWords)
                            ?? await HandleStances(keyWords)
                            ?? await HandleSingleMove(keyWords);
                        if (response != null)
                        {
                            await SendResponse(channel, response);
                        }
                        else
                        {
                            await SendResponse(
                                channel,
                                "@{user}, ничего не найдено по вашему запросу"
                            );
                        }
                    }
                },
                _cancellationToken
            );
        }
    }

    private bool IsChannelApproved(string channelId)
    {
        if (_approvedChannels.Contains(channelId))
        {
            return true;
        }
        else
        {
            //проверяем наличие канала в бд
            using var dbContext = factory.CreateDbContext();
            bool IsApproved = dbContext.TekkenChannels.Any(e =>
                e.TwitchId == channelId && e.FramedataStatus == TekkenFramedataStatus.Accepted
            );
            if (IsApproved)
            {
                _approvedChannels.Add(channelId);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private async Task<string?> HandleTagMoves(string[] keyWords)
    {
        var result = await frameData.GetMultipleMovesByTags(string.Join(' ', keyWords));
        if (result is not { Item2.Length: > 1 })
        {
            return null;
        }

        var character = await frameData.GetTekkenCharacter(string.Join(' ', keyWords.SkipLast(1)));
        if (character == null)
        {
            return null;
        }

        return $"\u2705 {character.Name} \u2705 {Enum.GetName(result.Value.Tag)} | "
            + $"Команды: {string.Join(", ", result.Value.Moves.Select(e => e.Command))}";
    }

    private async Task<string?> HandleStances(string[] keyWords)
    {
        if (!keyWords.Last().StartsWith("stance", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var charName = string.Join(' ', keyWords.SkipLast(1));
        var stances = await frameData.GetCharacterStances(charName, _cancellationToken);
        if (stances is not { Count: > 0 })
        {
            return null;
        }

        await using var dbContext = await factory.CreateDbContextAsync(_cancellationToken);
        var character = await frameData.FindCharacterInDatabaseAsync(charName, dbContext);
        if (character == null)
        {
            return null;
        }

        return $"\u2705 {character.Name} \u2705 Стойки: "
            + string.Join(", ", stances.Select(e => $"{e.Key} - {e.Value}"));
    }

    private async Task<string?> HandleSingleMove(string[] keyWords)
    {
        var move = await frameData.GetMoveAsync(keyWords);
        if (move?.Character == null)
        {
            return null;
        }

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

        if (move.Throw)
        {
            tags.Add("Throw");
        }

        var stanceInfo = !string.IsNullOrWhiteSpace(move.StanceCode)
            ? $" | Стойка: {move.StanceName} ({move.StanceCode})"
            : "";

        var tagsInfo = tags.Count > 0 ? $" | Теги: {string.Join(", ", tags)}" : "";

        return $"\u2705 {move.Character.Name} > {move.Command} \u2705 "
            + $"Старт: {move.StartUpFrame} | Блок: {move.BlockFrame} | Хит: {move.HitFrame} | "
            + $"CH: {move.CounterHitFrame} | Уровень: {move.HitLevel} | Урон: {move.Damage}"
            + stanceInfo
            + tagsInfo;
    }

    private Task SendResponse(string channel, string message)
    {
        try
        {
            if (
                !client.JoinedChannels.Any(e =>
                    e.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                client.JoinChannel(channel);
            }

            var joinedChannel = client.GetJoinedChannel(channel);

            // Twitch имеет ограничение на длину сообщения (500 символов)
            if (message.Length > 450)
            {
                message = message[..450] + "...";
            }

            client.SendMessage(joinedChannel, message);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Ошибка при отправке сообщения в Twitch");
        }

        return Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnMessageReceived += FrameDateMessage;
        });

        return Task.CompletedTask;
    }
}
