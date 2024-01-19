using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class Door : NetworkBehaviour, IInteractable
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private NetworkVariable<bool> _isOpen = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] private NetworkVariable<bool> _startOpened = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone);

        private const string k_openState = "Door_Open";
        private const string k_closeState = "Door_Close";

        private Collider _doorCollider;

        public string InteractionPrompt => throw new System.NotImplementedException();

        private void Awake()
        {
            _doorCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            if (_startOpened.Value)
            {
                transform.rotation = Quaternion.Euler(0, -90, 0);
            }
            _animator.enabled = true;
            if (IsServer)
                _isOpen = _startOpened;
        }

        public void Interact(Interactor interactor)
        {
            ulong interactorClientID = interactor.GetComponent<NetworkObject>().OwnerClientId;
            if (NetworkManager.Singleton.LocalClientId != interactorClientID) return;
            RequestInteractServerRpc(interactorClientID);
            ExecuteInteract();
        }


        [ServerRpc(RequireOwnership = false)]
        private void RequestInteractServerRpc(ulong interactorClientID)
        {
            _isOpen.Value = !_isOpen.Value;
            FireInteractClientRPC(interactorClientID);
        }

        [ClientRpc]
        private void FireInteractClientRPC(ulong interactorClientID)
        {
            if (NetworkManager.Singleton.LocalClientId == interactorClientID) return;

            ExecuteInteract();
        }

        private void ExecuteInteract()
        {
            string animationName = _isOpen.Value ? k_openState : k_closeState;
            _animator.Play(animationName);               
            _doorCollider.enabled = false;
        }

        public void OnAnimationFinished()
        {
            _doorCollider.enabled = true;
        }
    }
}
