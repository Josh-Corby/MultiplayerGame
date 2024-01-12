using Project.Input;
using UnityEngine;

namespace Project
{
    public class LocomotionState : BaseState
    {
        public LocomotionState(CharacterMotor player, Animator animator) : base(player, animator) { }

        public override void OnEnter()
        {
            _animator.CrossFade(LocomotionHash, _crossFadeDuration);
        }

        public override void FixedUpdate()
        {
            _player.UpdateMovement();
        }
    }
}
