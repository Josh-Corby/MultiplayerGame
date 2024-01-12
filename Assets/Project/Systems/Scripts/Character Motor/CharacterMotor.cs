using KBCore.Refs;
using UnityEngine;
using UnityEngine.Events;

namespace Project.Input
{
    public class CharacterMotor : ValidatedMonoBehaviour, IDamageable
    {
        public enum EJumpState
        {
            Neutral,
            Rising,
            Falling
        }

        [SerializeField, Self] protected Rigidbody _linkedRB;
        [SerializeField, Self] protected CapsuleCollider _linkedCollider;
        [SerializeField, Self] protected Animator _linkedAnimator;
        [SerializeField, Anywhere] protected CharacterMotorConfig _config;

        [SerializeField] protected UnityEvent<bool> OnRunChanged = new UnityEvent<bool>();
        [SerializeField] protected UnityEvent<Vector3> OnHitGround = new UnityEvent<Vector3>();
        [SerializeField] protected UnityEvent<Vector3> OnBeginJump = new UnityEvent<Vector3>();
        [SerializeField] protected UnityEvent<Vector3, float> OnFootStep = new UnityEvent<Vector3, float>();
        [SerializeField] protected UnityEvent<float, float> OnStaminaChanged = new UnityEvent<float, float>();
        [SerializeField] protected UnityEvent<float, float> OnHealthChanged = new UnityEvent<float, float>();
        [SerializeField] protected UnityEvent<float> OnTookDamage = new UnityEvent<float>();
        [SerializeField] protected UnityEvent<CharacterMotor> OnPlayerDied = new UnityEvent<CharacterMotor>();


        [Header("Debug Controls")]
        [SerializeField] protected bool DEBUG_OverrideMovement = false;
        [SerializeField] protected Vector2 DEBUG_MovementInput;
        [SerializeField] protected bool DEBUG_ToggleLookLock = false;
        [SerializeField] protected bool DEBUG_ToggleMovementLock = false;

        protected float JumpTimeRemaining = 0f;
        protected float _timeSinceLastFootstepAudio = 0f;
        protected float _timeInAir = 0f;

        protected float _previousStamina = 0f;
        protected float _staminaRecoveryDelayRemaining = 0f;

        protected float _previousHealth = 0f;
        protected float HealthRecoveryDelayRemaining = 0f;

        public SurfaceEffectSource CurrentSurfaceSource { get; protected set; } = null;
        protected float _currentSurfaceLastTickTime;

        protected RaycastHit _groundedHitResult;
        protected StateMachine _stateMachine;

        public bool IsMovementLocked { get; protected set; } = false;
        public bool IsLookingLocked { get; protected set; } = false;
        public EJumpState JumpState { get; protected set; } = EJumpState.Neutral;
        public bool IsJumping => JumpState == EJumpState.Rising || JumpState == EJumpState.Falling;
        public int JumpCount { get; protected set; } = 0;
        public bool IsRunning { get; protected set; } = false;
        public bool IsGrounded { get; protected set; } = true;
        public bool InCoyoteTime => CoyoteTimeRemaining > 0f;
        public bool IsGroundedOrInCoyoteTime => IsGrounded || InCoyoteTime;
        public bool IsCrouched { get; protected set; } = false; 
        public float OriginalDrag { get; protected set; }
        public bool InCrouchTransition { get; protected set; } = false;
        public bool TargetCrouchState { get; protected set; } = false;
        public Transform CurrentParent { get; protected set; } = null;
        public float CrouchTransitionProgress { get; protected set; } = 1f;
        public float CoyoteTimeRemaining { get; protected set; } = 0f;
        public float CurrentStamina { get; protected set; } = 0f;
        public float CurrentHealth { get; protected set; } = 0f;
        public bool CanCurrentlyJump => _config.CanJump && CurrentStamina >= _config.StaminaCost_Jumping;
        public bool CanCurrentlyRun => _config.CanRun && CurrentStamina > 0f;

        public float CurrentHeight
        {
            get
            {
                if(InCrouchTransition)
                {
                    return Mathf.Lerp(_config.CrouchHeight, _config.Height, CrouchTransitionProgress);
                }

                return IsCrouched? _config.CrouchHeight : _config.Height;
            }
        }

