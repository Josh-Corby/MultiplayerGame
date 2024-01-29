using Unity.Netcode;
using UnityEngine;
using Project.Input;
using Cinemachine;

namespace Project
{
    public class PlayerNetwork : NetworkBehaviour
    {
        [SerializeField] private InputReader _input;

        private CinemachineVirtualCamera _vcam;
        private AudioListener _playerAudioListener;
        private InventoryController _inventoryController;
        private ObjectThrower _thrower;
        [SerializeField] private Camera _cam;
        [SerializeField] private UsableItem_Base _currentItem;

        private void Awake()
        {
            _vcam = GetComponentInChildren<CinemachineVirtualCamera>();
            _playerAudioListener = GetComponentInChildren<AudioListener>();
            _inventoryController = GetComponentInChildren<InventoryController>();
            _thrower = GetComponent<ObjectThrower>();
        }

        public override void OnNetworkSpawn()
        {
            PlayerNetworkManager.Instance.RegisterConnectedPlayer(this);
            if (!IsOwner)
            {
                _playerAudioListener.enabled = false;
                _inventoryController.enabled = false;
                _cam.enabled = false;
                return;
            }

            _cam.GetComponent<CinemachineBrain>().enabled = true;
            _playerAudioListener.enabled = true;
            _vcam.Priority = 100;
            _input.EnablePlayerActions();
        }

        private void OnEnable()
        {
            _input.UseEquipment += UseItem;
        }

        private void OnDisable()
        {
            _input.UseEquipment -= UseItem;
        }

        private void UseItem()
        {
            if (_currentItem == null) return;
            if (_currentItem.TryGetComponent<IThrowable>(out var throwable))
            {
                _thrower.ThrowObject(_currentItem.gameObject);
                throwable.OnThrow();
                return;
            }

            _currentItem.Use();
        }
    }
}
