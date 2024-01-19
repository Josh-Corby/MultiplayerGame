using UnityEngine;
using UnityEngine.AI;

namespace Project
{
    public class EnemyGoToLightState : EnemyBaseState
    {
        private readonly NavMeshAgent _agent;
        private readonly float _toLightSpeed;
        private Transform _target;
        private SceneLight _targetLight;

        public EnemyGoToLightState(Enemy enemy, Animator animator, NavMeshAgent agent, float toLightSpeed) : base(enemy, animator)
        {
            _agent = agent;
            _toLightSpeed = toLightSpeed;
        }

        public override void OnEnter()
        {
            Debug.Log("Wander");
            _agent.speed = _toLightSpeed;
            _animator.CrossFade(WalkHash, _crossFadeDuration);
            _agent.SetDestination(_target.position);
        }

        public override void Update()
        {
            if (HasReachedDestination())
            {
                if (_targetLight.IsOn)
                    _targetLight.SetLightEnabled(false);
            }
        }

        public override void UpdateTarget(Transform target)
        {
            _targetLight = target.GetComponent<SceneLight>();
            _target = target.GetChild(0).transform;
        }

        private bool HasReachedDestination()
        {
            return !_agent.pathPending
                && _agent.remainingDistance <= _agent.stoppingDistance
                && (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f);
        }
    }
}

