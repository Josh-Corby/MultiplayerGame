using UnityEngine;
using UnityEngine.Events;

namespace Project
{
    public class MovementMode_Ground : MonoBehaviour, IMovementMode
    {
        public enum EJumpState
        {
            Neutral,
            Rising,
            Falling
        }

        [SerializeField] protected UnityEvent<bool> OnRunChanged = new UnityEvent<bool>();
        [SerializeField] protected UnityEvent<Vector3, float> OnHitGround = new UnityEvent<Vector3, float>();
        [SerializeField] protected UnityEvent<Vector3> OnBeginJump = new UnityEvent<Vector3>();
        [SerializeField] protected UnityEvent<Vector3, float> OnFootStep = new UnityEvent<Vector3, float>();

        protected MotorState _state;
        protected CharacterMotorConfig _config;
        protected CharacterMotor _motor;

        protected float _jumpTimeRemaining = 0f;
        protected float _timeSinceLastFootstepAudio = 0f;
        protected float _timeFalling = 0f;
        protected float _originalDrag;
        private RaycastHit _groundedHitResult;

        public EJumpState JumpState { get; protected set; } = EJumpState.Neutral;
        public bool IsJumping => JumpState == EJumpState.Rising || JumpState == EJumpState.Falling;
        public int JumpCount { get; protected set; } = 0;

        public bool InCoyoteTime => CoyoteTimeRemaining > 0f;
        public bool IsGroundedOrInCoyoteTime => _state.IsGrounded || InCoyoteTime;
        public float CoyoteTimeRemaining { get; protected set; } = 0f;

