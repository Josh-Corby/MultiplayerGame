using KBCore.Refs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace Project
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : NetworkBehaviour, IDamageable
    {
        [SerializeField, Self] protected NavMeshAgent _agent;
        [SerializeField, Self] protected PlayerDetector _playerDetector;
        [SerializeField, Child] protected Animator _animator;

        [SerializeField] protected float _wanderRadius = 10f;
        [SerializeField] protected float _timeBetweenAttacks = 1f;
        [SerializeField] protected float _attackDamage = 1f;

        [SerializeField] protected float _wanderSpeed = 10f;
        [SerializeField] protected float _patrolSpeed = 10f;
        [SerializeField] protected float _chaseSpeed = 15f;

        protected StateMachine _stateMachine;
        protected CountdownTimer _attackTimer;
        protected IDamageable _attackTarget;

        protected void OnValidate() => this.ValidateRefs();

        protected void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        protected void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

        private void Start()
        {
            if(!IsServer) enabled = false;
            _attackTimer = new CountdownTimer(_timeBetweenAttacks);
            _stateMachine = new StateMachine();

            var wanderState = new EnemyWanderState(this, _animator, _agent, _wanderSpeed, _wanderRadius);
            var chaseState = new EnemyChaseState(this, _animator, _agent, _chaseSpeed, _playerDetector.DetectedPlayer);
            var attackState = new EnemyAttackState(this, _animator, _agent, _playerDetector.DetectedPlayer);

            Any(wanderState, new FuncPredicate(() => !_playerDetector.CanDetectPlayer()));
            Any(attackState, new FuncPredicate(() => _playerDetector.CanAttackPlayer()));
            Any(chaseState, new FuncPredicate(() => _playerDetector.CanDetectPlayer() && !_playerDetector.CanAttackPlayer()));

            _stateMachine.SetState(wanderState);
        }

        private void Update()
        {
            _stateMachine.Update();
            _attackTimer.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
        }

        public void Attack()
        {
            if (_attackTimer.IsRunning) return;

            _attackTimer.Start();

            _attackTarget ??= _playerDetector.DetectedPlayer.GetComponent<IDamageable>();
            _attackTarget.OnTakeDamage(gameObject, _attackDamage);
            Debug.Log("Attacking");
        }

        public void OnTakeDamage(GameObject source, float amount)
        {
            throw new System.NotImplementedException();
        }

        public void OnPerformHeal(GameObject source, float amount)
        {
            throw new System.NotImplementedException();
        }
    }
}
