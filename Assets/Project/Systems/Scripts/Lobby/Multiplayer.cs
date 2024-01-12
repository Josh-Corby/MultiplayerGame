using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Utilities;

namespace Project
{

    [System.Serializable]
    public enum EncryptionType
    {
        DTLS, // Datagram Transform Layer Security
        WSS   // Web Socket Secure
    }
    // note: also Upd and Ws are possible choices

    public class Multiplayer : SingletonBase<Multiplayer>
    {
        [SerializeField] private string _lobbyName = "Lobby";
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private EncryptionType _encryption = EncryptionType.DTLS;

        private Lobby _currentLobby;

        private string ConnectionType => _encryption == EncryptionType.DTLS ? k_dtlsEncryption : k_wssEncryption;

        private const float k_lobbyHeartBeatInterval = 20f;
        private const float k_lobbyPollInterval = 65f;
        private const string k_keyJoinCode = "RelayJoinCode";
        private const string k_dtlsEncryption = "dtls"; // Datagram Transform Layer Security
        private const string k_wssEncryption = "wss";   // Web Socket Secure, use for WebGL builds

        private CountdownTimer _heartbeatTimer = new CountdownTimer(k_lobbyHeartBeatInterval);
        private CountdownTimer _pollForUpdatesTimer = new CountdownTimer(k_lobbyPollInterval);

        public string PlayerID { get; private set; }
        public string PlayerName { get; private set; }

        protected override async void Awake()
        {
            DontDestroy = true;
            base.Awake();

            await Authenticate();

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

        

        private async Task Authenticate()
        {
            await Authenticate("Player-" + Random.Range(0, 1000));
        }

        private async Task Authenticate(string playerName)
        {
            if(UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                InitializationOptions options = new InitializationOptions();
                options.SetProfile(playerName);

                await UnityServices.InitializeAsync(options);
            }

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
            };

            if(!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                PlayerID = AuthenticationService.Instance.PlayerId;
                PlayerName = playerName;
            }
        }

        public async Task CreateLobby()
        {
            try
            {
                Allocation allocation = await AllocateRelay();
                string relayJoinCode = await GetRelayJoinCode(allocation);

                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    IsPrivate = false
                };

                _currentLobby = await LobbyService.Instance.CreateLobbyAsync(_lobbyName, maxPlayers, options);
                Debug.Log("Created lobby: " + _currentLobby.Name + " with code " + _currentLobby.LobbyCode);

                _heartbeatTimer.Start();
                _pollForUpdatesTimer.Start();

                await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {k_keyJoinCode, new DataObject(DataObject.VisibilityOptions.Member,relayJoinCode)}
                    }
                });

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(
                    allocation, ConnectionType));

                NetworkManager.Singleton.StartHost();
            }

            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to create lobby: " + e.Message);
            }
        }

        public async Task QuickJoinLobby()
        {
            try
            {
                _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
                _pollForUpdatesTimer.Start();

                string relayJoinCode = _currentLobby.Data[k_keyJoinCode].Value;
                JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(
                    joinAllocation, ConnectionType));

                NetworkManager.Singleton.StartClient();
            }

            catch(LobbyServiceException e)
            {
                Debug.LogError("Failed to quick join lobby: " + e.Message);
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

            catch(RelayServiceException e)
            {
                Debug.LogError("Failed to get relay join code: " + e.Message);
                return default;
            }
        }

        private async Task<JoinAllocation> JoinRelay(string relayJoinCode)
        {
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
                return joinAllocation;
            }

            catch(RelayServiceException e)
            {
                Debug.LogError("Failed to join relay: " + e.Message);
                return default;
            }
        }

        private async Task HandleHeartbeatAsync()
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
                Debug.Log("Sent heartbeat ping to lobby: " + _currentLobby.Name);
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
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
                Debug.Log("Polled for updates on lobby " + lobby.Name);
            }

            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to poll for updates on lobby: " + e.Message);
            }
        }
    }
}
