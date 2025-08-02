using Microsoft.AspNetCore.SignalR;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.SignalRInterfaces;
using Telegram.Bot;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Hubs;

public class MainHub(
    ITwitchClient client,
    ITelegramBotClient telegramBotClient,
    IDbContextFactory<AppDbContext> factory
) : Hub<IMainHubCommands>
{
    public Task SendToMainTwitchMessage(string message)
    {
        return client.SendMessageToMainTwitchAsync(message);
    }

    public async Task SendToAdminsTelegramMessage(string message)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var admins = dbContext.Configuration.Single().AdminIdsArray;

        foreach (var variable in admins)
        {
            await telegramBotClient.SendMessage(variable, message);
        }
    }
}
