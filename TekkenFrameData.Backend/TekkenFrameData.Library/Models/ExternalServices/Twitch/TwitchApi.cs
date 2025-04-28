using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace TekkenFrameData.Library.Models.ExternalServices.Twitch;

public class TwitchExternalClient : ITwitchClient, IDeferredConfigLoader<TwitchClient>
{
    public bool IsActive { get; set; }
    public TwitchClient? Service { get; set; }
    public bool ThrowOnServiceNull { get; set; } = true;

    private bool _autoReListenOnException;
    private MessageEmoteCollection _channelEmotes;
    private ConnectionCredentials _connectionCredentials;
    private bool _disableAutoPong;
    private bool _isConnected;
    private bool _isInitialized;
    private IReadOnlyList<JoinedChannel> _joinedChannels;
    private WhisperMessage _previousWhisper;
    private string _twitchUsername;
    private bool _willReplaceEmotes;

    // Делегируем все вызовы в Service, если он не null
    private TA? CallService<TA>(Func<TwitchClient, TA> func) =>
        Service is null ? default : func(Service);

    public void CallService(Action<TwitchClient> action)
    {
        EnsureServiceInitialized();
        action(Service!);
    }

    void ITwitchClient.Initialize(
        ConnectionCredentials credentials,
        string channel,
        char chatCommandIdentifier,
        char whisperCommandIdentifier,
        bool autoReListenOnExceptions
    ) =>
        CallService(s =>
            s.Initialize(
                credentials,
                channel,
                chatCommandIdentifier,
                whisperCommandIdentifier,
                autoReListenOnExceptions
            )
        );

    void ITwitchClient.Initialize(
        ConnectionCredentials credentials,
        List<string> channels,
        char chatCommandIdentifier,
        char whisperCommandIdentifier,
        bool autoReListenOnExceptions
    ) =>
        CallService(s =>
            s.Initialize(
                credentials,
                channels,
                chatCommandIdentifier,
                whisperCommandIdentifier,
                autoReListenOnExceptions
            )
        );

    void ITwitchClient.SetConnectionCredentials(ConnectionCredentials credentials) =>
        CallService(s => s.SetConnectionCredentials(credentials));

    void ITwitchClient.AddChatCommandIdentifier(char identifier) =>
        CallService(s => s.AddChatCommandIdentifier(identifier));

    void ITwitchClient.AddWhisperCommandIdentifier(char identifier) =>
        CallService(s => s.AddWhisperCommandIdentifier(identifier));

    void ITwitchClient.RemoveChatCommandIdentifier(char identifier) =>
        CallService(s => s.RemoveChatCommandIdentifier(identifier));

    void ITwitchClient.RemoveWhisperCommandIdentifier(char identifier) =>
        CallService(s => s.RemoveWhisperCommandIdentifier(identifier));

    bool ITwitchClient.Connect() => CallService(s => s.Connect());

    void ITwitchClient.Disconnect() => CallService(s => s.Disconnect());

    void ITwitchClient.Reconnect() => CallService(s => s.Reconnect());

    JoinedChannel? ITwitchClient.GetJoinedChannel(string channel) =>
        CallService(s => s.GetJoinedChannel(channel));

    void ITwitchClient.JoinChannel(string channel, bool overrideCheck) =>
        CallService(s => s.JoinChannel(channel, overrideCheck));

    void ITwitchClient.LeaveChannel(JoinedChannel channel) =>
        CallService(s => s.LeaveChannel(channel));

    void ITwitchClient.LeaveChannel(string channel) => CallService(s => s.LeaveChannel(channel));

    void ITwitchClient.OnReadLineTest(string rawIrc) => CallService(s => s.OnReadLineTest(rawIrc));

    void ITwitchClient.SendMessage(JoinedChannel channel, string message, bool dryRun) =>
        CallService(s => s.SendMessage(channel, message, dryRun));

    void ITwitchClient.SendMessage(string channel, string message, bool dryRun) =>
        CallService(s => s.SendMessage(channel, message, dryRun));

    void ITwitchClient.SendReply(
        JoinedChannel channel,
        string replyToId,
        string message,
        bool dryRun
    ) => CallService(s => s.SendReply(channel, replyToId, message, dryRun));

