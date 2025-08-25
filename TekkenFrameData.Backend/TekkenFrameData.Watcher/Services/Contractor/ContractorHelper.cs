using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.Contractor;

public static class ContractorHelper
{
    private static DateTime LastPost { get; set; }

    private static async Task AwaitRpsLimit()
    {
        if (DateTime.Now - LastPost < TimeSpan.FromSeconds(1))
        {
            await Task.Delay(1000);
        }
        LastPost = DateTime.Now;
    }

    private const string HelloMessage =
        "@{0}, привет! Перед тем как добавить бота на свой канал прочти несколько нюансов о его работе в описании под каналом. "
        + "Как только закончишь прочтение описания бота и если все еще хочешь добавить бота с фреймдатой и викториной по теккен фреймдате на свой канал, то напиши команду !accept. "
        + "Если захочешь отключить бота от своего канала, используй команду !cancel_neutralbackkorobka.";

    private static async Task SendMessageToChannel(
        ITwitchClient client,
        string channelName,
        string message
    )
    {
        // Проверяем, подключен ли бот к каналу
        if (
            !client.JoinedChannels.Any(e =>
                e.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            client.JoinChannel(channelName);
            await Task.Delay(1000); // Небольшая задержка для подключения
        }

        var joinedChannel = client.GetJoinedChannel(channelName);
        if (joinedChannel != null)
        {
            client.SendMessage(joinedChannel, message);
        }
    }

    public static bool IsAuthorizedUser(OnChatCommandReceivedArgs e)
    {
        var userId = e.Command.ChatMessage.UserId;
        var isBroadcaster = e.Command.ChatMessage.IsBroadcaster;
        var isModerator = e.Command.ChatMessage.IsModerator;

        // Проверяем, является ли пользователь владельцем канала, модератором или администратором
        return isBroadcaster
            || isModerator
            || userId.Equals(
                TwitchClientExstension.AuthorId.ToString(),
                StringComparison.OrdinalIgnoreCase
            )
            || userId.Equals(
                TwitchClientExstension.AnubisaractId.ToString(),
                StringComparison.OrdinalIgnoreCase
            );
    }

    public static async Task StartTask(
        IDbContextFactory<AppDbContext> factory,
        ITwitchClient client,
        OnChatCommandReceivedArgs e,
        CancellationToken cancellationToken
    )
    {
        // Проверяем права доступа
        if (!IsAuthorizedUser(e))
        {
            await SendMessageToChannel(
                client,
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.DisplayName}, у тебя нет прав для выполнения этой команды!"
            );
            return;
        }

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var userName = e.Command.ChatMessage.DisplayName;
        var userId = e.Command.ChatMessage.UserId;
        var channelName = e.Command.ChatMessage.Channel;

        var channel = await dbContext.TekkenChannels.FirstOrDefaultAsync(
            e => e.TwitchId == userId,
            cancellationToken
        );

        if (channel is null)
        {
            var token = new TwitchAcceptesToken(userId);
            channel = new TwitchTekkenChannel()
            {
                TwitchId = userId,
                FramedataStatus = TekkenFramedataStatus.Accepting,
                Name = userName,
            };

            dbContext.AcceptesTokens.Add(token);
            dbContext.TekkenChannels.Add(channel);
            await dbContext.SaveChangesAsync(cancellationToken);
            await AwaitRpsLimit();
            await SendMessageToChannel(client, channelName, string.Format(HelloMessage, userName));
        }
        else
        {
            var status = channel.FramedataStatus;

            switch (status)
            {
                case TekkenFramedataStatus.None:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #212 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepting:
                    await AwaitRpsLimit();
                    var token = dbContext.AcceptesTokens.Find(userId)!;
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, тебе надо написать !accept для активации бота!"
                    );
                    break;
                case TekkenFramedataStatus.Canceled:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, ты отменил отписку бота, если хочешь вернуть - напиши разработчику!"
                    );
                    break;
                case TekkenFramedataStatus.Rejected:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(client, channelName, $"@{userName}, каво");
                    break;
                case TekkenFramedataStatus.Accepted:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, у тебя уже все готово!"
                    );
                    break;
            }
        }
    }

    public static async Task AcceptTask(
        IDbContextFactory<AppDbContext> factory,
        ITwitchClient client,
        OnChatCommandReceivedArgs e,
        CancellationToken cancellationToken
    )
    {
        // Проверяем права доступа
        if (!IsAuthorizedUser(e))
        {
            await SendMessageToChannel(
                client,
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.DisplayName}, у тебя нет прав для выполнения этой команды!"
            );
            return;
        }

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var userName = e.Command.ChatMessage.DisplayName;
        var userId = e.Command.ChatMessage.UserId;
        var message = e.Command.ArgumentsAsString;
        var channelName = e.Command.ChatMessage.Channel;

        var channel = await dbContext.TekkenChannels.FirstOrDefaultAsync(
            e => e.TwitchId == userId,
            cancellationToken
        );

        if (channel is null)
        {
            var token = new TwitchAcceptesToken(userId);
            channel = new TwitchTekkenChannel()
            {
                TwitchId = userId,
                FramedataStatus = TekkenFramedataStatus.Accepting,
                Name = userName,
            };

            dbContext.AcceptesTokens.Add(token);
            dbContext.TekkenChannels.Add(channel);
            await dbContext.SaveChangesAsync(cancellationToken);
            await AwaitRpsLimit();
            await SendMessageToChannel(client, channelName, string.Format(HelloMessage, userName));
        }
        else
        {
            var status = channel.FramedataStatus;

            switch (status)
            {
                case TekkenFramedataStatus.None:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #212 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepting:
                    // Просто активируем канал без проверки токена
                    channel.FramedataStatus = TekkenFramedataStatus.Accepted;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, все готово! Скоро бот подключится, возможна задержка."
                    );
                    TwitchFramedate.ApprovedChannels.Clear();
                    break;
                case TekkenFramedataStatus.Canceled:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, ты отменил отписку бота, если хочешь вернуть - напиши разработчику!"
                    );
                    break;
                case TekkenFramedataStatus.Rejected:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(client, channelName, $"@{userName}, каво");
                    break;
                case TekkenFramedataStatus.Accepted:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, у тебя уже все готово!"
                    );
                    break;
            }
        }
    }

    public static async Task CancelTask(
        IDbContextFactory<AppDbContext> factory,
        ITwitchClient client,
        OnChatCommandReceivedArgs e,
        CancellationToken cancellationToken
    )
    {
        // Проверяем права доступа
        if (!IsAuthorizedUser(e))
        {
            await SendMessageToChannel(
                client,
                e.Command.ChatMessage.Channel,
                $"@{e.Command.ChatMessage.DisplayName}, у тебя нет прав для выполнения этой команды!"
            );
            return;
        }

        // Определяем тип команды
        var isFullCancel = e.Command.CommandText.Equals(
            "cancel_neutralbackkorobka",
            StringComparison.OrdinalIgnoreCase
        );

        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var userName = e.Command.ChatMessage.DisplayName;
        var userId = e.Command.ChatMessage.UserId;
        var message = e.Command.ArgumentsAsString;
        var channelName = e.Command.ChatMessage.Channel;

        var channel = await dbContext.TekkenChannels.FirstOrDefaultAsync(
            e => e.TwitchId == userId,
            cancellationToken
        );

        if (channel is null)
        {
            var token = new TwitchAcceptesToken(userId);
            channel = new TwitchTekkenChannel()
            {
                TwitchId = userId,
                FramedataStatus = TekkenFramedataStatus.Accepting,
                Name = userName,
            };

            dbContext.AcceptesTokens.Add(token);
            dbContext.TekkenChannels.Add(channel);
            await dbContext.SaveChangesAsync(cancellationToken);
            await AwaitRpsLimit();
            await SendMessageToChannel(client, channelName, string.Format(HelloMessage, userName));
        }
        else
        {
            var status = channel.FramedataStatus;

            switch (status)
            {
                case TekkenFramedataStatus.None:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #212 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepting:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #232 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Canceled:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, ты уже отменил подписку бота, если хочешь вернуть - напиши разработчику!"
                    );
                    break;
                case TekkenFramedataStatus.Rejected:
                    await AwaitRpsLimit();
                    await SendMessageToChannel(
                        client,
                        channelName,
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #233 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepted:
                    if (isFullCancel)
                    {
                        // Полное отключение бота
                        channel.FramedataStatus = TekkenFramedataStatus.Canceled;
                        await dbContext.SaveChangesAsync(cancellationToken);
                        await AwaitRpsLimit();
                        await SendMessageToChannel(
                            client,
                            channelName,
                            $"@{userName}, готово, подпискака отменена, бот скоро свалит! Примерно 5 минут!"
                        );
                    }
                    else
                    {
                        // Обычная команда cancel - объясняем как полностью отключить
                        await AwaitRpsLimit();
                        await SendMessageToChannel(
                            client,
                            channelName,
                            $"@{userName}, если ты хочешь полностью отключить бота от своего канала, используй команду !cancel_neutralbackkorobka"
                        );
                    }
                    break;
            }
        }
    }

    public static async Task RejectTask(
        IDbContextFactory<AppDbContext> factory,
        ITwitchClient client,
        OnChatCommandReceivedArgs e,
        CancellationToken cancellationToken
    )
    {
        var userName = e.Command.ChatMessage.DisplayName;
        var channelName = e.Command.ChatMessage.Channel;

        userName = userName.StartsWith('@') ? userName[1..] : userName;

        var userId = e.Command.ChatMessage.UserId;
        var isAdmin = userId.Equals(
            TwitchClientExstension.AuthorId.ToString(),
            StringComparison.OrdinalIgnoreCase
        );

        if (isAdmin)
        {
            await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
            var message = e.Command.ArgumentsAsList;

            foreach (var variable in message)
            {
                await dbContext
                    .TekkenChannels.Where(e => e.Name == variable)
                    .ExecuteUpdateAsync(
                        e => e.SetProperty(t => t.FramedataStatus, TekkenFramedataStatus.Rejected),
                        cancellationToken: cancellationToken
                    );

                await dbContext.SaveChangesAsync(cancellationToken);

                await SendMessageToChannel(
                    client,
                    channelName,
                    $@"{userName}, канал {variable} был режекнут"
                );
            }
        }
        else
        {
            await SendMessageToChannel(
                client,
                channelName,
                $"@{e.Command.ChatMessage.DisplayName}, у тебя нет прав для выполнения этой команды!"
            );
        }
    }
}
