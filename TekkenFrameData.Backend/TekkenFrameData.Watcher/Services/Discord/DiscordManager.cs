using System;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SteamKit2;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Watcher.Services.Framedata;
using DiscordMessageBuilder = DSharpPlus.Entities.DiscordMessageBuilder;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordManager(
    DiscordClient client,
    DiscordFramedataChannels discordFramedataChannels,
    Tekken8FrameData frameData,
    IHostApplicationLifetime lifetime
) : IHostedService
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;

    public Task HandleEventAsync(DiscordClient sender, GuildCreateEventArgs eventArgs)
    {
        return Task.Factory.StartNew(
            async () => await DiscordBotAnswers.OnDiscordServerJoin(sender, eventArgs),
            _cancellationToken
        );
    }

    public async Task HandleEventAsync(DiscordClient sender, MessageCreateEventArgs eventArgs)
    {
        var channelId = eventArgs.Channel.Id;
        var messageText = eventArgs.Message.Content;
        var splits = messageText.Split(' ');
        var keywords = splits.Skip(1).ToArray();

        // Проверка на упоминание бота без команды
        var botMention1 = $"<@{sender.CurrentUser.Id}>";
        var botMention2 = $"<@!{sender.CurrentUser.Id}>";
        if (
            (messageText.Trim() == botMention1 || messageText.Trim() == botMention2)
            && !eventArgs.Author.IsBot
        )
        {
            var embed = new DiscordEmbedBuilder(FrameDataSlashCommands.DefaultEmbed)
                .WithTitle("Доступные команды Discord-бота")
                .WithColor(DiscordColor.Azure)
                .WithDescription(DiscordHelpFormatter.HelpText);

            await eventArgs.Message.RespondAsync(embed);
            return;
        }

        if (discordFramedataChannels.Channels.Contains(channelId))
        {
            if (splits.ElementAtOrDefault(0)?.StartsWith("/fd") ?? false)
            {
                var character = await frameData.GetTekkenCharacter(
                    string.Join(' ', keywords),
                    true
                );

                if (character is not null)
                {
                    await Task.Factory.StartNew(
                        async () =>
                            await DiscordBotAnswers.CharacterOnlyRequest(
                                (Character)character,
                                eventArgs
                            ),
                        _cancellationToken
                    );
                }
                else
                {
                    await Task.Factory.StartNew(
                        async () =>
                            await DiscordBotAnswers.FramedataRequest(sender, frameData, eventArgs),
                        _cancellationToken
                    );
                }
            }
        }
    }

    public async Task HandleEventAsync(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs eventArgs
    )
    {
        var channelId = eventArgs.Channel.Id;
        var callbackType = eventArgs.Interaction.Data.CustomId.Split(':').ElementAtOrDefault(0);
        switch (callbackType)
        {
            case "framedata":
                if (discordFramedataChannels.Channels.Contains(channelId))
                {
                    await Task.Factory.StartNew(
                        async () =>
                            await DiscordBotAnswers.FramedataCallback(sender, frameData, eventArgs),
                        _cancellationToken
                    );
                }
                break;
            case "guildjoin":
                await Task.Factory.StartNew(
                    async () =>
                        await DiscordBotAnswers.GuildJoinCallback(
                            discordFramedataChannels,
                            eventArgs
                        ),
                    _cancellationToken
                );
                break;
        }
    }

    public async Task HandleEventAsync(DiscordClient sender, GuildDeleteEventArgs eventArgs)
    {
        await Task.Factory.StartNew(
            () => DiscordBotAnswers.OnDiscordServerLeave(discordFramedataChannels, eventArgs),
            _cancellationToken
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Подписка на события
        client.GuildCreated += HandleEventAsync;
        client.MessageCreated += HandleEventAsync;
        client.ComponentInteractionCreated += HandleEventAsync;
        client.GuildDeleted += HandleEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
