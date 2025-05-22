using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;

namespace TekkenFrameData.Watcher.Services.RebootService;

public class RebootService(IDbContextFactory<AppDbContext> factory, ILogger<RebootService> logger)
{
    public const string RebootScript = "sh ~/Загрузки/update_build_repo.sh";

    public async Task<string> UpdateService()
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var config = dbContext.Configuration.Single();

        // Параметры подключения
        using var client = new SshClient(
            "host.docker.internal",
            27022,
            config.SSH_Login,
            config.SSH_Password
        );

        // Подключаемся к серверу
        client.Connect();

        try
        {
            // Создаем команду для выполнения скрипта через bash
            var command = client.CreateCommand(RebootScript);

            // Выполняем команду
            var result = command.Execute();

            // Выводим результат выполнения
            client.Disconnect();
            return $"Результат выполнения: {result}";
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            client.Disconnect();
            return ex.Message;
        }
        finally
        {
            // Отключаемся от сервера
            client.Disconnect();
        }
    }
}
