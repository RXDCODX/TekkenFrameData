using System.Threading;
using System.Threading.Tasks;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.Abstract;

/// <summary>
///     A marker interface for Update Receiver service
/// </summary>
public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}