        public float CurrentMaxSpeed
        {
            get
            {
                float speed = 0;

                if (IsGroundedOrInCoyoteTime || IsJumping)
                    speed = (IsRunning ? _config.RunSpeed : _config.WalkSpeed) * (IsCrouched ? _config.CrouchSpeedMultiplier : 1f);

                else if (_config.CanAirControl)
                    speed = (IsRunning ? _config.RunSpeed : _config.WalkSpeed) * _config.AirControlMultiplier;

                return CurrentSurfaceSource != null ? CurrentSurfaceSource.Effect(speed, EEffectableParameter.Speed) : speed;
            }
        }

        protected Vector2 _input_Move;
        protected Vector2 _input_Look;
        protected bool _input_Jump;
        protected bool _input_Run;
        protected bool _input_Crouch;
        protected bool _input_PrimaryAction;

        protected virtual void Awake()
        {
            _previousStamina = CurrentStamina = _config.MaxStamina;
            _previousHealth = CurrentHealth = _config.MaxHealth;

            // state machine
            _stateMachine = new StateMachine();

            // declare states
            var locomotionState = new LocomotionState(this, _linkedAnimator);
            var jumpState = new JumpState(this, _linkedAnimator);

            // define transition
            At(locomotionState, jumpState, new FuncPredicate(() => IsJumping));
            At(jumpState, locomotionState, new FuncPredicate(() => IsGrounded && !IsJumping));

            // set initial state
            _stateMachine.SetState(locomotionState);
        }

        protected virtual void Start()
        {
            _linkedCollider.material = _config.Material_Default;
            _linkedCollider.radius = _config.Radius;
            _linkedCollider.height = CurrentHeight;
            _linkedCollider.center = Vector3.up * (CurrentHeight * 0.5f);


            OriginalDrag = _linkedRB.drag;

            OnStaminaChanged?.Invoke(CurrentStamina, _config.MaxStamina);
            OnHealthChanged?.Invoke(CurrentHealth, _config.MaxHealth);
        }

        protected void Update()
        {
            if (DEBUG_ToggleLookLock)
            {
                DEBUG_ToggleLookLock = false;
                IsLookingLocked = !IsLookingLocked;
            }

            if (DEBUG_ToggleMovementLock)
            {
                DEBUG_ToggleMovementLock = false;
                IsMovementLocked = !IsMovementLocked;
            }

            UpdateHealth();
            UpdateStamina();

            if (_previousStamina != CurrentStamina)
            {
                _previousStamina = CurrentStamina;
                OnStaminaChanged?.Invoke(CurrentStamina, _config.MaxStamina);
            }

            if(_previousHealth != CurrentHealth)
            {
                _previousHealth = CurrentHealth;
                OnHealthChanged?.Invoke(CurrentHealth, _config.MaxHealth);
            }

            _stateMachine.Update();
        }

        protected void FixedUpdate()
        {
            bool wasGrounded = IsGrounded;
            bool wasRunning = IsRunning;

            _groundedHitResult = UpdateIsGrounded();

            UpdateSurfaceEffects();

            // activate coyote time?
            if (wasGrounded && !IsGrounded)
                CoyoteTimeRemaining = _config.CoyoteTime;

            // reduce coyote time
            else if (CoyoteTimeRemaining > 0)
                CoyoteTimeRemaining -= Time.deltaTime;

            UpdateRunning(_groundedHitResult);

            if(wasRunning != IsRunning)
                OnRunChanged?.Invoke(IsRunning);

            // switch back to grounded material
            if(!wasGrounded && IsGrounded)
            {
                _linkedCollider.material = _config.Material_Default;
                _linkedRB.drag = OriginalDrag;
                _timeSinceLastFootstepAudio = 0f;
                CoyoteTimeRemaining = 0f;

                if(_timeInAir >= _config.MinAirTimeForLandedSound)
                    OnHitGround?.Invoke(_linkedRB.position);
            }

            // track how long we have been in the air
            _timeInAir = IsGroundedOrInCoyoteTime ? 0f : (_timeInAir + Time.deltaTime);

            _stateMachine.FixedUpdate();
            //UpdateMovement();
        }

        protected virtual void LateUpdate()
        {
            UpdateCrouched();
        }

        void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);


