using KBCore.Refs;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace Project
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(LightDetector))]
    public class Nightmare : Enemy
    {
        [SerializeField, Self] private LightDetector _lightDetector;
        [SerializeField] private Transform[] _patrolPoints;

        [SerializeField] private float _toLightSpeed = 15f;

        private void Start()
        {
            _attackTimer = new CountdownTimer(_timeBetweenAttacks);
            _stateMachine = new StateMachine();

            var goToLightState = new EnemyGoToLightState(this, _animator, _agent, _toLightSpeed);
            var chaseState = new EnemyChaseState(this, _animator, _agent, _chaseSpeed, _playerDetector.Player);
            var attackState = new EnemyAttackState(this, _animator, _agent, _playerDetector.Player);

            Any(chaseState, new FuncPredicate(() => _playerDetector.CanDetectPlayer() && !_playerDetector.CanAttackPlayer()));
            Any(attackState, new FuncPredicate(() => _playerDetector.CanAttackPlayer()));

            if (_patrolPoints.Length > 0)
            {
                var patrolState = new EnemyPatrolState(this, _animator, _agent, _patrolSpeed, _patrolPoints);
                At(patrolState, goToLightState, new FuncPredicate(() =>
                {
                    if (_lightDetector.CanSeeLight())
                    {
                        goToLightState.UpdateTarget(_lightDetector.CurrentDetectedLight.transform);
                        return true;
                    }
                    return false;
                }));

                At(goToLightState, patrolState, new FuncPredicate(() => !_lightDetector.CanSeeLight()));
                At(chaseState, patrolState, new FuncPredicate(() => !_playerDetector.CanDetectPlayer()));
                _stateMachine.SetState(patrolState);
            }

            else
            {
                var wanderState = new EnemyWanderState(this, _animator, _agent, _wanderSpeed, _wanderRadius);
                At(wanderState, goToLightState, new FuncPredicate(() =>
                {
                    if (_lightDetector.CanSeeLight())
                    {
                        goToLightState.UpdateTarget(_lightDetector.CurrentDetectedLight.transform);
                        return true;
                    }
                    return false;
                }));


                At(goToLightState, wanderState, new FuncPredicate(() => !_lightDetector.CanSeeLight()));
                At(chaseState, wanderState, new FuncPredicate(() => !_playerDetector.CanDetectPlayer()));
                _stateMachine.SetState(wanderState);
            }
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
    }
}
