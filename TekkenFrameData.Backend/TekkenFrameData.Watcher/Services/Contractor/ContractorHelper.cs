using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
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
        + "Как только закончишь прочтение описания бота и если все еще хочешь добавить бота с фреймдатой и викториной по теккен фреймдате на свой канал, то напиши следующее сообщение. !accept {1}. ";

    public static async Task StartTask(
        IDbContextFactory<AppDbContext> factory,
        ITwitchClient client,
        OnChatCommandReceivedArgs e,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var userName = e.Command.ChatMessage.DisplayName;
        var userId = e.Command.ChatMessage.UserId;

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
            await client.SendMessageToMainTwitchAsync(
                string.Format(HelloMessage, userName, token.Token)
            );
        }
        else
        {
            var status = channel.FramedataStatus;

            switch (status)
            {
                case TekkenFramedataStatus.None:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #212 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepting:
                    await AwaitRpsLimit();
                    var token = dbContext.AcceptesTokens.Find(userId)!;
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, тебе надо написать !accept и твой токен!! Твой токен - {token.Token}"
                    );
                    break;
                case TekkenFramedataStatus.Canceled:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, ты отменил отписку бота, если хочешь вернуть - напиши разработчику!"
                    );
                    break;
                case TekkenFramedataStatus.Rejected:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync($"@{userName}, каво");
                    break;
                case TekkenFramedataStatus.Accepted:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
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
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var userName = e.Command.ChatMessage.DisplayName;
        var userId = e.Command.ChatMessage.UserId;
        var message = e.Command.ArgumentsAsString;

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
            await client.SendMessageToMainTwitchAsync(
                string.Format(HelloMessage, userName, token.Token)
            );
        }
        else
        {
            var status = channel.FramedataStatus;

            switch (status)
            {
                case TekkenFramedataStatus.None:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #212 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepting:

                    var isPassed = Guid.TryParse(message, out var value);
                    await AwaitRpsLimit();
                    if (isPassed)
                    {
                        var token = dbContext.AcceptesTokens.Find(userId)!;

                        if (value.Equals(Guid.Parse(token.Token)))
                        {
                            channel.FramedataStatus = TekkenFramedataStatus.Accepted;

                            await dbContext.SaveChangesAsync(cancellationToken);

                            await client.SendMessageToMainTwitchAsync(
                                $"@{userName}, все готово! Скоро бот подключится, возможна задержка."
                            );
                            break;
                        }
                    }
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, указан кривой токен после команды."
                    );
                    break;
                case TekkenFramedataStatus.Canceled:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, ты отменил отписку бота, если хочешь вернуть - напиши разработчику!"
                    );
                    break;
                case TekkenFramedataStatus.Rejected:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync($"@{userName}, каво");
                    break;
                case TekkenFramedataStatus.Accepted:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
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
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

        var userName = e.Command.ChatMessage.DisplayName;
        var userId = e.Command.ChatMessage.UserId;
        var message = e.Command.ArgumentsAsString;

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
            await client.SendMessageToMainTwitchAsync(
                string.Format(HelloMessage, userName, token.Token)
            );
        }
        else
        {
            var status = channel.FramedataStatus;

            switch (status)
            {
                case TekkenFramedataStatus.None:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #212 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepting:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #232 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Canceled:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, ты уже отменил подписку бота, если хочешь вернуть - напиши разработчику!"
                    );
                    break;
                case TekkenFramedataStatus.Rejected:
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, как ты поймал эту ошибку вообще?! Напиши разрабу о том что ты поймал ошибку #233 и скинь свой твич канал."
                    );
                    break;
                case TekkenFramedataStatus.Accepted:

                    channel.FramedataStatus = TekkenFramedataStatus.Canceled;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await AwaitRpsLimit();
                    await client.SendMessageToMainTwitchAsync(
                        $"@{userName}, готово, подпискака отменена, бот скоро свалит! Примерно 5 минут!"
                    );
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

                await client.SendMessageToMainTwitchAsync(
                    $@"{userName}, канал {variable} был режекнут"
                );
            }
        }
    }
}
