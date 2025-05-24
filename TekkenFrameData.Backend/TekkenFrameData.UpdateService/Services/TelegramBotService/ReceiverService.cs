using TekkenFrameData.UpdateService.Services.TelegramBotService.Abstract;
using Telegram.Bot;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    ILogger<ReceiverServiceBase<UpdateHandler>> logger
) : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);
