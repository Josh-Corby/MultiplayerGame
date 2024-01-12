using UnityEngine;
using UnityEngine.AI;

namespace Project
{
    public class EnemyChaseState : EnemyBaseState
    {
        private readonly NavMeshAgent _agent;
        private readonly float _chaseSpeed;
        private Transform _target;

        public EnemyChaseState(Enemy enemy, Animator animator, NavMeshAgent agent, float chaseSpeed, Transform target) : base(enemy, animator)
        {
            _agent = agent;
            _chaseSpeed = chaseSpeed;
            _target = target;
        }

        public override void UpdateTarget(Transform target) => _target = target;

        public override void OnEnter()
        {
            Debug.Log("Chase");
            _agent.speed = _chaseSpeed;
            _animator.CrossFade(RunHash, _crossFadeDuration);
        }

        public override void Update()
        {
            _agent.SetDestination(_target.position);
        }
    }
}
