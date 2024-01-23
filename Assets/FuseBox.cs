using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class FuseBox : NetworkBehaviour, IInteractable
    {
        [SerializeField] private NetworkVariable<bool> _isEnabled = new NetworkVariable<bool>();
        [SerializeField] private bool _startEnabled;
        [SerializeField] private List<SceneLight> _connectedLights = new List<SceneLight>();

        public string InteractionPrompt => throw new System.NotImplementedException();

        private void Awake()
        {
            _connectedLights = SceneLightManager.Instance.AllLights;
        }

        public void Interact(Interactor interactor)
        {
            throw new System.NotImplementedException();
        }
    }
}
