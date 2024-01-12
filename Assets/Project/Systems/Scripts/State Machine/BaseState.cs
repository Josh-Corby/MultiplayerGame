using Project.Input;
using UnityEngine;

namespace Project
{
    public abstract class BaseState : IState
    {
        protected readonly CharacterMotor _player;
        protected readonly Animator _animator;

        protected static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        protected static readonly int JumpHash = Animator.StringToHash("Jump");

        protected const float _crossFadeDuration = 0.1f;

        protected BaseState(CharacterMotor player, Animator animator)
        {
            _player = player;
            _animator = animator;
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
