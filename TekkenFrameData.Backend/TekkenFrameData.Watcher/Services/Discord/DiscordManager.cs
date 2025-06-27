using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordManager(
    DiscordFramedataChannels discordFramedataChannels,
    Tekken8FrameData frameData,
    IHostApplicationLifetime lifetime
)
    : IHostedService,
        IEventHandler<GuildCreatedEventArgs>,
        IEventHandler<MessageCreatedEventArgs>,
        IEventHandler<ComponentInteractionCreatedEventArgs>,
        IEventHandler<GuildDeletedEventArgs>
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;

    public Task HandleEventAsync(DiscordClient sender, GuildCreatedEventArgs eventArgs)
    {
        return Task.Factory.StartNew(
            async () => await DiscordBotAnswers.OnDiscordServerJoin(sender, eventArgs),
            _cancellationToken
        );
    }

    public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs eventArgs)
    {
        var channelId = eventArgs.Channel.Id;
        var messageText = eventArgs.Message.Content;
        var splits = messageText.Split(' ');
        var keywords = splits.Skip(1).ToArray();

        if (discordFramedataChannels.Channels.Contains(channelId))
        {
            if (splits.ElementAtOrDefault(0)?.StartsWith("/fd") ?? false)
            {
                var character = await frameData.GetTekkenCharacter(
                    string.Join(' ', keywords),
                    true
                );

                if (character != null)
                {
                    await Task.Factory.StartNew(
                        async () =>
                            await DiscordBotAnswers.CharacterOnlyRequest(
                                sender,
                                character,
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
        ComponentInteractionCreatedEventArgs eventArgs
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
                            sender,
                            discordFramedataChannels,
                            eventArgs
                        ),
                    _cancellationToken
                );
                break;
        }
    }

    public async Task HandleEventAsync(DiscordClient sender, GuildDeletedEventArgs eventArgs)
    {
        await Task.Factory.StartNew(
            () =>
                DiscordBotAnswers.OnDiscordServerLeave(sender, discordFramedataChannels, eventArgs),
            _cancellationToken
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
