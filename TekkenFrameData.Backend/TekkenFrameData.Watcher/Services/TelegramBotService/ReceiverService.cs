using TekkenFrameData.Watcher.Services.TelegramBotService.Abstract;
using Telegram.Bot;

namespace TekkenFrameData.Watcher.Services.TelegramBotService;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService(
        ITelegramBotClient botClient,
        UpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<UpdateHandler>> logger)
        : base(botClient, updateHandler, logger)
    {
    }
}