using Unity.Netcode;
using UnityEngine;
using Utilities;

namespace Project
{
    public class WindUpTorch : UsableItem_Base
    {
        [SerializeField] private Light _torch;
        [SerializeField] private NetworkVariable<float> _currentIntensity = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] private float _torchAngle;
        [SerializeField] private float _lightMaxIntensity;
        [SerializeField] private float _powerUpAmount;
        [SerializeField] private float _powerDownAmount;
        [SerializeField] private float _powerDownTickRate;

        private CountdownTimer _powerDownTimer;

        private void Awake()
        {
            _torch = GetComponent<Light>();
            _powerDownTimer = new CountdownTimer(_powerDownTickRate);
            _powerDownTimer.OnTimerStop += () =>
            {
                PowerDown();
                _powerDownTimer.Start();
            };

            _powerDownTimer.Start();
        }

        private void Start()
        {
            _torch.intensity = 0;
            _torch.spotAngle = _torchAngle;
            _powerUpAmount = _lightMaxIntensity / 10f;
        }

        private void Update()
        {
            _powerDownTimer.Tick(Time.deltaTime);
        }

        public override void Use()
        {
            if(!IsOwner) return;

            float currentIntensity = _torch.intensity + _powerUpAmount;
            currentIntensity = Mathf.Clamp(currentIntensity, 0, _lightMaxIntensity);
            _torch.intensity = currentIntensity;
            
            RequestUseServerRPC(currentIntensity);
        }

        [ServerRpc(RequireOwnership =false)]
        private void RequestUseServerRPC(float currentIntensity)
        {
            _currentIntensity.Value = currentIntensity;
            FireUseClientRPC();
        }

        [ClientRpc]
        private void FireUseClientRPC()
        {
            if (IsOwner) return;

            SetTorchWithNetworkIntensity();
        }

        private void SetTorchWithNetworkIntensity()
        {
            _torch.intensity = _currentIntensity.Value;
        }

        private void PowerDown()
        {
            float currentIntensity = _torch.intensity - _powerDownAmount    ;

            currentIntensity = Mathf.Clamp(currentIntensity, 0, _lightMaxIntensity);
            _torch.intensity = currentIntensity;
        }
    }
}
