using UnityEngine;

namespace Project
{
    public abstract class EnemyBaseState : IState
    {
        protected readonly Enemy _enemy;
        protected readonly Animator _animator;

        protected const float _crossFadeDuration = 0.1f;

        // animation hashes
        protected static readonly int IdleHash = Animator.StringToHash("Idle");
        protected static readonly int RunHash = Animator.StringToHash("Run");
        protected static readonly int WalkHash = Animator.StringToHash("Walk");
        protected static readonly int AttackHash = Animator.StringToHash("Attack");
        protected static readonly int DieHash = Animator.StringToHash("Die");

        protected EnemyBaseState(Enemy enemy, Animator animator)
        {
            _enemy = enemy;
            _animator = animator;
        }
        public virtual void UpdateTarget(Transform target)
        {

        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void Update()
        {
        }
    }
}
