using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    [Description(
        "Команда для коробки покинуть канал твича (но все равно реконнектится по дефолтным правилам)"
    )]
    public async Task<Message> OnLeaveTwitchChannelCommandReceived(
        ITelegramBotClient client,
        Message message,
        CancellationToken token
    )
    {
        var txt = message.Text;
        var splits = txt?.Split(' ');

        if (splits is { Length: 2 })
        {
            var channelName = splits[1];

            if (
                twitchClient.JoinedChannels.Any(e =>
                    e.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                twitchClient.LeaveChannel(channelName);
                return await client.SendMessage(
                    message.Chat,
                    "Покинул твич канал: " + channelName,
                    cancellationToken: token
                );
            }
            else
            {
                return await client.SendMessage(
                    message.Chat,
                    "Не нашел твич канал среди присоединенных: " + channelName,
                    cancellationToken: token
                );
            }
        }

        return await client.SendMessage(
            message.Chat,
            "Кривые параметры!",
            cancellationToken: token
        );
    }
}
