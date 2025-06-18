using System.Collections.Generic;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace TekkenFrameData.Watcher.Services.TwitchFramedata;

public class TwitchFramedataChannelsEvents
{
    public delegate Task TwitchEvent(object? sender, ChannelConnectedEventArgs args);

    public class ChannelConnectedEventArgs : EventArgs
    {
        public required Stream[] Streams { get; set; }
    }

    public event TwitchEvent ChannelConnected = (sender, args) => Task.CompletedTask;

    public void InvokeChannelsConnected(Stream[] streams)
    {
        ChannelConnected.Invoke(this, new ChannelConnectedEventArgs() { Streams = streams });
    }
}
