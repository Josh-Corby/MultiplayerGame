﻿using UnityEngine;
using Utilities;
namespace Project
{
    public class PlayerDetector : MonoBehaviour
    {
        [SerializeField] private float _detectionAngle = 60f; // cone in front of enemy
        [SerializeField] private float _detectionRadius = 10f; // large circle around enemy
        [SerializeField] private float _innerDetectionRadius = 5f; // small circle around enemy
        [SerializeField] private float _detectionCooldown = 1f; // Time between detections
        [SerializeField] private float _attackRange = 2f; // distance from enemy to player to attack

        public Transform Player { get; private set; }
        private CountdownTimer _detectionTimer;

        IDetectionStrategy _detectionStrategy;

        private void Awake()
        {
            _detectionTimer = new CountdownTimer(_detectionCooldown);
            Player = GameObject.FindGameObjectWithTag("Player").transform; // dont forget to tag the player with this tag
            _detectionStrategy = new ConeDetectionStrategy(_detectionAngle, _detectionRadius, _innerDetectionRadius);
        }

        private void Update() => _detectionTimer.Tick(Time.deltaTime);

        public bool CanDetectPlayer() => _detectionTimer.IsRunning || _detectionStrategy.Execute(Player, transform, _detectionTimer);

        public bool CanAttackPlayer()
        {
            var directionToPlayer = Player.position - transform.position;
            return directionToPlayer.magnitude <= _attackRange;
        }

        public void SetDetectionStrategy(IDetectionStrategy detectionStrategy) => _detectionStrategy = detectionStrategy;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

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
