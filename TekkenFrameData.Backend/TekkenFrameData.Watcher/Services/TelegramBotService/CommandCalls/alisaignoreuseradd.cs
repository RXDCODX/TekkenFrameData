using System.Text.Json;
using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    public async Task<Message> OnAlisaIgnoreUserAddCommandReceived(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken
    )
    {
        var messageText = message.Text;
        var splits = messageText?.Split(' ');

        if (splits is { Length: 2 })
        {
            var nickname = splits[1].StartsWith('@') ? splits[1][1..] : splits[1];
            var userInfo = await twitchApi.Helix.Users.GetUsersAsync(null, [nickname]);

            if (userInfo is { Users.Length: > 0 })
            {
                var user = userInfo.Users[0];
                await alisaBlocklist.AddBlockerUser(user);
                return await botClient.SendMessage(
                    message.Chat,
                    "Добавил юзера: " + JsonSerializer.Serialize(user),
                    cancellationToken: cancellationToken
                );
            }
        }

        return await botClient.SendMessage(
            message.Chat,
            "Кривые параметры или пользователь не был найден",
            cancellationToken: cancellationToken
        );
    }
}
