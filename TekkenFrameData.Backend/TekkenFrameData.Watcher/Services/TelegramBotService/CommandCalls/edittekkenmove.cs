using TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls.Attribute;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.CommandCalls;

public partial class Commands
{
    [Admin]
    public async Task<Message> OnEditTekkenMoveCommandReceived(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken token
    )
    {
        return await botClient.SendMessage(message.Chat, "заглушка", cancellationToken: token);

        //var text = message.Text;
        //var splits = text?.Split(' ');

        //if (splits is { Length: 2 })
        //{
        //    var tekkenMove = JsonSerializer.Deserialize<TekkenMove>(splits[1]);

        //    if (tekkenMove is not null)
        //    {
        //        await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
        //        var isCharFound = await dbContext.TekkenCharacters.AnyAsync(
        //            e => e.Name == tekkenMove.CharacterName,
        //            cancellationToken: token
        //        );

        //        if (isCharFound)
        //        {
        //            var isMoveExists = await dbContext.TekkenMoves.AnyAsync();
        //        }
        //    }
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
