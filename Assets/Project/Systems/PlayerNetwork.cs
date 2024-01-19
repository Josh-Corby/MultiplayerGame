using Unity.Netcode;
using UnityEngine;
using Project.Input;
using Cinemachine;

namespace Project
{
    public class PlayerNetwork : NetworkBehaviour
    {
        [SerializeField] private InputReader _input;

        private CinemachineVirtualCamera _playerCamera;
        private AudioListener _playerAudioListener;
        private InventoryController _inventoryController;

        [SerializeField] private UsableItem_Base _currentItem;

        private void Awake()
        {
            _playerCamera = GetComponentInChildren<CinemachineVirtualCamera>();
            _playerAudioListener = GetComponentInChildren<AudioListener>();
            _inventoryController = GetComponentInChildren<InventoryController>();
            _currentItem = GetComponentInChildren<UsableItem_Base>();
        }

        public override void OnNetworkSpawn()
        {
            PlayerNetworkManager.Instance.RegisterConnectedPlayer(this);
            if (!IsOwner)
            {
                _playerAudioListener.enabled = false;
                _playerCamera.Priority = 0;
                _inventoryController.enabled = false;
                return;
            }

            _playerAudioListener.enabled = true;
            _playerCamera.Priority = 100;
            _input.EnablePlayerActions();
        }

        private void OnEnable()
        {
            _input.UseItem += UseItem;
        }

        private void OnDisable()
        {
            _input.UseItem -= UseItem;
        }

        private void UseItem()
        {
            _currentItem.Use();
        }
    }
}
