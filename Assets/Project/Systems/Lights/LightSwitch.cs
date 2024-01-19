using KBCore.Refs;
using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class LightSwitch : NetworkBehaviour, IInteractable
    {
        [SerializeField] private SceneLight _linkedLight;
        [SerializeField] private bool _isOn;

        public string InteractionPrompt => throw new System.NotImplementedException();

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

        }

        [ClientRpc]
        private void FireInteractClientRpc(ulong interactorClientID)
        {

        }

        private void ExecuteInteract()
        {
            _linkedLight.ToggleLight();
        }
    }
}
