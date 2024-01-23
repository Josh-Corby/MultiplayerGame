using UnityEngine;

namespace Project
{
    public interface IMovementMode
    {
        void Initialise(CharacterMotorConfig config, CharacterMotor motor, MotorState state);

        RaycastHit FixedUpdate_GroundedCheck();

        void FixedUpdate_OnBecameGrounded();

        void FixedUpdate_PreGroundedCheck();

        void FixedUpdate_TickMovement(RaycastHit groundHitResult, Vector2 movementVector);

        void LateUpdate_Tick();

        float CurrentMaxSpeed { get; set; }
    }
}
