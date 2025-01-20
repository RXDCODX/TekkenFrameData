using TekkenFrameData.Watcher.Services.TelegramBotService.Abstract;

namespace TekkenFrameData.Watcher.Services.TelegramBotService;

// Compose Polling and ReceiverService implementations
public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
        : base(serviceProvider, logger)
    {
    }
}