        public float CurrentMaxSpeed
        {
            get
            {
                float speed = 0;

                if (IsGroundedOrInCoyoteTime || IsJumping)
                    speed = (_state.IsRunning ? _config.RunSpeed : _config.WalkSpeed) * (_state.IsCrouched ? _config.CrouchSpeedMultiplier : 1f);

                else if (_config.CanAirControl)
                    speed = (_state.IsRunning ? _config.RunSpeed : _config.WalkSpeed) * _config.AirControlMultiplier;

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
            _state.LocalGravity.ApplyGravity = true;

            state.LinkedCollider.material = _config.Material_Default;
            state.LinkedCollider.radius = _config.Radius;
            state.LinkedCollider.height = _state.CurrentHeight;
            state.LinkedCollider.center = Vector3.up * (_state.CurrentHeight * 0.5f);

            _originalDrag = state.LinkedRB.drag;
        }

        public void FixedUpdate_PreGroundedCheck()
        {
            if (_state.LocalGravity == null)
                _state.LinkedRB.AddForce(Physics.gravity, ForceMode.Acceleration);

            // align to the local gravity vector
            transform.rotation = Quaternion.FromToRotation(transform.up, _state.UpVector) * transform.rotation;
        }

        public RaycastHit FixedUpdate_GroundedCheck()
        {
            bool wasGrounded = _state.IsGrounded;
            bool wasRunning = _state.IsRunning;

            RaycastHit groundCheckResult = UpdateIsGrounded();
            _groundedHitResult = groundCheckResult;

            // activate coyote time?
            if (wasGrounded && !_state.IsGrounded)
                CoyoteTimeRemaining = _config.CoyoteTime;

            // reduce coyote time
            else if (CoyoteTimeRemaining > 0)
                CoyoteTimeRemaining -= Time.deltaTime;

            UpdateRunning(groundCheckResult);

            if (wasRunning != _state.IsRunning)
                OnRunChanged?.Invoke(_state.IsRunning);

            return groundCheckResult;
        }

        public void FixedUpdate_OnBecameGrounded()
        {
            _state.LinkedCollider.material = _config.Material_Default;
            _state.LinkedRB.drag = _originalDrag;
            _timeSinceLastFootstepAudio = 0f;
            CoyoteTimeRemaining = 0f;

            OnHitGround?.Invoke(_state.LinkedRB.position, _state.LastRequestedVelocity.magnitude);
        }

        public void FixedUpdate_TickMovement(RaycastHit groundHitResult, Vector2 movementVector)
        {
            if (!IsGroundedOrInCoyoteTime)
            {
                // track how long we have been in the air
                _state.TimeInAir += Time.deltaTime;

                if(!IsJumping || JumpState == EJumpState.Falling)
                    _timeFalling += Time.deltaTime;
            }

            UpdateMovement(movementVector);
        }

        public void LateUpdate_Tick()
        {
            UpdateCrouch();
        }

        public RaycastHit UpdateIsGrounded()
        {
            // currently performing a jump
            if (_jumpTimeRemaining > 0)
            {
                _state.IsGrounded = false;
                return new RaycastHit();
            }

            // get bottom of RB
            Vector3 startPos = _state.LinkedRB.position + _state.UpVector * _state.CurrentHeight * 0.5f;
            // get distance of raycast
            float groundCheckDistance = (_state.CurrentHeight * 0.5f) + _config.GroundedCheckBuffer;

            // perform raycast
            float radius = _config.Radius * 0.25f;
            if (Physics.SphereCast(startPos, radius, _state.DownVector, out RaycastHit groundHitResult,
                                   groundCheckDistance, _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
            {
                _state.IsGrounded = true;
                _timeFalling = 0f;
                JumpCount = 0;
                _jumpTimeRemaining = 0f;
                JumpState = EJumpState.Neutral;

                // is autoparenting enabled?
                if (_config.AutoParent)
                {
                    // auto parent to anything!
                    if (_config.AutoParentMode == CharacterMotorConfig.EAutoParentMode.Anything)
                    {
                        if (groundHitResult.transform != _state.CurrentParent)
                        {
                            _state.CurrentParent = groundHitResult.transform;
                            transform.SetParent(_state.CurrentParent, true);
                        }
                    }
                    else
                    {
                        // search for autotarget component
                        var target = groundHitResult.transform.gameObject.GetComponentInParent<CharacterMotorAutoParentTarget>();
                        if (target != null && target.transform != _state.CurrentParent)
                        {
                            _state.CurrentParent = target.transform;
                            transform.SetParent(_state.CurrentParent, true);
                        }
                    }
                }
            }

            else
                _state.IsGrounded = false;

            return groundHitResult;
        }

        public void UpdateMovement(Vector2 inputVector)
        {
            // calculate movement input
            Vector3 movementVector = transform.forward * inputVector.y + transform.right * inputVector.x;
            movementVector *= CurrentMaxSpeed;

            // maintain rb.y velocity
            //movementVector.y = _state.LinkedRB.velocity.y;

            // are we on the ground?
            if (IsGroundedOrInCoyoteTime)
            {
                // project onto the current surface
                movementVector = Vector3.ProjectOnPlane(movementVector, _groundedHitResult.normal);

                // trying to move up too steep a slope
                if (movementVector.y > 0 && Vector3.Angle(_state.UpVector, _groundedHitResult.normal) > _config.SlopeLimit)
                    movementVector = Vector3.zero;
            } // in the air
            else
                movementVector += _state.DownVector * _config.FallAcceleration * _timeFalling;

            UpdateJumping(ref movementVector);

            if (IsGroundedOrInCoyoteTime && !IsJumping)
            {
                CheckForStepUp(ref movementVector);

                UpdateFootstepAudio(inputVector);
            }

            // update the velocity
            _state.LinkedRB.velocity = Vector3.MoveTowards(_state.LinkedRB.velocity, movementVector, _config.Acceleration);
        }

        public void UpdateJumping(ref Vector3 movementVector)
        {
            // jump requested?
            bool triggeredJumpThisFrame = false;
            if (_state.Input_Jump && _motor.CanCurrentlyJump)
            {
                _state.Input_Jump = false;

                // check if we can jump
                bool triggerJump = true;
                int numJumpsPermitted = _config.CanDoubleJump ? 2 : 1;

                if (JumpCount >= numJumpsPermitted)
                    triggerJump = false;

                if (!IsGroundedOrInCoyoteTime && !IsJumping)
                    triggerJump = false;

                // jump is permitted?
                if (triggerJump)
                {
                    if (JumpCount == 0)
                        triggeredJumpThisFrame = true;

                    float jumpTime = _config.JumpTime;
                    if (_state.CurrentSurfaceSource != null)
                        jumpTime = _state.CurrentSurfaceSource.Effect(jumpTime, EEffectableParameter.JumpVelocity);

                    _state.LinkedCollider.material = _config.Material_Jumping;
                    _state.LinkedRB.drag = 0f;
                    _jumpTimeRemaining += jumpTime;
                    JumpState = EJumpState.Rising;
                    CoyoteTimeRemaining = 0f;
                    ++JumpCount;

                    OnBeginJump?.Invoke(_state.LinkedRB.position);

                    _motor.ConsumeStamina(_config.StaminaCost_Jumping);
                }
            }

            else
                _state.Input_Jump = false;

            if (JumpState == EJumpState.Rising)
            {
                // update remaining jump time if not jumping this frame
                if (!triggeredJumpThisFrame)
                    _jumpTimeRemaining -= Time.deltaTime;

                // jumping finished
                if (_jumpTimeRemaining <= 0)
                {
                    JumpState = EJumpState.Falling;
                    _timeFalling = 0f;
                }

                // damp jump velocity over jump time
                else
                {
                    // get bottom of RB
                    Vector3 startPos = _state.LinkedRB.position + _state.UpVector * _state.CurrentHeight * 0.5f;
                    // get distance of spherecast
                    float ceilingCheckRadius = _config.Radius + _config.CeilingCheckRadiusBuffer;
                    float ceilingCheckDistance = (_state.CurrentHeight * 0.5f) - _config.Radius + _config.CeilingCheckRangeBuffer;

                    // perform spherecast
                    RaycastHit ceilingHitResult;
                    if (Physics.SphereCast(startPos, ceilingCheckRadius, _state.UpVector, out ceilingHitResult,
                                           ceilingCheckDistance, _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        JumpState = EJumpState.Falling;
                        _jumpTimeRemaining = 0f;
                        movementVector.y = 0f;
                    }

                    else
                    {
                        float jumpVelocity = _state.JumpVelocity;

                        if (_state.CurrentSurfaceSource != null)
                            jumpVelocity = _state.CurrentSurfaceSource.Effect(jumpVelocity, EEffectableParameter.JumpVelocity);


                        movementVector += _state.UpVector * (jumpVelocity + Vector3.Dot(movementVector, _state.DownVector));
                    }
                }
            }
        }

        public void UpdateRunning(RaycastHit groundCheckResult)
        {
            // no longer able to run?
            if (!_motor.CanCurrentlyRun)
            {
                _state.IsRunning = false;
                return;
            }

            // stop running if no input
            if (_state.Input_Move.magnitude < float.Epsilon)
            {
                _state.IsRunning = false;
                return;
            }

            // not grounded AND not jumping
            if (!IsGroundedOrInCoyoteTime && !IsJumping)
            {
                _state.IsRunning = false;
                return;
            }

            // cannot run?
            if (!_config.CanRun)
            {
                _state.IsRunning = false;
                return;
            }

            // setup run toggle
            if (_config.IsRunToggle)
            {
                if (_state.Input_Run && !_state.IsRunning)
                    _state.IsRunning = true;
            }
            else
                _state.IsRunning = _state.Input_Run;
        }

        public void UpdateCrouch()
        {
            // do nothing if either movement or looking are locked
            if (_state.IsMovementLocked || _state.IsLookingLocked)
                return;

            // not allowed to crouch?
            if (!_config.CanCrouch)
                return;

            /* disable crouch whenever character is in air?
            // are we jumping or in the air
            if (IsJumping || !IsGroundedOrInCoyoteTime)
            {
                // crouched or transitioning to crouch
                if (_state.IsCrouched || TargetCrouchState)
                {
                    TargetCrouchState = false;
                    InCrouchTransition = true;
                }
            }
            */

            else if (_config.IsCrouchToggle)
            {
                // toggle crouch state?
                if (_state.Input_Crouch)
                {
                    _state.Input_Crouch = false;

                    _state.TargetCrouchState = !_state.TargetCrouchState;
                    _state.InCrouchTransition = true;
                }
            }

            else
            {
                // requested crouch state different to current target
                if (_state.Input_Crouch != _state.TargetCrouchState)
                {
                    _state.TargetCrouchState = _state.Input_Crouch;
                    _state.InCrouchTransition = true;
                }
            }

            // update crouch if mid transition
            if (_state.InCrouchTransition)
            {
                // update the progress
                _state.CrouchTransitionProgress = Mathf.MoveTowards(_state.CrouchTransitionProgress,
                                                             _state.TargetCrouchState ? 0f : 1f,
                                                             Time.deltaTime / _config.CrouchTransitionTime);

                // finished changing crouch state
                if (Mathf.Approximately(_state.CrouchTransitionProgress, _state.TargetCrouchState ? 0f : 1f))
                {
                    _state.IsCrouched = _state.TargetCrouchState;
                    _state.InCrouchTransition = false;
                }
            }
        }

        public void CheckForStepUp(ref Vector3 movementVector)
        {
            Vector3 lookAheadStartPoint = transform.position + _state.UpVector * (_config.StepCheck_MaxStepHeight * 0.5f);
            Vector3 lookAheadDirection = movementVector.normalized;
            float lookAheadDistance = _config.Radius + _config.StepCheck_LookAheadRange;

            // check if there is a potential step ahead
            if (Physics.Raycast(lookAheadStartPoint, lookAheadDirection, lookAheadDistance,
                                _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
            {
                lookAheadStartPoint = transform.position + _state.UpVector * _config.StepCheck_MaxStepHeight;

                // check if there is clear space above the step
                if (!Physics.Raycast(lookAheadStartPoint, lookAheadDirection, lookAheadDistance,
                                    _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 candidatePoint = lookAheadStartPoint + lookAheadDirection * lookAheadDistance;

                    // check the surface of the step
                    RaycastHit hitResult;
                    if (Physics.Raycast(candidatePoint, _state.DownVector, out hitResult, _config.StepCheck_MaxStepHeight * 2f,
                                       _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        // is the step shallow enough in slope
                        if (Vector3.Angle(_state.UpVector, hitResult.normal) <= _config.SlopeLimit)
                        {
                            _state.LinkedRB.position = hitResult.point;
                        }
                    }
                }
            }
        }

        public void UpdateFootstepAudio(Vector2 movementInput)
        {
            // is the player attempting to move?
            if (movementInput.magnitude > float.Epsilon)
            {
                // update time since last audio
                _timeSinceLastFootstepAudio += Time.deltaTime;

                // time for footstep audio?
                float footstepInterval = _state.IsRunning ? _config.FootstepInterval_Running : _config.FootstepInterval_Walking;
                if (_timeSinceLastFootstepAudio >= footstepInterval)
                {
                    OnFootStep?.Invoke(_state.LinkedRB.position, _state.LinkedRB.velocity.magnitude);

                    _timeSinceLastFootstepAudio -= footstepInterval;
                }
            }
        }
    }
}
