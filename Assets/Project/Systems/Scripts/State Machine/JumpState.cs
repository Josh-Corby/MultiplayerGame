using Project.Input;
using UnityEngine;

namespace Project
{
    public class JumpState : BaseState
    {
        public JumpState(CharacterMotor player, Animator animator) : base(player, animator) { }

        public override void OnEnter()
        {
            _animator.CrossFade(JumpHash, _crossFadeDuration);
        }

        public override void FixedUpdate()
        {
            _player.UpdateMovement();
        }
    }
}
