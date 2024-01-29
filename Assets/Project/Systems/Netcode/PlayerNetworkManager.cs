using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Project
{
    public class PlayerNetworkManager : SingletonBase<PlayerNetworkManager>
    {
        [SerializeField] private GameObject _playerPrefab;

        public List<PlayerNetwork> ConnectedPlayers = new List<PlayerNetwork>();

        public event UnityAction<PlayerNetwork> OnPlayerConnected = delegate { }; 

        private void OnEnable()
        {
            if(NetworkManager.Singleton != null)
                NetworkManager.Singleton.SceneManager.OnLoadComplete += SpawnPlayer;
        }

        private void SpawnPlayer(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if(NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject == null)
                NetworkObjectSpawner.SpawnNewNetworkObjectChangeOwnershipToClient(_playerPrefab, Vector3.zero, clientId);
        }

        public void RegisterConnectedPlayer(PlayerNetwork player)
        {
            ConnectedPlayers.Add(player);
            OnPlayerConnected?.Invoke(player);
        }

        public void DeRegisterConnectedPlayer(PlayerNetwork player)
        {
            ConnectedPlayers.Remove(player);
        }
    }
}