    void ITwitchClient.SendReply(string channel, string replyToId, string message, bool dryRun) =>
        CallService(s => s.SendReply(channel, replyToId, message, dryRun));

    void ITwitchClient.SendQueuedItem(string message) =>
        CallService(s => s.SendQueuedItem(message));

    void ITwitchClient.SendRaw(string message) => CallService(s => s.SendRaw(message));

    void ITwitchClient.SendWhisper(string receiver, string message, bool dryRun) =>
        CallService(s => s.SendWhisper(receiver, message, dryRun));

    bool ITwitchClient.AutoReListenOnException
    {
        get => Service?.AutoReListenOnException ?? false;
        set
        {
            if (Service != null)
                Service.AutoReListenOnException = value;
        }
    }

    MessageEmoteCollection ITwitchClient.ChannelEmotes => Service?.ChannelEmotes ?? _channelEmotes;

    ConnectionCredentials ITwitchClient.ConnectionCredentials =>
        Service?.ConnectionCredentials ?? _connectionCredentials;

    bool ITwitchClient.DisableAutoPong
    {
        get => Service?.DisableAutoPong ?? _disableAutoPong;
        set
        {
            if (Service != null)
                Service.DisableAutoPong = value;
            _disableAutoPong = value;
        }
    }

    bool ITwitchClient.IsConnected => Service?.IsConnected ?? _isConnected;

    bool ITwitchClient.IsInitialized => Service?.IsInitialized ?? _isInitialized;

    IReadOnlyList<JoinedChannel> ITwitchClient.JoinedChannels =>
        Service?.JoinedChannels ?? _joinedChannels;

    WhisperMessage ITwitchClient.PreviousWhisper => Service?.PreviousWhisper ?? _previousWhisper;

    string ITwitchClient.TwitchUsername => Service?.TwitchUsername ?? _twitchUsername;

    bool ITwitchClient.WillReplaceEmotes
    {
        get => Service?.WillReplaceEmotes ?? _willReplaceEmotes;
        set
        {
            if (Service != null)
                Service.WillReplaceEmotes = value;
            _willReplaceEmotes = value;
        }
    }

    event EventHandler<OnChannelStateChangedArgs>? ITwitchClient.OnChannelStateChanged
    {
        add
        {
            if (Service != null)
                Service.OnChannelStateChanged += value;
        }
        remove
        {
            if (Service != null)
                Service.OnChannelStateChanged -= value;
        }
    }

    event EventHandler<OnChatClearedArgs>? ITwitchClient.OnChatCleared
    {
        add
        {
            if (Service != null)
                Service.OnChatCleared += value;
        }
        remove
        {
            if (Service != null)
                Service.OnChatCleared -= value;
        }
    }

    event EventHandler<OnChatColorChangedArgs>? ITwitchClient.OnChatColorChanged
    {
        add
        {
            if (Service != null)
                Service.OnChatColorChanged += value;
        }
        remove
        {
            if (Service != null)
                Service.OnChatColorChanged -= value;
        }
    }

    event EventHandler<OnChatCommandReceivedArgs>? ITwitchClient.OnChatCommandReceived
    {
        add
        {
            if (Service != null)
                Service.OnChatCommandReceived += value;
        }
        remove
        {
            if (Service != null)
                Service.OnChatCommandReceived -= value;
        }
    }

    event EventHandler<OnConnectedArgs>? ITwitchClient.OnConnected
    {
        add
        {
            if (Service != null)
                Service.OnConnected += value;
        }
        remove
        {
            if (Service != null)
                Service.OnConnected -= value;
        }
    }

    event EventHandler<OnConnectionErrorArgs>? ITwitchClient.OnConnectionError
    {
        add
        {
            if (Service != null)
                Service.OnConnectionError += value;
        }
        remove
        {
            if (Service != null)
                Service.OnConnectionError -= value;
        }
    }

    event EventHandler<OnDisconnectedEventArgs>? ITwitchClient.OnDisconnected
    {
        add
        {
            if (Service != null)
                Service.OnDisconnected += value;
        }
        remove
        {
            if (Service != null)
                Service.OnDisconnected -= value;
        }
    }

