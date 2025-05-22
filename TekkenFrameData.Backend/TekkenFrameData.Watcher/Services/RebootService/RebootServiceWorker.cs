using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;

namespace TekkenFrameData.Watcher.Services.RebootService;

public class RebootServiceWorker(
    IDbContextFactory<AppDbContext> factory,
    ILogger<RebootServiceWorker> logger
)
{
    public const string RebootScript = "sh ~/Загрузки/update_repo.sh";

    public async Task<string> UpdateService()
    {
        var message = "good";
        await using var dbContext = await factory.CreateDbContextAsync();
        message = "asd1234";
        var config = dbContext.Configuration.Single();
        message = "asd1235";

        // Параметры подключения
        using var client = new SshClient(
            "host.docker.internal",
            27022,
            config.SSH_Login,
            config.SSH_Password
        );
        message = "asd1236";

        // Подключаемся к серверу
        client.Connect();
        message = "asd1237";

        try
        {
            // Создаем команду для выполнения скрипта через bash
            var command = client.CreateCommand(RebootScript);
            message = "asd1238";

            // Выполняем команду
            var result = command.Execute();
            message = "asd1239";

            // Выводим результат выполнения
            client.Disconnect();
            message = result;
            return $"Результат выполнения: {message}";
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            client.Disconnect();
            message = "asd12310";
            return message;
        }
        finally
        {
            // Отключаемся от сервера
            client.Disconnect();
        }
    }
}
