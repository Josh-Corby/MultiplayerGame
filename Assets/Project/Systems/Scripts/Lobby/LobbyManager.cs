using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Eflatun.SceneReference;

namespace Project
{
    [Serializable]
    public enum EncryptionType
    {
        DTLS, // Datagram Transform Layer Security
        WSS   // Web Socket Secure
    }
    // note: also Upd and Ws are possible choices

    public class LobbyManager : SingletonBase<LobbyManager>
    {
        public event EventHandler OnLeftLobby;

        public event EventHandler<LobbyEventArgs> OnJoinedLobby;
        public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
        public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
        public class LobbyEventArgs : EventArgs
        {
            public Lobby lobby;
        }

        public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
        public class OnLobbyListChangedEventArgs : EventArgs
        {
            public List<Lobby> lobbyList;
        }
        public const string k_playerName = "PlayerName";
        private const string k_startGame = "StartGame_RelayCode";

        [SerializeField] private SceneReference _gameScene;
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private EncryptionType _encryption = EncryptionType.DTLS;

        private string ConnectionType => _encryption == EncryptionType.DTLS ? k_dtlsEncryption : k_wssEncryption;

        private const float k_lobbyHeartBeatInterval = 20f;
        private const float k_lobbyPollInterval = 1.1f;
        private const string k_dtlsEncryption = "dtls"; // Datagram Transform Layer Security
        private const string k_wssEncryption = "wss";   // Web Socket Secure, use for WebGL builds

        private readonly CountdownTimer _heartbeatTimer = new CountdownTimer(k_lobbyHeartBeatInterval);
        private readonly CountdownTimer _pollForUpdatesTimer = new CountdownTimer(k_lobbyPollInterval);

        public Lobby CurrentLobby { get; private set; }
        public string PlayerID { get; private set; }
        public string PlayerName { get; private set; }


        protected override void Awake()
        {
            DontDestroy = true;
            base.Awake();
        }

        private async void Start()
        {
            await Authenticate(EditPlayerName.Instance.GetPlayerName());

            _heartbeatTimer.OnTimerStop += () =>
            {
                HandleHeartbeatAsync();
                _heartbeatTimer.Start();
            };

            _pollForUpdatesTimer.OnTimerStop += () =>
            {
                HandlePollForUpdatesAsync();
                _pollForUpdatesTimer.Start();
            };
        }

        private void Update()
        {
            _heartbeatTimer.Tick(Time.deltaTime);
            _pollForUpdatesTimer.Tick(Time.deltaTime);
        }

        private async Task Authenticate()
        {
            await Authenticate("Player-" + Random.Range(0, 1000));
        }

        private async Task Authenticate(string playerName)
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                InitializationOptions options = new InitializationOptions();
                options.SetProfile(playerName);

