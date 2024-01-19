using UnityEngine;
using UnityEngine.AI;

namespace Project
{
    public class EnemyAttackState : EnemyBaseState
    {
        private readonly NavMeshAgent _agent;
        private Transform _target;

        public EnemyAttackState(Enemy enemy, Animator animator, NavMeshAgent agent, Transform target) : base(enemy, animator)
        {
            _agent = agent;
            _target = target;
        }

        public override void UpdateTarget(Transform target) => _target = target;

        public override void OnEnter()
        {
            Debug.Log("Attack");
            _animator.CrossFade(AttackHash, _crossFadeDuration);
        }

        public override void Update()
        {
            _agent.SetDestination(_target.position);
            _enemy.Attack();
        }
    }
}
