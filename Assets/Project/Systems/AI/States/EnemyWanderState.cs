using UnityEngine;
using UnityEngine.AI;

namespace Project
{
    public class EnemyWanderState : EnemyBaseState
    {
        private readonly NavMeshAgent _agent;
        private readonly float _wanderSpeed;
        private readonly Transform _enemyTransform;
        private readonly float _wanderRadius;

        public EnemyWanderState(Enemy enemy, Animator animator, NavMeshAgent agent, float wanderSpeed, float wanderRadius) : base(enemy, animator)
        {
            _agent = agent;
            _wanderSpeed = wanderSpeed;
            _wanderRadius = wanderRadius;
            _enemyTransform = enemy.transform;
        }

        public override void OnEnter()
        {
            Debug.Log("Wander");
            _agent.speed = _wanderSpeed;
            _animator.CrossFade(WalkHash, _crossFadeDuration);
            GetNewDestination();
        }

        private void GetNewDestination()
        {
            // find a new destination
            var randomDirection = Random.insideUnitSphere * _wanderRadius;
            randomDirection += _enemyTransform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, _wanderRadius, 1);
            var finalPosition = hit.position;
            _agent.SetDestination(finalPosition);
        }
        public override void Update()
        {
            if (HasReachedDestination())
            {
                GetNewDestination();
            }
        }

        private bool HasReachedDestination()
        {
            return !_agent.pathPending
                && _agent.remainingDistance <= _agent.stoppingDistance
                && (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f);
        }
    }
}
