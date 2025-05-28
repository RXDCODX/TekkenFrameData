using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamKit2;
using TekkenFrameData.Library.Models.Steam;

namespace TekkenFrameData.Watcher.Services.SteamLobbyLinker
{
    public class SteamLobbyWorker : BackgroundService
    {
        private const uint Tekken8AppId = 1778820;

        private readonly SteamClient _steamClient;
        private readonly CallbackManager _callbackManager;
        private readonly ILogger<SteamLobbyWorker> _logger;

        private SteamUser? _steamUser;
        private SteamFriends? _steamFriends;

        //private bool _isConnected;
        private bool _isLoggedOn;

        public event Action<OnLobbyLinkAvailableEventArgs>? OnLobbyLinkAvailable;

        public SteamLobbyWorker(ILogger<SteamLobbyWorker> logger)
        {
            _logger = logger;

            _steamClient = new SteamClient();
            _callbackManager = new CallbackManager(_steamClient);

            _steamUser =
                _steamClient.GetHandler<SteamUser>()
                ?? throw new InvalidOperationException("SteamUser handler not found");
            _steamFriends =
                _steamClient.GetHandler<SteamFriends>()
                ?? throw new InvalidOperationException("SteamFriends handler not found");

            SetupCallbacks();
        }

        private void SetupCallbacks()
        {
            _callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _callbackManager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendsList);
            _callbackManager.Subscribe<SteamFriends.PersonaStateCallback>(OnPersonaState);
            _callbackManager.Subscribe<SteamFriends.ChatInviteCallback>(CallbackFunc);
        }

        private void ConnectToSteam()
        {
            _logger.LogInformation("[Steam] Connecting...");
            _steamClient.Connect();
        }

        private void DisconnectFromSteam()
        {
            if (_isLoggedOn)
            {
                _steamUser?.LogOff();
            }

            _steamClient.Disconnect();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConnectToSteam();

            while (!stoppingToken.IsCancellationRequested)
            {
                _callbackManager.RunWaitCallbacks(TimeSpan.FromMilliseconds(100));

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            DisconnectFromSteam();
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            //_isConnected = true;
            _logger.LogInformation("[Steam] Connected. Logging in...");

            _steamUser?.LogOn(
                new SteamUser.LogOnDetails
                {
                    Username = "your_username",
                    Password = "your_password",
                    // TwoFactorCode = "123456" // Optional if using Steam Guard
                }
            );
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            //_isConnected = false;
            _isLoggedOn = false;
            _logger.LogWarning("[Steam] Disconnected.");
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                _logger.LogError("[Steam] Login failed: {Result}", callback.Result);
                return;
            }

            _isLoggedOn = true;
            _logger.LogInformation("[Steam] Logged in successfully.");
        }

        private void OnFriendsList(SteamFriends.FriendsListCallback callback)
        {
            _logger.LogInformation(
                "[Steam] Friends list received. Friend count: {Count}",
                callback.FriendList.Count
            );
        }

        private void OnPersonaState(SteamFriends.PersonaStateCallback callback)
        {
            if (callback.GameID.AppID == Tekken8AppId)
            {
                _logger.LogInformation("[Steam] Friend {Name} is playing Tekken 8.", callback.Name);

                string lobbyLink =
                    $"steam://joinlobby/{Tekken8AppId}/0/{callback.FriendID.ConvertToUInt64()}";
                _logger.LogInformation("[Steam] Possible lobby link: {LobbyLink}", lobbyLink);

                OnLobbyLinkAvailable?.Invoke(
                    new OnLobbyLinkAvailableEventArgs(
                        callback.FriendID.ConvertToUInt64(),
                        lobbyLink
                    )
                );
            }
        }

        //private void OnGamePlayed(SteamFriends.GamePlayedCallback callback)
        //{
        //    if (callback.GamePlayed?.GameID.AppID == Tekken8AppId)
        //    {
        //        _logger.LogInformation("[Steam] Game played event detected for Tekken 8.");
        //    }
        //}

        private void CallbackFunc(SteamFriends.ChatInviteCallback obj)
        {
            if (obj.GameID == Tekken8AppId) { }
        }
    }
}
