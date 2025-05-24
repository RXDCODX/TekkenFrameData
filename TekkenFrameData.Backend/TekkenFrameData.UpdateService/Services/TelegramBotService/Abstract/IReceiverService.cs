using TekkenFrameData.Library.DB;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService.Abstract;

/// <summary>
///     A marker interface for Update Receiver service
/// </summary>
public interface IReceiverService
{
    Task ReceiveAsync(AppDbContext appDbContext, CancellationToken stoppingToken);
}
