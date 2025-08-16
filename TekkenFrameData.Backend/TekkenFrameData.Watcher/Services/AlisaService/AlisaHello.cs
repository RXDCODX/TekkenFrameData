using System.Collections.Frozen;
using ProtoBuf.Serializers;
using TekkenFrameData.Library.Exstensions;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;
using TekkenFrameData.Watcher.Services.TwitchFramedata;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Watcher.Services.AlisaService;

public class AlisaHello(
    ITwitchClient client,
    IHostApplicationLifetime lifetime,
    IDbContextFactory<AppDbContext> factory
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            client.OnChatCommandReceived += ClientOnOnChatCommandReceived;
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            client.OnChatCommandReceived -= ClientOnOnChatCommandReceived;
        });

        return Task.CompletedTask;
    }

    private static readonly string[] Answers =
    [
        "В замок канала {0} вошёл отважный воитель {1} в доспехах, сверкающих как солнце.",
        "В этот чат канала {0} проник таинственный искатель приключений {1}, скрывающий лицо за капюшоном.",
        "В таинственный чат канала {0} прошёл путешественник {1} с картой звездного неба за плечами.",
        "На волшебный стрим {0} зашёл мудрец {1} с пером и свитком знаний.",
        "В лунный свет канала {0} вышел странник {1}, ищущий ответы среди звезд.",
        "В ночные улицы чата вышел герой {1} с плащом и острым взглядом, готовый к битве с тенями прошлого.",
        "В зал славы канала {0} вошёл самурай {1}, готовый рассказать свою историю.",
        "На улицах города {0} засиял свет — это герой {1}, полный мечтаний и надежд.",
        "В уютную чайную в горах {0} поднялся отважный авантюрист {1}, наслаждаясь ароматом зелёного чая и мечтая о новых приключениях.",
        "В оживлённый портовый город {0} прибыл мастер меча {1}, его глаза сверкают от предвкушения новых боёв и встреч.",
        "В яркий фестивальный город {0} прибыл герой-авантюрист {1}, чтобы принять участие в легендарных состязаниях.",
        "В уютную деревню {0} у подножия гор зашёл странник-авантюрист {1}, мечтающий о великих подвигах и новых друзьях.",
    ];

    private int LastIndex { get; set; } = 0;

    private async void ClientOnOnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var command = e.Command.CommandText;
        var activator = e.Command.ChatMessage.Username;
        var userId = e.Command.ChatMessage.UserId;
        var channel = e.Command.ChatMessage.Channel;
        var channelId = e.Command.ChatMessage.RoomId;
        var arguments = e.Command.ArgumentsAsList;

        if (command.Equals("hello", StringComparison.OrdinalIgnoreCase))
        {
            if (
                activator.Equals("AlisaAssistant", StringComparison.OrdinalIgnoreCase)
                || userId.Equals(TwitchClientExstension.AuthorId.ToString())
            )
            {
                if (arguments.Count == 1)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        if (IsChannelApproved(channelId))
                        {
                            var index = 0;
                            while (index == LastIndex)
                            {
                                index = Random.Shared.Next(0, Answers.Length);
                            }

                            LastIndex = index;

                            var answer = Answers[index];

                            var userName = arguments[0];

                            userName = userName.StartsWith('@') ? userName : '@' + userName;

                            var msg = string.Format(answer, channel.FirstCharToUpper(), userName);

                            if (
                                !client.JoinedChannels.Any(t =>
                                    t.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)
                                )
                            )
                            {
                                client.JoinChannel(channel);
                            }

                            client.SendMessage(channel, msg);
                        }
                    });
                }
            }
        }
    }

    private bool IsChannelApproved(string channelId)
    {
        if (TwitchFramedate.ApprovedChannels.Contains(channelId))
        {
            return true;
        }
        else
        {
            //проверяем наличие канала в бд
            using var dbContext = factory.CreateDbContext();
            var isApproved = dbContext.TekkenChannels.Any(e =>
                e.TwitchId == channelId && e.FramedataStatus == TekkenFramedataStatus.Accepted
            );
            if (isApproved)
            {
                TwitchFramedate.ApprovedChannels.Add(channelId);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
