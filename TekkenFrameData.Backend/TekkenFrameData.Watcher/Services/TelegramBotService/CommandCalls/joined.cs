using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    public async Task<Message> OnJoinedCommandReceived(
        ITelegramBotClient client,
        Message update,
        CancellationToken token
    )
    {
        var text = twitchClient.JoinedChannels.ToArray();

        return await client.SendMessage(
            update.Chat,
            string.Join(Environment.NewLine, text.Select(e => $"{e.Channel} | {e.ChannelState}")),
            cancellationToken: token
        );
    }
}
