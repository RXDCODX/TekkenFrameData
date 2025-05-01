using System.Collections.Concurrent;
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
    private static DateTimeOffset LastPost { get; set; }

    private static async Task AwaitRpsLimit()
    {
        if (DateTimeOffset.Now - LastPost < TimeSpan.FromSeconds(1))
        {
            await Task.Delay(1000);
        }
        LastPost = DateTimeOffset.Now;
    }

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

        var channel = await dbContext.TekkenChannels.FindAsync(userId, cancellationToken);

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
                $"@{userName}, привет! Перед тем как добавить бота на свой канал прочти несколько нюансов о его работе. "
                    + $"У бота есть режим фреймдаты, режим теккен викторины. "
                    + $"Чтобы узнать фреймдату нужно написать !fd \"charname\" \"move input\". "
                    + $"Запуск викторины - !tekken_victorina, доступна только стримеру и модераторам. "
                    + $"Лидерборд - !tekken_leaders, личная стата - !tekken_me. "
                    + $"Если у вас на канале стоит фоловер мод - то боту нужно выдать VIP или права модератора чтобы обойти это ограничение. "
                    + $"Все данные берутся с http://tekkendocs.com/, автор не несет отвественности за недостоверно предоставленную информацию. "
                    + $"Бот в любой момент может быть отключен по любым причинам. "
                    + $"Бот предоставляется по схеме \"Как есть\". "
                    + $"Автор бота - http://twitch.tv/rxdcodx. "
                    + $"Спасибо @doshipanda за мотивацию и помощь по проекту. "
                    + $"Если хочешь добавить бота с фреймдатой и викториной по теккен фреймдате на свой канал, то напиши - !accept {token.Token}. "
                    + $"Если захочешь в будущем отключить - !reject."
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

        var channel = await dbContext.TekkenChannels.FindAsync(userId, cancellationToken);

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
                $"@{userName}, привет! Перед тем как добавить бота на свой канал прочти несколько нюансов о его работе. "
                    + $"У бота есть режим фреймдаты, режим теккен викторины. "
                    + $"Чтобы узнать фреймдату нужно написать !fd \"charname\" \"move input\". "
                    + $"Запуск викторины - !tekken_victorina, доступна только стримеру и модераторам. "
                    + $"Лидерборд - !tekken_leaders, личная стата - !tekken_me. "
                    + $"Если у вас на канале стоит фоловер мод - то боту нужно выдать VIP или права модератора чтобы обойти это ограничение. "
                    + $"Все данные берутся с http://tekkendocs.com/, автор не несет отвественности за недостоверно предоставленную информацию. "
                    + $"Бот в любой момент может быть отключен по любым причинам. "
                    + $"Бот предоставляется по схеме \"Как есть\". "
                    + $"Автор бота - http://twitch.tv/rxdcodx. "
                    + $"Спасибо @doshipanda за мотивацию и помощь по проекту. "
                    + $"Если хочешь добавить бота с фреймдатой и викториной по теккен фреймдате на свой канал, то напиши - !accept {token.Token}. "
                    + $"Если захочешь в будущем отключить - !reject."
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
                                $"@{userName}, все готово! Скоро бот подключиться, возможна задержка."
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

        var channel = await dbContext.TekkenChannels.FindAsync(userId, cancellationToken);

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
                $"@{userName}, привет! Перед тем как добавить бота на свой канал прочти несколько нюансов о его работе. "
                    + $"У бота есть режим фреймдаты, режим теккен викторины. "
                    + $"Чтобы узнать фреймдату нужно написать !fd \"charname\" \"move input\". "
                    + $"Запуск викторины - !tekken_victorina, доступна только стримеру и модераторам. "
                    + $"Лидерборд - !tekken_leaders, личная стата - !tekken_me. "
                    + $"Если у вас на канале стоит фоловер мод - то боту нужно выдать VIP или права модератора чтобы обойти это ограничение. "
                    + $"Все данные берутся с http://tekkendocs.com/, автор не несет отвественности за недостоверно предоставленную информацию. "
                    + $"Бот в любой момент может быть отключен по любым причинам. "
                    + $"Бот предоставляется по схеме \"Как есть\". "
                    + $"Автор бота - http://twitch.tv/rxdcodx. "
                    + $"Спасибо @doshipanda за мотивацию и помощь по проекту. "
                    + $"Если хочешь добавить бота с фреймдатой и викториной по теккен фреймдате на свой канал, то напиши - !accept {token.Token}. "
                    + $"Если захочешь в будущем отключить - !reject."
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
}