                await UnityServices.InitializeAsync(options);
            }

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
            };

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                PlayerID = AuthenticationService.Instance.PlayerId;
                PlayerName = playerName;
            }
        }

        private async Task HandleHeartbeatAsync()
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
                Debug.Log("Sent heartbeat ping to lobby: " + CurrentLobby.Name);
            }

            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to heartbeat lobby: " + e.Message);
            }
        }

        private async Task HandlePollForUpdatesAsync()
        {
            try
            {
                CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
                Debug.Log("Polled for updates on lobby: " + CurrentLobby.Name);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });

                if (!IsPlayerInLobby())
                {
                    // Player was kicked out of this lobby
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });

                    CurrentLobby = null;
                }

                if (CurrentLobby.Data[k_startGame].Value != "0")
                {
                    // Start Game!
                    if (!IsLobbyHost())
                    {
                        Debug.Log("Start game");
                        JoinRelay(CurrentLobby.Data[k_startGame].Value);
                    }

                    CurrentLobby = null;

                }              
            }

            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to poll for updates on lobby: " + e.Message);
            }
        }

        public bool IsLobbyHost()
        {
            return CurrentLobby != null && CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;
        }

        private bool IsPlayerInLobby()
        {
            if (CurrentLobby != null && CurrentLobby.Players != null)
            {
                foreach (Player player in CurrentLobby.Players)
                {
                    if (player.Id == AuthenticationService.Instance.PlayerId)
                    {
                        // This player is in this lobby
                        return true;
                    }
                }
            }
            return false;
        }

        private Player GetPlayer()
        {
            return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { k_playerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName) },
        });
        }

        public async Task CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
        {
            try
            {
                Player player = GetPlayer();

                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    Player = player,
                    IsPrivate = isPrivate,
                    Data = new Dictionary<string, DataObject>
                {
                    { k_startGame, new DataObject(DataObject.VisibilityOptions.Member,"0") }
                }
                };

                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

                CurrentLobby = lobby;
                _heartbeatTimer.Start();
                _pollForUpdatesTimer.Start();

                OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
                Debug.Log("Created Lobby " + lobby.Name);

            }
            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to create lobby: " + e.Message);
            }
        }

        public async Task RefreshLobbyList()
        {
            try
            {
                QueryLobbiesOptions options = new QueryLobbiesOptions();
                options.Count = 25;

                // Filter for open lobbies only
                options.Filters = new List<QueryFilter>
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0")
                };

                // Order by newest lobbies first
                options.Order = new List<QueryOrder>
                {
                    new QueryOrder(
                        asc: false,
                        field: QueryOrder.FieldOptions.Created)
                };

                QueryResponse lobbyListQueryRespose = await Lobbies.Instance.QueryLobbiesAsync(options);

                OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryRespose.Results });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e.Message);
            }
        }

        public async void JoinLobby(Lobby lobby)
        {
            try
            {
                Player player = GetPlayer();

                CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
                {
                    Player = player,
                });

                _pollForUpdatesTimer.Start();
                OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
            }

            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to quick join lobby: " + e.Message);
            }
        }

        public async void UpdatePlayerName(string playerName)
        {
            PlayerName = playerName;

            if (CurrentLobby != null)
            {
                try
                {
                    UpdatePlayerOptions options = new UpdatePlayerOptions();

                    options.Data = new Dictionary<string, PlayerDataObject>
                    {
                        {
                            k_playerName, new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Public,
                                value: playerName)
                        }
                    };

                    string playerId = AuthenticationService.Instance.PlayerId;
                    CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(CurrentLobby.Id, playerId, options);
                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });


                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e.Message);
                }
            }
        }

        public async void LeaveLobby()
        {
            if (CurrentLobby != null)
            {
                try
                {
                    await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, AuthenticationService.Instance.PlayerId);

                    CurrentLobby = null;

                    OnLeftLobby?.Invoke(this, EventArgs.Empty);
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e.Message);
                }
            }
        }

        public async void KickPlayer(string playerId)
        {
            if (IsLobbyHost())
            {
                try
                {
                    await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, playerId);
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e.Message);
                }
            }
        }

        public async Task StartGame()
        {
            if (IsLobbyHost())
            {
                try
                {
                    Debug.Log("Start game");
                    string relayJoinCode = await CreateRelay();

                    Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(CurrentLobby.Id, new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject>
                        {
                            { k_startGame, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                        }
                    });

                    CurrentLobby = lobby;
                    Debug.Log("Loading game scene");
                    NetworkManager.Singleton.StartHost();
                    Loader.LoadNetwork(_gameScene);

                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e.Message);
                }
            }
        }

        private async Task<string> CreateRelay()
        {
            try
            {
                Allocation allocation = await AllocateRelay();
                string relayJoinCode = await GetRelayJoinCode(allocation);
                Debug.Log(relayJoinCode);
                RelayServerData relayServerData = new RelayServerData(allocation, ConnectionType);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();
                return relayJoinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e.Message);
                return default;
            }
        }

        private async Task<Allocation> AllocateRelay()
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1); // exclude the host
                return allocation;
            }

            catch (RelayServiceException e)
            {
                Debug.LogError("Failed to allocate relay: " + e.Message);
                return default;
            }
        }

        private async Task<string> GetRelayJoinCode(Allocation allocation)
        {
            try
            {
                string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                return relayJoinCode;
            }

            catch (RelayServiceException e)
            {
                Debug.LogError("Failed to get relay join code: " + e.Message);
                return default;
            }
        }

        private async void JoinRelay(string relayJoinCode)
        {
            try
            {
                Debug.Log("Joining relay with " + relayJoinCode);
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
                RelayServerData relayServerData = new RelayServerData(joinAllocation, ConnectionType);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.LogError("Failed to join relay: " + e.Message);
            }
        }
    }
}