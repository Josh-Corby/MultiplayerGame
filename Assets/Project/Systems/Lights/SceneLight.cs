using KBCore.Refs;
using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class SceneLight : NetworkBehaviour
    {
        public bool IsPowered;
        [SerializeField] private bool _startOn;
        [SerializeField] private Light _linkedLight;
        public NetworkVariable<bool> IsOn { get; private set; } = new NetworkVariable<bool>(
                                                        readPerm: NetworkVariableReadPermission.Everyone, 
                                                        writePerm: NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                IsOn.Value = _startOn;

            Debug.Log("Registering Light");
            SceneLightManager.Instance.RegisterLight(this);
            SetLightEnabled(IsOn.Value);
        }
  
        public void ToggleLight()
        {
            SetLightEnabled(!IsOn.Value);
        }

        public void SetLightEnabled(bool enabled)
        {
            if (IsServer)
                IsOn.Value = enabled;
            _linkedLight.enabled = enabled;
        }

    }
}
