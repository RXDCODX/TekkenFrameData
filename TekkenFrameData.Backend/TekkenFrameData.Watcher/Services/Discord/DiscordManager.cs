using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordManager(
    DiscordClient discordClient,
    DiscordFramedataChannels discordFramedataChannels,
    Tekken8FrameData frameData,
    IHostApplicationLifetime lifetime
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            discordClient.GuildCreated += DiscordBotAnswers.OnDiscordServerJoin;
            discordClient.MessageCreated += DiscordClientOnMessageCreated;
            discordClient.ComponentInteractionCreated += DiscordClientOnComponentInteractionCreated;
            discordClient.GuildDeleted += DiscordClientOnGuildDeleted;

            discordClient.ConnectAsync(
                new DiscordActivity("https://t.me/redcxde"),
                UserStatus.DoNotDisturb
            );
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            discordClient.GuildCreated -= DiscordBotAnswers.OnDiscordServerJoin;
            discordClient.MessageCreated -= DiscordClientOnMessageCreated;
            discordClient.ComponentInteractionCreated -= DiscordClientOnComponentInteractionCreated;
            discordClient.GuildDeleted -= DiscordClientOnGuildDeleted;

            discordClient.DisconnectAsync();
        });

        return Task.CompletedTask;
    }

    private async Task DiscordClientOnGuildDeleted(DiscordClient sender, GuildDeleteEventArgs args)
    {
        await Task.Factory.StartNew(
            () => DiscordBotAnswers.OnDiscordServerLeave(sender, discordFramedataChannels, args)
        );
    }

    private async Task DiscordClientOnComponentInteractionCreated(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args
    )
    {
        var channelId = args.Channel.Id;

        if (discordFramedataChannels.Channels.Contains(channelId))
        {
            var callbackType = args.Interaction.Data.CustomId.Split(':').ElementAtOrDefault(0);
            switch (callbackType)
            {
                case "framedata":
                    await Task.Factory.StartNew(
                        async () =>
                            await DiscordBotAnswers.FramedataCallback(sender, frameData, args)
                    );
                    break;
                case "guildjoin":
                    await Task.Factory.StartNew(
                        async () =>
                            await DiscordBotAnswers.GuildJoinCallback(
                                sender,
                                discordFramedataChannels,
                                args
                            )
                    );
                    break;
            }
        }
    }

    private async Task DiscordClientOnMessageCreated(
        DiscordClient sender,
        MessageCreateEventArgs args
    )
    {
        var channelId = args.Channel.Id;
        var messageText = args.Message.Content;
        var splits = messageText.Split(' ');
        var keywords = splits.Skip(1).ToArray();

        if (discordFramedataChannels.Channels.Contains(channelId))
        {
            var character = await frameData.GetTekkenCharacter(string.Join(' ', keywords));

            if (character != null)
            {
                await Task.Factory.StartNew(
                    async () =>
                        await DiscordBotAnswers.CharacterOnlyRequest(sender, character, args)
                );
            }
            else
            {
                await Task.Factory.StartNew(
                    async () => await DiscordBotAnswers.FramedataRequest(sender, frameData, args)
                );
            }
        }
    }
}
