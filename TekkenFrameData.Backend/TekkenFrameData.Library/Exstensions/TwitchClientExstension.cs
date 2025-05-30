﻿using Microsoft.Extensions.Logging;
using TwitchLib.Client.Interfaces;

namespace TekkenFrameData.Library.Exstensions;

public static class TwitchClientExstension
{
    public const string Channel = "neutralbackkorobka";
    public const int ChannelId = 1305814373;
    public const int AuthorId = 785975641;
    public const int AnubisaractId = 77322336;

    public static async Task SendMessageToMainTwitchAsync<T>(
        this ITwitchClient client,
        string message,
        ILogger<T>? logger = default
    )
        where T : class
    {
        try
        {
            if (
                !client.JoinedChannels.Any(e =>
                    e.Channel.Equals(Channel, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                client.JoinChannel(Channel);
            }

            var channel = client.GetJoinedChannel(Channel);
            if (message.Length > 450)
            {
                while (message.Length > 450)
                {
                    var split = message.Take(450).ToArray();
                    var newmessage = message.Skip(450).ToArray();
                    message = new string(newmessage);

                    await Task.Delay(750);

                    client.SendMessage(channel, new string(split));
                }
            }
            else
            {
                client.SendMessage(channel, message);
            }
        }
        catch (Exception e)
        {
            logger?.LogException(e);
        }
    }

    public static async Task SendMessageToMainTwitchAsync(
        this ITwitchClient client,
        string message,
        ILogger? logger = null
    )
    {
        try
        {
            if (
                !client.JoinedChannels.Any(e =>
                    e.Channel.Equals(Channel, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                client.JoinChannel(Channel);
            }

            var channel = client.GetJoinedChannel(Channel);

            if (message.Contains('.'))
            {
                var splits = message.Split(
                    '.',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                );
                var isPass = splits.All(e => e.Length < 450);

                if (isPass)
                {
                    foreach (var split in splits)
                    {
                        await Task.Delay(3000);

                        client.SendMessage(channel, split + '.');
                    }
                }

                return;
            }

            if (message.Length > 450)
            {
                while (message.Length > 450)
                {
                    var split = message.Take(450).ToArray();
                    var newmessage = message.Skip(450).ToArray();
                    message = new string(newmessage);

                    await Task.Delay(3000);

                    client.SendMessage(channel, new string(split));
                }
            }
            else
            {
                client.SendMessage(channel, message);
            }
        }
        catch (Exception e)
        {
            logger?.LogException(e);
        }
    }
}
