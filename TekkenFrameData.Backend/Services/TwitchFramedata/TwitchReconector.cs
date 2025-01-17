using System.Text.Json;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace tekkenfd.Services.TwitchFramedata
{
    public class TwitchReconector : IHostedService
    {
        private readonly ILogger<TwitchReconector> _logger;
        private readonly ITwitchClient _client;
        private readonly ITwitchAPI _api;
        private readonly TwitchFramedateChannelConnecter _channelConnecter;

        private readonly string _tokenInfoPath = Path.Combine(Directory.GetCurrentDirectory(), "TwitchToken.json");
        private TokenInfo? _tokenInfo;
        private readonly System.Timers.Timer _timer;

        private TokenInfo? Token
        {
            get => _tokenInfo;

            set
            {
                _tokenInfo = value;
                ReconnectClient().GetAwaiter().GetResult();
                File.WriteAllText(_tokenInfoPath, JsonSerializer.Serialize(Token));
            }
        }

        public class TokenInfo
        {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public TimeSpan ExpiresIn { get; set; }
            public DateTime WhenCreated { get; set; }
            public DateTime WhenExpires => WhenCreated.Add(ExpiresIn);
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        }

        public TwitchReconector(ILogger<TwitchReconector> logger,
            ITwitchClient client,
            ITwitchAPI api,
            TwitchFramedateChannelConnecter channelConnecter)
        {
            _logger = logger;
            _client = client;
            _api = api;
            _channelConnecter = channelConnecter;

            if (!File.Exists(_tokenInfoPath))
            {
                var text =
                    $"При отсутвующем {_tokenInfoPath} не удалось найти Environment Variable с названием token. Помести в параметры запуска контейнера аргумент token с последним опубликованным в телеграмме refresh token";
                var token = Environment.GetEnvironmentVariable("token");

                if (token == null)
                {
                    _logger.LogCritical(text);
                    throw new NullReferenceException(text);
                }

                RefreshToken(token).GetAwaiter().GetResult();
            }
            else
            {
                var text = File.ReadAllText(_tokenInfoPath);

                if (string.IsNullOrWhiteSpace(text)) throw new NullReferenceException($"{_tokenInfoPath} был пуст");

                Token = JsonSerializer.Deserialize<TokenInfo>(text);
            }

            _client.OnDisconnected += ClientOnOnDisconnected;

            if (Token == null) throw new NullReferenceException("Token был пуст хотя ожидалось обратное");

            var credential = new ConnectionCredentials("Higemus", Token.AccessToken);
            _client.Initialize(credential);
            _client.Connect();

            _timer = new System.Timers.Timer(TimeSpan.FromMinutes(5));
            _timer.AutoReset = true;
            _timer.Elapsed += async (sender, args) => await ReconnectClient();
        }

        private async void ClientOnOnDisconnected(object? sender, OnDisconnectedEventArgs e)
        {
            await ReconnectClient();

            if(!_client.IsConnected) _timer.Start();
        }

        private async Task ReconnectClient()
        {
            if (Token != null)
            {
                if (DateTime.Now >= Token.WhenExpires)
                {
                    var validated = await ValidateToken(Token.AccessToken);

                    if (!validated)
                    {
                        var isRefreshed = await RefreshToken(Token.RefreshToken);

                        if (!isRefreshed)
                        {
                            _logger.LogCritical("Не удалось обновить твич токен, приложение срочно требует закрытие если не закрылось самостоятельно");
                        }
                        else
                        {
                            if (_client.IsConnected) _client.Disconnect();

                            var credentials = new ConnectionCredentials("Higemus", Token.AccessToken);
                            _client.Initialize(credentials);
                            _client.Connect();
                            await _channelConnecter.ConnectToStreams();
                        }
                    }
                }
            }
        }

        private async Task<bool> ValidateToken(string token)
        {
            try
            {
                var response = await _api.Auth.ValidateAccessTokenAsync(token);

                if (response == null)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e) when (e.Message.Contains("invalid access token"))
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e.StackTrace);
                return false;
            }
        }

        private async Task<bool> RefreshToken(string refreshToken)
        {
            try
            {
                var result = await _api.Auth.RefreshAuthTokenAsync(refreshToken, _api.Settings.Secret, _api.Settings.ClientId);
                var tokenInfo = new TokenInfo()
                {
                    AccessToken = result.AccessToken,
                    ExpiresIn = TimeSpan.FromSeconds(result.ExpiresIn),
                    RefreshToken = result.RefreshToken,
                    WhenCreated = DateTime.Now.AddSeconds(-30)
                };

                _logger.LogCritical("Твич обновил токен! ```{0}```", JsonSerializer.Serialize(tokenInfo));

                Token = tokenInfo;
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e.StackTrace);
            }

            return false;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var variable = Environment.GetEnvironmentVariable("token");

            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new NullReferenceException("Не удалось найти Environment Variable с названием token - укажи для него последний refresh token из телеграмма");
            }

            await RefreshToken(variable);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            _timer.Dispose();
            return Task.CompletedTask;
        }
    }
}
