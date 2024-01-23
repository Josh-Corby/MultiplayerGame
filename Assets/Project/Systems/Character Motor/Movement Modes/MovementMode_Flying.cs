using UnityEngine;

namespace Project
{
    public class MovementMode_Flying : MonoBehaviour, IMovementMode
    {
        protected MotorState _state;
        protected CharacterMotorConfig _config;
        protected CharacterMotor _motor;
        private Camera _camera;

        public float CurrentMaxSpeed
        {
            get
            {
                float speed = _config.RunSpeed;
                return _state.CurrentSurfaceSource != null ? _state.CurrentSurfaceSource.Effect(speed, EEffectableParameter.Speed) : speed;
            }

            set
            {
                throw new System.NotImplementedException($"CurrentMaxSpeed cannot be set directly. Update the motor config to change speed");
            }
        }

        public void Initialise(CharacterMotorConfig config, CharacterMotor motor, MotorState state)
        {
            _state = state;
            _config = config;
            _motor = motor;
            _state.LocalGravity.ApplyGravity = false;

            _camera = GetComponentInChildren<Camera>();
        }

        public void FixedUpdate_PreGroundedCheck()
        {
            // align to the local gravity vector
            transform.rotation = Quaternion.FromToRotation(transform.up, _state.UpVector) * transform.rotation;
        }

        public RaycastHit FixedUpdate_GroundedCheck()
        {
            _state.IsGrounded = false;
            _state.IsCrouched = false;
            return new RaycastHit();
        }

        public void FixedUpdate_OnBecameGrounded()
        {

        }

        public void FixedUpdate_TickMovement(RaycastHit groundHitResult, Vector2 movementVector)
        {
            float verticalInput = (_state.Input_Jump ? 1f : 0f) + (_state.Input_Crouch ? -1f : 0f);
            Vector3 movementInput = _camera.transform.forward * movementVector.y +
                                    _camera.transform.right * movementVector.x +
                                    _state.UpVector * verticalInput;

            movementInput *= CurrentMaxSpeed;

            _state.LinkedRB.velocity = movementInput;
        }

        public void LateUpdate_Tick()
        {

        }
    }
}
