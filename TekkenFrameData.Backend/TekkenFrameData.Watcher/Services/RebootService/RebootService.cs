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
        Console.WriteLine("123asd");
        await using var dbContext = await factory.CreateDbContextAsync();
        var config = dbContext.Configuration.Single();
        Console.WriteLine("1234asd");

        // Параметры подключения
        using var client = new SshClient(
            "host.docker.internal",
            27022,
            config.SSH_Login,
            config.SSH_Password
        );
        Console.WriteLine("1235asd");

        // Подключаемся к серверу
        client.Connect();
        Console.WriteLine("1236asd");

        try
        {
            Console.WriteLine("1237asd");

            // Создаем команду для выполнения скрипта через bash
            var command = client.CreateCommand(
                $"echo \"{RebootScript.Replace("\"", "\\\"")}\" | bash"
            );
            Console.WriteLine("1238asd");

            // Выполняем команду
            var result = command.Execute();
            Console.WriteLine("1239asd");

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
