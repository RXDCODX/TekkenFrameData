using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description("получить список твич аккаунт и их статусы принятия фреймдаты бота")]
    public async Task<Message> OnTwitchChannelStatusCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
        var channels = dbContext
            .TekkenChannels.AsEnumerable()
            .Select(e => e.Name + " | " + Enum.GetName(e.FramedataStatus))
            .ToList();
        return await client.SendMessage(
            message.Chat,
            string.Join(Environment.NewLine, channels),
            cancellationToken: token
        );
    }
}
