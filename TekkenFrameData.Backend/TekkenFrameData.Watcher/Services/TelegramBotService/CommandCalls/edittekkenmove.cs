using System.Text.Json;
using TekkenFrameData.Library.Models.FrameData;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    public Task<Message> OnEditTekkenMoveCommandReceived(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken token
    )
    {
        return botClient.SendMessage(message.Chat, "заглушка", cancellationToken: token);

        //var text = message.Text;
        //var splits = text?.Split(' ');

        //if (splits is { Length: 2 })
        //{
        //    var tekkenMove = JsonSerializer.Deserialize<TekkenMove>(splits[1]);
        //}

        //var resultMessage =
        //    "нужно вторым параметром прислать сериализированную строку объекта: "
        //    + Environment.NewLine
        //    + JsonSerializer.Serialize(
        //        new TekkenMove() { Command = "command name", CharacterName = "character name" }
        //    );
        //return botClient.SendMessage(message.Chat, resultMessage, cancellationToken: token);
    }
}
