using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    public async Task<Message> OnJoinedCommandReceived(
        ITelegramBotClient client,
        Message update,
        CancellationToken token
    )
    {
        var channels = twitchClient.JoinedChannels.ToArray();
        var text = string.Join(
            Environment.NewLine,
            channels.Select(e => $"{e.Channel} | {e.ChannelState}")
        );

        return await client.SendMessage(
            update.Chat,
            string.IsNullOrWhiteSpace(text) ? "Null" : text,
            cancellationToken: token
        );
    }
}
