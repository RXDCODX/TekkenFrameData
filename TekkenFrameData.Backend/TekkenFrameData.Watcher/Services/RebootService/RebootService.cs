using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;

namespace TekkenFrameData.Watcher.Services.RebootService;

public class RebootService(IDbContextFactory<AppDbContext> factory, ILogger<RebootService> logger)
{
    public const string RebootScript = """
        #!/bin/bash

        REPO_DIR="$HOME/Загрузки/git/TekkenFrameData"
        REPO_URL="https://github.com/user/project"

        # Проверяем существование директории
        if [ -d "$REPO_DIR" ]; then
            echo "Директория существует, обновляю репозиторий..."
            cd "$REPO_DIR"
            git pull
        else
            echo "Директория не найдена, клонирую репозиторий..."
            gh repo clone "$REPO_URL" "$REPO_DIR"
        fi
        """;

    public async Task UpdateService()
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
            var command = client.CreateCommand(
                $"echo \"{RebootScript.Replace("\"", "\\\"")}\" | bash"
            );

            // Выполняем команду
            var result = command.Execute();

            // Выводим результат выполнения
            logger.LogInformation("Результат выполнения: {result}", result);
            if (!string.IsNullOrEmpty(command.Error))
            {
                logger.LogError("Ошибки: {command.Error}", command.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
        finally
        {
            // Отключаемся от сервера
            client.Disconnect();
        }
    }
}
