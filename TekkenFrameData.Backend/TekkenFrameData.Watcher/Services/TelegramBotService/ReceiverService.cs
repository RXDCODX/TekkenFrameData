using Microsoft.Extensions.Logging;
using TekkenFrameData.Watcher.Services.TelegramBotService.Abstract;
using Telegram.Bot;

namespace TekkenFrameData.Watcher.Services.TelegramBotService;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    ILogger<ReceiverServiceBase<UpdateHandler>> logger)
    : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);