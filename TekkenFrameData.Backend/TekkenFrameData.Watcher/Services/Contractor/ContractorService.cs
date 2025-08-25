using TekkenFrameData.Watcher.Services.TwitchFramedata;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.Contractor;

public class ContractorService(
    IHostApplicationLifetime lifetime,
    ITwitchClient client,
    IDbContextFactory<AppDbContext> factory,
    TwitchFramedateChannelConnecter connector
) : BackgroundService
{
    private CancellationToken CancellationToken => lifetime.ApplicationStopping;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnChatCommandReceived += ClientOnOnChatCommandReceived;
        });

        return Task.CompletedTask;
    }

    private async void ClientOnOnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var channel = e.Command.ChatMessage.Channel;
        var command = e.Command.CommandText.ToLower();

        // Убираем ограничение на основной канал - теперь команды работают в любом канале
        await Task
            .Factory.StartNew(
                () =>
                {
                    var taskResult = command switch
                    {
                        "start" => ContractorHelper.StartTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        "accept" => ContractorHelper.AcceptTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        "cancel" => ContractorHelper.CancelTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        "cancel_neutralbackkorobka" => ContractorHelper.CancelTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        "reject" => ContractorHelper.RejectTask(
                            factory,
                            client,
                            e,
                            CancellationToken
                        ),
                        _ => Task.CompletedTask,
                    };

                    return taskResult != Task.CompletedTask;
                },
                CancellationToken
            )
            .ContinueWith(
                async (t) =>
                {
                    if (await t)
                    {
                        await connector.ConnectToStreams();
                    }
                },
                CancellationToken
            );
    }
}