        #region Movement
        protected RaycastHit UpdateIsGrounded()
        {
            // currently performing a jump
            if (JumpTimeRemaining > 0)
            {
                IsGrounded = false;
                return new RaycastHit();
            }

            // get bottom of RB
            Vector3 startPos = _linkedRB.position + Vector3.up * CurrentHeight * 0.5f;
            // get distance of raycast
            float groundCheckDistance = (CurrentHeight * 0.5f) + _config.GroundedCheckBuffer;

            // perform raycast
            RaycastHit groundHitResult;
            if (Physics.Raycast(startPos, Vector3.down, out groundHitResult,
                                groundCheckDistance, _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
            {
                IsGrounded = true;
                JumpCount = 0;
                JumpTimeRemaining = 0f;
                JumpState = EJumpState.Neutral;

                // check for a surface effect
                SurfaceEffectSource surfaceEffectSource = null;
                if (groundHitResult.collider.gameObject.TryGetComponent<SurfaceEffectSource>(out surfaceEffectSource))
                    SetSurfaceEffectSource(surfaceEffectSource);
                else
                    SetSurfaceEffectSource(null);

                // is autoparenting enabled?
                if (_config.AutoParent)
                {
                    // auto parent to anything!
                    if (_config.AutoParentMode == CharacterMotorConfig.EAutoParentMode.Anything)
                    {
                        if (groundHitResult.transform != CurrentParent)
                        {
                            CurrentParent = groundHitResult.transform;
                            transform.SetParent(CurrentParent, true);
                        }
                    }
                    else
                    {
                        // search for autotarget component
                        var target = groundHitResult.transform.gameObject.GetComponentInParent<CharacterMotorAutoParentTarget>();
                        if(target != null && target.transform != CurrentParent)
                        {
                            CurrentParent = target.transform;
                            transform.SetParent(CurrentParent, true);
                        }
                    }
                }       
            }

            else
            {
                IsGrounded = false;
                SetSurfaceEffectSource(null);
            }

            return groundHitResult;
        }
        public void UpdateMovement()
        {
            if (DEBUG_OverrideMovement)
                _input_Move = DEBUG_MovementInput;

            // movement locked?
            if (IsMovementLocked)
                _input_Move = Vector2.zero;

            // calculate movement input
            Vector3 movementVector = transform.forward * _input_Move.y + transform.right * _input_Move.x;
            movementVector *= CurrentMaxSpeed;

            // maintain rb.y velocity
            movementVector.y = _linkedRB.velocity.y;

            // are we on the ground?
            if(IsGroundedOrInCoyoteTime)
            {
                // project onto the current surface
                movementVector = Vector3.ProjectOnPlane(movementVector, _groundedHitResult.normal);

                // trying to move up too steep a slope
                if(movementVector.y > 0 && Vector3.Angle(Vector3.up, _groundedHitResult.normal) > _config.SlopeLimit)
                    movementVector = Vector3.zero;              
            } // in the air
            else
                movementVector += Vector3.down * _config.FallVelocity * Time.fixedDeltaTime;

            UpdateJumping(ref movementVector);

            if (IsGroundedOrInCoyoteTime && !IsJumping)
            {
                CheckForStepUp(ref movementVector);

                UpdateFootstepAudio();
            }

            // update the velocity
            _linkedRB.velocity = Vector3.MoveTowards(_linkedRB.velocity, movementVector, _config.Acceleration);
        }
        protected void UpdateJumping(ref Vector3 movementVector)
        {
            // jump requested?
            bool triggeredJumpThisFrame = false;
            if (_input_Jump && CanCurrentlyJump)
            {
                _input_Jump = false;

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
                    if (CurrentSurfaceSource != null)
                        jumpTime = CurrentSurfaceSource.Effect(jumpTime, EEffectableParameter.JumpVelocity);

                    _linkedCollider.material = _config.Material_Jumping;
                    _linkedRB.drag = 0f;
                    JumpTimeRemaining += jumpTime;
                    JumpState = EJumpState.Rising;
                    CoyoteTimeRemaining = 0f;
                    ++JumpCount;

                    OnBeginJump?.Invoke(_linkedRB.position);

                    ConsumeStamina(_config.StaminaCost_Jumping);
                }
            }

            else
                _input_Jump = false;

            if (JumpState == EJumpState.Rising)
            {
                // update remaining jump time if not jumping this frame
                if (!triggeredJumpThisFrame)
                    JumpTimeRemaining -= Time.deltaTime;

                // jumping finished
                if (JumpTimeRemaining <= 0)
                {
                    JumpState = EJumpState.Falling;
                }

                // damp jump velocity over jump time
                else
                {
                    // get bottom of RB
                    Vector3 startPos = _linkedRB.position + Vector3.up * CurrentHeight * 0.5f;
                    // get distance of spherecast
                    float ceilingCheckRadius = _config.Radius + _config.CeilingCheckRadiusBuffer;
                    float ceilingCheckDistance = (CurrentHeight * 0.5f) - _config.Radius + _config.CeilingCheckRangeBuffer;

                    // perform spherecast
                    RaycastHit ceilingHitResult;
                    if (Physics.SphereCast(startPos, ceilingCheckRadius, Vector3.up, out ceilingHitResult,
                                           ceilingCheckDistance, _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        JumpState = EJumpState.Falling;
                        JumpTimeRemaining = 0f;
                        movementVector.y = 0f; 
                    }

                    else
                    {
                        float jumpVelocity = _config.JumpVelocity;

                        if(CurrentSurfaceSource != null)
                            jumpVelocity = CurrentSurfaceSource.Effect(jumpVelocity, EEffectableParameter.JumpVelocity);
                        movementVector.y = jumpVelocity * (JumpTimeRemaining / _config.JumpTime);
                    }
                }
            }
        }
        protected void UpdateRunning(RaycastHit groundCheckResult)
        {
            // no longer able to run?
            if (!CanCurrentlyRun)
            {
                IsRunning = false; 
                return;
            }

            // stop running if no input
            if (_input_Move.magnitude < float.Epsilon)
            {
                IsRunning = false;
                return;
            }

            // not grounded AND not jumping
            if(!IsGroundedOrInCoyoteTime && !IsJumping)
            {
                IsRunning = false;
                return;
            }

            // cannot run?
            if(!_config.CanRun)
            {
                IsRunning = false;
                return;
            }

            // setup run toggle
            if(_config.IsRunToggle)
            {
                if (_input_Run && !IsRunning)
                    IsRunning = true;
            }
            else
                IsRunning = _input_Run;
        }     
        protected void UpdateCrouched()
        {
            // do nothing if either movement or looking are locked
            if (IsMovementLocked || IsLookingLocked)
                return;

            // not allowed to crouch?
            if (!_config.CanCrouch)
                return;

            /* disable crouch whenever character is in air?
            // are we jumping or in the air
            if (IsJumping || !IsGroundedOrInCoyoteTime)
            {
                // crouched or transitioning to crouch
                if (IsCrouched || TargetCrouchState)
                {
                    TargetCrouchState = false;
                    InCrouchTransition = true;
                }
            }
            */

            else if (_config.IsCrouchToggle)
            {
                // toggle crouch state?
                if (_input_Crouch)
                {
                    _input_Crouch = false;

                    TargetCrouchState = !TargetCrouchState;
                    InCrouchTransition = true;
                }
            }

            else
            {
                // requested crouch state different to current target
                if(_input_Crouch != TargetCrouchState)
                {
                    TargetCrouchState = _input_Crouch; 
                    InCrouchTransition = true;
                }
            }

            // update crouch if mid transition
            if (InCrouchTransition)
            {
                // update the progress
                CrouchTransitionProgress = Mathf.MoveTowards(CrouchTransitionProgress,
                                                             TargetCrouchState ? 0f : 1f,
                                                             Time.deltaTime / _config.CrouchTransitionTime);

                // update the collider and camera
                _linkedCollider.height = CurrentHeight;
                _linkedCollider.center = Vector3.up * (CurrentHeight * 0.5f);
                
                // finished changing crouch state
                if(Mathf.Approximately(CrouchTransitionProgress, TargetCrouchState ? 0f : 1f))
                {
                    IsCrouched = TargetCrouchState;
                    InCrouchTransition = false;
                }
            }
        }
        public void SetMovementLock(bool locked)
        {
            IsMovementLocked = locked;
        }
        protected void CheckForStepUp(ref Vector3 movementVector)
        {
            Vector3 lookAheadStartPoint = transform.position + Vector3.up * (_config.StepCheck_MaxStepHeight * 0.5f);
            Vector3 lookAheadDirection = movementVector.normalized;
            float lookAheadDistance = _config.Radius + _config.StepCheck_LookAheadRange;

            // check if there is a potential step ahead
            if (Physics.Raycast(lookAheadStartPoint, lookAheadDirection, lookAheadDistance, 
                                _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
            {
                lookAheadStartPoint = transform.position + Vector3.up * _config.StepCheck_MaxStepHeight;

                // check if there is clear space above the step
                if (!Physics.Raycast(lookAheadStartPoint, lookAheadDirection, lookAheadDistance,
                                    _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 candidatePoint = lookAheadStartPoint + lookAheadDirection * lookAheadDistance;

                    // check the surface of the step
                    RaycastHit hitResult;
                    if(Physics.Raycast(candidatePoint, Vector3.down, out hitResult, _config.StepCheck_MaxStepHeight * 2f, 
                                       _config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        // is the step shallow enough in slope
                        if(Vector3.Angle(Vector3.up,hitResult.normal) <= _config.SlopeLimit)
                        {
                            _linkedRB.position = hitResult.point;
                        }
                    }
                }
            }
        }
        #endregion

        #region Health
        protected void UpdateHealth()
        {
            if (CurrentHealth < _config.MaxHealth) // if we're able to recover
            {
                if (HealthRecoveryDelayRemaining > 0f)
                    HealthRecoveryDelayRemaining -= Time.deltaTime;

                if (HealthRecoveryDelayRemaining <= 0f)
                    CurrentHealth = Mathf.Min(CurrentHealth + _config.HealthRecoveryRate * Time.deltaTime,
                                              _config.MaxHealth);
            }
        }
        public virtual void OnTakeDamage(GameObject source, float amount)
        {
            OnTookDamage?.Invoke(amount);

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
            HealthRecoveryDelayRemaining = _config.HealthRecoveryDelay;
            // have we died?
            if(CurrentHealth <= 0 && _previousHealth > 0)
            {
                OnPlayerDied?.Invoke(this);
            }
        }
        public void OnPerformHeal(GameObject source, float amount)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, _config.MaxHealth);
        }
        #endregion

        #region Stamina
        protected void UpdateStamina()
        {
            // if we're running consume stamina
            if (IsRunning && IsGrounded)
                ConsumeStamina(_config.StaminaCost_Running * Time.deltaTime);
            else if(CurrentStamina < _config.MaxStamina) // if we're able to recover
            {
                if(_staminaRecoveryDelayRemaining > 0f)
                _staminaRecoveryDelayRemaining -= Time.deltaTime;

                if (_staminaRecoveryDelayRemaining <= 0f)
                    CurrentStamina = Mathf.Min(CurrentStamina + _config.StaminaRecoveryRate * Time.deltaTime,
                                              _config.MaxStamina);
            }
        }
        protected void ConsumeStamina(float amount)
        {
            CurrentStamina = Mathf.Max(CurrentStamina - amount, 0f);
            _staminaRecoveryDelayRemaining = _config.StaminaRecoveryDelay;
        }
        #endregion

        protected void UpdateFootstepAudio()
        {
            // is the player attempting to move?
            if (_input_Move.magnitude > float.Epsilon)
            {
                // update time since last audio
                _timeSinceLastFootstepAudio += Time.deltaTime;

                // time for footstep audio?
                float footstepInterval = IsRunning ? _config.FootstepInterval_Running : _config.FootstepInterval_Walking;
                if (_timeSinceLastFootstepAudio >= footstepInterval)
                {
                    OnFootStep?.Invoke(_linkedRB.position, _linkedRB.velocity.magnitude);

                    _timeSinceLastFootstepAudio -= footstepInterval;
                }
            }           
        }

        #region Surfaces
        protected void UpdateSurfaceEffects()
        {
            // no surface effect
            if (CurrentSurfaceSource == null)
                return;

            // expire the surface effect?
            if (_currentSurfaceLastTickTime + CurrentSurfaceSource.PersistenceTime < Time.time)
            {
                CurrentSurfaceSource = null;
                return;
            }
        }

        protected void SetSurfaceEffectSource(SurfaceEffectSource newSource)
        {
            // changing to a new effect?
            if (newSource != null && newSource != CurrentSurfaceSource)
            {
                CurrentSurfaceSource = newSource;
                _currentSurfaceLastTickTime = Time.time;
            }
            // on the same source?
            else if (newSource != null && newSource == CurrentSurfaceSource)
            {
                _currentSurfaceLastTickTime = Time.time;
            }
        }
        #endregion
    }
}
