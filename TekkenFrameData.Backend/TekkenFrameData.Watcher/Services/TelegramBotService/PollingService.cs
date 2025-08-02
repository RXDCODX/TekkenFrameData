using TekkenFrameData.Watcher.Services.TelegramBotService.Abstract;

namespace TekkenFrameData.Watcher.Services.TelegramBotService;

// Compose Polling and ReceiverService implementations
public class PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
    : PollingServiceBase<ReceiverService>(serviceProvider, logger);
