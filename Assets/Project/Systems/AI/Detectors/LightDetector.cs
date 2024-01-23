using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
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

        [field: SerializeField] public SceneLight CurrentDetectedLight { get; private set; }


        private void Start()
        {
            _detectionTimer = new CountdownTimer(_detectionCooldown);
            _detectionStrategy = new ConeDetectionStrategy(_detectionAngle, _detectionRadius, _innerDetectionRadius);
        }

        private void Update() => _detectionTimer.Tick(Time.deltaTime);

        public bool CanSeeLight() => (_detectionTimer.IsRunning || CanSeeAnyLight()) && CurrentDetectedLight != null;

        private bool CanSeeAnyLight()
        {
            List<SceneLight> candidateLights = new List<SceneLight>(SceneLightManager.Instance.AllLights);

            if(candidateLights.Count == 0)
            {
                CurrentDetectedLight = null;
                return false;
            }

            for(int i = candidateLights.Count - 1; i >= 0; i--)
            {
                SceneLight candidateLight = candidateLights[i];
                if (!candidateLight.IsOn.Value)
                {
                    candidateLights.Remove(candidateLight);
                    continue;
                }

                if(_detectionStrategy.Execute(candidateLight.transform.GetChild(0), transform, _detectionTimer))
                    CurrentDetectedLight = candidateLight;
                else
                    candidateLights.Remove(candidateLight);
            }

            SceneLight TargetLight = GetClosestCandidateLight(candidateLights);
            if (CurrentDetectedLight != TargetLight)
            {
                CurrentDetectedLight = TargetLight;
            }
            return true;
        }

        private SceneLight GetClosestCandidateLight(List<SceneLight> candidateLights)
        {
            SceneLight candidateLight = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < candidateLights.Count; i++)
            {
                candidateLight = candidateLights[i];
                float distance = Vector3.Distance(transform.position, candidateLight.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    candidateLight = candidateLights[i];
                }
            }

            return candidateLight;
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
