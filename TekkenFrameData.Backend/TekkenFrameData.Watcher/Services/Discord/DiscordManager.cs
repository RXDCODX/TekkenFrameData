using DSharpPlus;
using DSharpPlus.EventArgs;
using TekkenFrameData.Watcher.Services.Framedata;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordManager(
    DiscordClient client,
    Tekken8FrameData frameData,
    IHostApplicationLifetime lifetime,
    IDbContextFactory<AppDbContext> appFactory
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
                await Task.Factory.StartNew(
                    async () =>
                        await DiscordBotAnswers.FramedataCallback(
                            sender,
                            appFactory,
                            frameData,
                            eventArgs
                        ),
                    _cancellationToken
                );
                break;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Подписка на события
        client.GuildCreated += HandleEventAsync;
        client.ComponentInteractionCreated += HandleEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
