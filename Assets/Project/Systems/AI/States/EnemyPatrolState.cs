using UnityEngine;
using UnityEngine.AI;

namespace Project
{
    public class EnemyPatrolState : EnemyBaseState
    {
        private readonly Transform[] _patrolTargets;
        private readonly NavMeshAgent _agent;
        private readonly float _patrolSpeed;
        private int _currentPatrolIndex;

        public EnemyPatrolState(Enemy enemy, Animator animator, NavMeshAgent agent, float patrolSpeed, Transform[] patrolTargets) : base(enemy, animator)
        {
            _agent = agent;
            _patrolTargets = patrolTargets;
        }

        public override void OnEnter()
        {
            Debug.Log("Patrolling");
            _agent.speed = _patrolSpeed;
            _animator.CrossFade(WalkHash, _crossFadeDuration);
            _agent.SetDestination(GetClosestPatrolPoint());
        }

        public override void Update()
        {
            if (HasReachedDestination())
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolTargets.Length;
                _agent.SetDestination(_patrolTargets[_currentPatrolIndex].position);
            }
        }

        private Vector3 GetClosestPatrolPoint()
        {
            Vector3 closestPoint = _patrolTargets[0].position;
            float closestDistance = Vector3.Distance(_agent.transform.position, closestPoint);
            for (int i = 0; i < _patrolTargets.Length; i++)
            {
                float distance = Vector3.Distance(_agent.transform.position, _patrolTargets[i].position);

                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = _patrolTargets[i].position;
                    _currentPatrolIndex = i;
                }
            }

            return closestPoint;
        }

        private bool HasReachedDestination()
        {
            return !_agent.pathPending
                && _agent.remainingDistance <= _agent.stoppingDistance
                && (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f);
        }
    }
}
