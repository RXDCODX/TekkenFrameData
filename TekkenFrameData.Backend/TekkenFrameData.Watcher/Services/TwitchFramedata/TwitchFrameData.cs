using System.Collections.Generic;
using System.Text.RegularExpressions;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.Framedata;
using TekkenFrameData.Watcher.Services.TekkenVictorina;
using TekkenFrameData.Watcher.Services.TelegramBotService;
using Telegram.Bot;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class TwitchFramedate(
    ILogger<TwitchFramedate> logger,
    ITwitchClient client,
    Tekken8FrameData frameData,
    IHostApplicationLifetime lifetime,
    IDbContextFactory<AppDbContext> factory,
    ITelegramBotClient telegramBotClient
) : BackgroundService
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private static readonly Regex Regex = new(@"\p{C}+");
    public static readonly List<string> ApprovedChannels = [];

    public async void FrameDateMessage(object? sender, OnChatCommandReceivedArgs args)
    {
        var message = args.Command.ChatMessage.Message;
        var userName = args.Command.ChatMessage.DisplayName;
        var userId = args.Command.ChatMessage.UserId;
        var command = args.Command.CommandText;
        var channelId = args.Command.ChatMessage.RoomId;
        var channelName = args.Command.ChatMessage.Channel;
        var isBroadcaster = args.Command.ChatMessage.IsBroadcaster;
        if (PassByUser(userId) || IsChannelApproved(channelId))
        {
            if (command.StartsWith("fd", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Run(
                    async () =>
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
                                channelName,
                                $"@{userName}, плохие параметры запроса фреймдаты"
                            );
                            return;
                        }

                        var response =
                            await HandleTagMoves(keyWords) ?? await HandleStances(keyWords);

                        if (response is null)
                        {
                            var result = await HandleSingleMove(keyWords);
                            if (
                                result.HasValue
                                && CrossChannelManager.MovesInVictorina.Values.Contains(
                                    result.Value.move
                                )
                            )
                            {
                                client.SendMessage(
                                    channelName,
                                    $"@{userName}, этот удар находиться в теккен виткорине, пока что не могу подсказать!"
                                );
                                return;
                            }
                            else if (result.HasValue)
                            {
                                await SendResponse(channelName, result.Value.response);
                                return;
                            }
                        }

                        if (response != null)
                        {
                            await SendResponse(channelName, response);
                        }
                        else
                        {
                            await SendResponse(
                                channelName,
                                $"@{userName}, ничего не найдено по вашему запросу"
                            );
                        }
                    },
                    _cancellationToken
                );
            }
            else if (command.StartsWith("feedback"))
            {
                if (UpdateHandler.AdminLongs is { Length: > 0 })
                {
                    var message2 = $"""
                        Твич-фидбек от
                            {userName} (id: {userId})
                        С канала
                            {channelName}

                        Сообщение:
                            {message}

                        {DateTime.Now:F}
                        """;

                    foreach (var adminLong in UpdateHandler.AdminLongs)
                    {
                        await telegramBotClient.SendMessage(
                            adminLong,
                            message2,
                            cancellationToken: _cancellationToken
                        );
                    }
                }
            }
        }
        else
        {
            if (
                isBroadcaster
                && (
                    command.StartsWith("fd", StringComparison.OrdinalIgnoreCase)
                    || command.StartsWith("start")
                )
            )
            {
                client.SendMessage(
                    channelName,
                    $"@{channelName}, перед пользованием тебе нужно согласиться на использование! Посмотри описание моего канала для подробностей!"
                );
            }
        }
    }

    private bool PassByUser(string userId)
    {
        var pass =
            userId.Trim().Equals(TwitchClientExstension.AuthorId.ToString())
            || userId.Trim().Equals(TwitchClientExstension.AnubisaractId.ToString());

        if (pass)
        {
            return true;
        }

        if (ApprovedChannels.Contains(userId))
        {
            return true;
        }

        //проверяем наличие канала в бд
        using var dbContext = factory.CreateDbContext();
        var isApproved = dbContext.TekkenChannels.Any(e =>
            e.TwitchId == userId && e.FramedataStatus == TekkenFramedataStatus.Accepted
        );
        if (!isApproved)
        {
            return false;
        }

        ApprovedChannels.Add(userId);
        return true;
    }

    private bool IsChannelApproved(string channelId)
    {
        if (ApprovedChannels.Contains(channelId))
        {
            return true;
        }
        else
        {
            //проверяем наличие канала в бд
            using var dbContext = factory.CreateDbContext();
            var IsApproved = dbContext.TekkenChannels.Any(e =>
                e.TwitchId == channelId && e.FramedataStatus == TekkenFramedataStatus.Accepted
            );
            if (IsApproved)
            {
                ApprovedChannels.Add(channelId);
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
        return character == null
            ? null
            : $"\u2705 {character.Name} \u2705 {Enum.GetName(result.Value.Tag)} | "
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
        return character == null
            ? null
            : $"\u2705 {character.Name} \u2705 Стойки: "
                + string.Join(", ", stances.Select(e => $"{e.Key} - {e.Value}"));
    }

    private async Task<(Move move, string response)?> HandleSingleMove(string[] keyWords)
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

        return (
            move,
            $"\u2705 {move.Character.Name} > {move.Command} \u2705 "
                + $"Старт: {move.StartUpFrame} | Блок: {move.BlockFrame} | Хит: {move.HitFrame} | "
                + $"CH: {move.CounterHitFrame} | Уровень: {move.HitLevel} | Урон: {move.Damage}"
                + stanceInfo
                + tagsInfo
        );
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
            client.OnChatCommandReceived += FrameDateMessage;
        });

        return Task.CompletedTask;
    }
}