    event EventHandler<OnExistingUsersDetectedArgs>? ITwitchClient.OnExistingUsersDetected
    {
        add
        {
            if (Service != null)
                Service.OnExistingUsersDetected += value;
        }
        remove
        {
            if (Service != null)
                Service.OnExistingUsersDetected -= value;
        }
    }

    event EventHandler<OnGiftedSubscriptionArgs>? ITwitchClient.OnGiftedSubscription
    {
        add
        {
            if (Service != null)
                Service.OnGiftedSubscription += value;
        }
        remove
        {
            if (Service != null)
                Service.OnGiftedSubscription -= value;
        }
    }

    event EventHandler<OnIncorrectLoginArgs>? ITwitchClient.OnIncorrectLogin
    {
        add
        {
            if (Service != null)
                Service.OnIncorrectLogin += value;
        }
        remove
        {
            if (Service != null)
                Service.OnIncorrectLogin -= value;
        }
    }

    event EventHandler<OnJoinedChannelArgs>? ITwitchClient.OnJoinedChannel
    {
        add
        {
            if (Service != null)
                Service.OnJoinedChannel += value;
        }
        remove
        {
            if (Service != null)
                Service.OnJoinedChannel -= value;
        }
    }

    event EventHandler<OnLeftChannelArgs>? ITwitchClient.OnLeftChannel
    {
        add
        {
            if (Service != null)
                Service.OnLeftChannel += value;
        }
        remove
        {
            if (Service != null)
                Service.OnLeftChannel -= value;
        }
    }

    event EventHandler<OnLogArgs>? ITwitchClient.OnLog
    {
        add
        {
            if (Service != null)
                Service.OnLog += value;
        }
        remove
        {
            if (Service != null)
                Service.OnLog -= value;
        }
    }

    event EventHandler<OnMessageReceivedArgs>? ITwitchClient.OnMessageReceived
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnMessageSentArgs>? ITwitchClient.OnMessageSent
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnModeratorJoinedArgs>? ITwitchClient.OnModeratorJoined
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnModeratorLeftArgs>? ITwitchClient.OnModeratorLeft
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnModeratorsReceivedArgs>? ITwitchClient.OnModeratorsReceived
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnNewSubscriberArgs>? ITwitchClient.OnNewSubscriber
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnRaidNotificationArgs>? ITwitchClient.OnRaidNotification
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnReSubscriberArgs>? ITwitchClient.OnReSubscriber
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnSendReceiveDataArgs>? ITwitchClient.OnSendReceiveData
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnUserBannedArgs>? ITwitchClient.OnUserBanned
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnUserJoinedArgs>? ITwitchClient.OnUserJoined
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnUserLeftArgs>? ITwitchClient.OnUserLeft
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnUserStateChangedArgs>? ITwitchClient.OnUserStateChanged
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnUserTimedoutArgs>? ITwitchClient.OnUserTimedout
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnWhisperCommandReceivedArgs>? ITwitchClient.OnWhisperCommandReceived
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnWhisperReceivedArgs>? ITwitchClient.OnWhisperReceived
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnWhisperSentArgs>? ITwitchClient.OnWhisperSent
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnMessageThrottledEventArgs>? ITwitchClient.OnMessageThrottled
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnWhisperThrottledEventArgs>? ITwitchClient.OnWhisperThrottled
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnErrorEventArgs>? ITwitchClient.OnError
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnReconnectedEventArgs>? ITwitchClient.OnReconnected
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnVIPsReceivedArgs>? ITwitchClient.OnVIPsReceived
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnCommunitySubscriptionArgs>? ITwitchClient.OnCommunitySubscription
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnMessageClearedArgs>? ITwitchClient.OnMessageCleared
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnRequiresVerifiedEmailArgs>? ITwitchClient.OnRequiresVerifiedEmail
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnRequiresVerifiedPhoneNumberArgs>? ITwitchClient.OnRequiresVerifiedPhoneNumber
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnBannedEmailAliasArgs>? ITwitchClient.OnBannedEmailAlias
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnUserIntroArgs>? ITwitchClient.OnUserIntro
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    event EventHandler<OnAnnouncementArgs>? ITwitchClient.OnAnnouncement
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public void EnsureServiceInitialized()
    {
        if (Service is null)
        {
            throw new NullReferenceException();
        }
    }
}
