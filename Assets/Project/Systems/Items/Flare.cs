using UnityEngine;
using Utilities;

namespace Project
{
    public class Flare : UsableItem_Base, IThrowable
    {
        private Rigidbody _rigidbody;
        private CountdownTimer _timer;
        private Light _light;
        private float _timerInterval = 1;
        private float _intensityLossPerInterval = 0.8f;

        private void Start()
        {
            _light = GetComponentInChildren<Light>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.freezeRotation = true;
            _rigidbody.isKinematic = true;   
        }

        private void Update()
        {
            _timer?.Tick(Time.deltaTime);
        }

        public void OnThrow()
        {
            _timer = new CountdownTimer(_timerInterval);
            _timer.OnTimerStop += () =>
            {
                _light.intensity -= _intensityLossPerInterval;
                _timer.Start();
            };
            _timer.Start();
        }

        public override void Use()
        {
        }
    }
}
