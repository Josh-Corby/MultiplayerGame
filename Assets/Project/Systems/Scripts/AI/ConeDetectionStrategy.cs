using UnityEngine;
using Utilities;

namespace Project
{
    public class ConeDetectionStrategy : IDetectionStrategy
    {
        private readonly float _detectionAngle;
        private readonly float _detectionRadius;
        private readonly float _innerDetectionRadius;

        public ConeDetectionStrategy(float detectionAngle, float detectionRadius, float innerDetectionRadus)
        {
            _detectionAngle = detectionAngle;
            _detectionRadius = detectionRadius;
            _innerDetectionRadius = innerDetectionRadus;
        }

        public bool Execute(Transform target, Transform detector, CountdownTimer timer)
        {
            if (timer.IsRunning) return false;

            var directionToTarget = target.position - detector.position;
            var angleToTarget = Vector3.Angle(directionToTarget, detector.forward);

            // if the player is not within the detection angle + outer radius(in vision cone) or player is not in proximity range, return false
            if((!(angleToTarget < _detectionAngle / 2f) || !(directionToTarget.magnitude < _detectionRadius)) 
                && !(directionToTarget.magnitude < _innerDetectionRadius))
                return false;

            timer.Start();
            return true;
        }
    }
}
