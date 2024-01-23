using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class LightSwitch : NetworkBehaviour, IInteractable
    {
        [SerializeField] private SceneLight _linkedLight;
        [SerializeField] private NetworkVariable<bool> _isOn = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

        public string InteractionPrompt => throw new System.NotImplementedException();

        public void Interact(Interactor interactor)
        {
            ulong interactorClientID = interactor.GetComponent<NetworkObject>().OwnerClientId;
            if (NetworkManager.Singleton.LocalClientId != interactorClientID) return;
            RequestInteractServerRpc(interactorClientID);
            ExecuteInteract();
        }

        public void Interact()
        {
            if (!IsServer) return;
            RequestInteractServerRpc(NetworkManager.Singleton.LocalClientId);
            ExecuteInteract();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestInteractServerRpc(ulong interactorClientID)
        {
            _isOn.Value = !_isOn.Value;
            FireInteractClientRpc(interactorClientID);
        }

        [ClientRpc]
        private void FireInteractClientRpc(ulong interactorClientID)
        {
            if (NetworkManager.Singleton.LocalClientId == interactorClientID) return;

            ExecuteInteract();
        }

        private void ExecuteInteract()
        {
            _linkedLight.ToggleLight();
        }


    }
}
