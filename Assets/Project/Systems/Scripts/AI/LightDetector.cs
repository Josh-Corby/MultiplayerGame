using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Project
{
    public class LightDetector : MonoBehaviour
    {
        [SerializeField] private float _detectionAngle = 60f; // cone in front of enemy
        [SerializeField] private float _detectionRadius = 10f; // large circle around enemy
        [SerializeField] private float _innerDetectionRadius = 5f; // small circle around enemy
        [SerializeField] private float _detectionCooldown = 1f; // Time between detections

        private CountdownTimer _detectionTimer;

        IDetectionStrategy _detectionStrategy;

        [SerializeField] private List<SceneLight> _activeLights = new();

        public SceneLight CurrentDetectedLight { get; private set; }

        private void OnEnable()
        {
            SceneLightManager.OnLightActivated += (activatedLight) => _activeLights.Add(activatedLight);
            SceneLightManager.OnLightDeactivated += (deactivatedLight) => _activeLights.Remove(deactivatedLight);
        }

        private void OnDisable()
        {
            SceneLightManager.OnLightActivated -= (activeLights) => _activeLights.Add(activeLights);
            SceneLightManager.OnLightDeactivated -= (deactivatedLight) => _activeLights.Remove(deactivatedLight);
        }

        private void Start()
        {
            _detectionTimer = new CountdownTimer(_detectionCooldown);
            _detectionStrategy = new ConeDetectionStrategy(_detectionAngle, _detectionRadius, _innerDetectionRadius);
        }

        private void Update() => _detectionTimer.Tick(Time.deltaTime);

        public bool CanSeeLight() => _detectionTimer.IsRunning || CanSeeAnyLight();

        private bool CanSeeAnyLight()
        {
            for(int i = _activeLights.Count - 1; i >= 0; i--)
            {
                SceneLight candidateLight = _activeLights[i];              
                if(!candidateLight.IsOn) continue;

                if(_detectionStrategy.Execute(candidateLight.transform.GetChild(0), transform, _detectionTimer))
                {
                    CurrentDetectedLight = candidateLight;
                    return true;
                }
            }

            CurrentDetectedLight = null;
            return false;
        }

        public void SetDetectionStrategy(IDetectionStrategy detectionStrategy) => _detectionStrategy = detectionStrategy;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;

            // draw a sphere for the radii
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
            Gizmos.DrawWireSphere(transform.position, _innerDetectionRadius);

            // calculate our cone directions
            Vector3 forwardConeDirection = Quaternion.Euler(0, _detectionAngle / 2, 0) * transform.forward * _detectionRadius;
            Vector3 backwardConeDirection = Quaternion.Euler(0, -_detectionAngle / 2, 0) * transform.forward * _detectionRadius;

            Gizmos.DrawLine(transform.position, transform.position + forwardConeDirection);
            Gizmos.DrawLine(transform.position, transform.position + backwardConeDirection);
        }
    }
}
