using Kart;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Utilities;

namespace Project
{
    // Network variables should be value objects
    public struct InputPayload : INetworkSerializable
    {
        public int Tick;
        public ulong NetworkObjectID;
        public DateTime TimeStamp;
        public Vector2 InputVector;
        public Vector3 Position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref NetworkObjectID);
            serializer.SerializeValue(ref TimeStamp);
            serializer.SerializeValue(ref InputVector);
            serializer.SerializeValue(ref Position);
        }
    }

    public struct StatePayload : INetworkSerializable
    {
        public int Tick;
        public ulong NetworkObjectID;
        public Quaternion Rotation;
        public Vector3 Position;
        public Vector3 Velocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref NetworkObjectID);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
        }
    }

    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(StaminaComponent))]
    public class CharacterMotor : NetworkBehaviour
    {
        public enum EJumpState
        {
            Neutral,
            Rising,
            Falling
        }

        #region References
        [SerializeField] protected Rigidbody _linkedRB;
        [SerializeField] protected CapsuleCollider _linkedCollider;
        [SerializeField] protected Animator _linkedAnimator;
        [SerializeField] protected CharacterMotorConfig _config;
        [SerializeField] protected HealthComponent _healthComponent;
        [SerializeField] protected StaminaComponent _staminaComponent;

        protected ClientNetworkTransform _clientNetworkTransform;
        #endregion

        #region Fields
        protected Vector2 _input_Move;
        protected Vector2 _input_Look;
        protected bool _input_Jump;
        protected bool _input_Run;
        protected bool _input_Crouch;
        protected bool _input_PrimaryAction;

        protected float JumpTimeRemaining = 0f;
        protected float _timeSinceLastFootstepAudio = 0f;
        protected float _timeInAir = 0f;
        protected float _currentSurfaceLastTickTime;
        protected RaycastHit _groundedHitResult;
        #endregion

        #region Properties
        public SurfaceEffectSource CurrentSurfaceSource { get; protected set; } = null;
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
        public bool CanCurrentlyJump => _config.CanJump && _staminaComponent.CanJump;
        public bool CanCurrentlyRun => _config.CanRun && _staminaComponent.CanRun;

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
        #endregion

        #region Unity Events
        [Header("Unity Events")]
        [SerializeField] protected UnityEvent<bool> OnRunChanged = new UnityEvent<bool>();
        [SerializeField] protected UnityEvent<Vector3> OnHitGround = new UnityEvent<Vector3>();
        [SerializeField] protected UnityEvent<Vector3> OnBeginJump = new UnityEvent<Vector3>();
        [SerializeField] protected UnityEvent<Vector3, float> OnFootStep = new UnityEvent<Vector3, float>();
        #endregion

        #region Netcode
        // netcode general
        protected NetworkTimer _networkTimer;
        protected const float k_serverTickRate = 60f; // 60 pfs
        protected const int k_bufferSize = 1024;

        // netcode client specific
        protected CircularBuffer<StatePayload> _clientStateBuffer;
        protected CircularBuffer<InputPayload> _clientInputBuffer;
        protected StatePayload _lastServerState;
        protected StatePayload _lastProcessedState;

        // netcode server specific
        protected CircularBuffer<StatePayload> _serverStateBuffer;
        protected Queue<InputPayload> _serverInputQueue;

        [Header("Netcode")]
        [SerializeField] protected float _reconciliationCooldownTime = 1f;
        [SerializeField] protected float _reconciliationThreshold = 10f;
        protected CountdownTimer _reconciliationTimer;

        [SerializeField] protected float _extrapolationLimit = 0.5f; // 500 milliseconds
        [SerializeField] protected float _extrapolationMultiplier = 1.2f;
        protected StatePayload _extrapolationState;
        protected CountdownTimer _extrapolationTimer;
        #endregion

        protected void Awake()
        {          
            Init();

            _clientNetworkTransform = GetComponent<ClientNetworkTransform>();

            _networkTimer = new NetworkTimer(k_serverTickRate);
            _clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);

            _serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _serverInputQueue = new Queue<InputPayload>();

            _reconciliationTimer = new CountdownTimer(_reconciliationCooldownTime);
            _extrapolationTimer = new CountdownTimer(_extrapolationLimit);

            _reconciliationTimer.OnTimerStart += () =>
            {
                _extrapolationTimer.Stop();
            };

            _extrapolationTimer.OnTimerStart += () =>
            {
                _reconciliationTimer.Stop();
                SwitchAuthorityMode(AuthorityMode.Server);
            };

            _extrapolationTimer.OnTimerStop += () =>
            {
                _extrapolationState = default;
                SwitchAuthorityMode(AuthorityMode.Client);
            };
        }

        protected void Update()
        {
            _networkTimer.Update(Time.deltaTime);
            _reconciliationTimer.Tick(Time.deltaTime);
            _extrapolationTimer.Tick(Time.deltaTime);

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                transform.position += transform.forward * 20f;
            }

            // run on Update or FixedUpdate, or both - depends on the game, consider exposing an option to the editor
            Extrapolate();
        }

        protected void FixedUpdate()
        {
            while (_networkTimer.ShouldTick())
            {
                HandleClientTick();
                HandleServerTick();
            }

            // run on Update or FixedUpdate, or both - depends on the game, consider exposing an option to the editor
            Extrapolate();
        }

        protected virtual void LateUpdate()
        {
            UpdateCrouched();
        }

        protected void Init()
        {
            _healthComponent.Init(_config);
            _staminaComponent.Init(_config);

            _linkedCollider.material = _config.Material_Default;
            _linkedCollider.radius = _config.Radius;
            _linkedCollider.height = CurrentHeight;
            _linkedCollider.center = Vector3.up * (CurrentHeight * 0.5f);
            OriginalDrag = _linkedRB.drag;
        }

        #region Netcode
        protected void HandleServerTick()
        {
            if (!IsServer) return;

            var bufferIndex = -1;
            InputPayload inputPayload = default;

            while (_serverInputQueue.Count > 0)
            {
                inputPayload = _serverInputQueue.Dequeue();

                bufferIndex = inputPayload.Tick % k_bufferSize;

                Debug.Log("Server movement");
                StatePayload statePayload = ProcessMovement(inputPayload);
                _serverStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;
            SendToClientRPC(_serverStateBuffer.Get(bufferIndex));
            HandleExtrapolation(_serverStateBuffer.Get(bufferIndex), CalculateLatencyInMillis(inputPayload));
        }

        protected void HandleClientTick()
        {
            if (!IsClient || !IsOwner) return;

            Debug.Log("client tick");
            var currentTick = _networkTimer.CurrentTick;
            var bufferIndex = currentTick % k_bufferSize;

            InputPayload inputPayload = new InputPayload()
            {
                Tick = currentTick,
                NetworkObjectID = NetworkObjectId,
                TimeStamp = DateTime.Now,
                InputVector = _input_Move,
                Position = _linkedRB.position
            };

            _clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRPC(inputPayload);

            StatePayload statePayload = new StatePayload()
            {
                Tick = currentTick,
                NetworkObjectID = NetworkObjectId,
                Rotation = _linkedRB.rotation,
                Position = _linkedRB.position,
                Velocity = _linkedRB.velocity
            };

            _clientStateBuffer.Add(statePayload, bufferIndex);

            HandleServerReconciliation();
        }

        [ClientRpc]
        protected void SendToClientRPC(StatePayload statePayload)
        {
            if (!IsOwner) return;
            _lastServerState = statePayload;
        }

        [ServerRpc]
        protected void SendToServerRPC(InputPayload input)
        {
            _serverInputQueue.Enqueue(input);
        }

        protected bool ShouldExtrapolate(float latency) => latency < _extrapolationLimit && latency > Time.fixedDeltaTime;

        protected void Extrapolate()
        {
            if(IsServer && _extrapolationTimer.IsRunning)
            {
                _linkedRB.position += _extrapolationState.Position.With(y: 0);
            }
        }

        protected void HandleExtrapolation(StatePayload latest, float latency)
        {
            if(ShouldExtrapolate(latency))
            {
                if(_extrapolationState.Position != default)
                {
                    latest = _extrapolationState;
                }

                var positionAdjustment = latest.Velocity * (1 + latency * _extrapolationMultiplier);
                _extrapolationState.Position = positionAdjustment;
                _extrapolationState.Rotation = latest.Rotation;
                _extrapolationState.Velocity = latest.Velocity;
                _extrapolationTimer.Start();
            }

            else
            {
                _extrapolationTimer.Stop();
                // reconcile if desired
            }
        }

        protected bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = _lastProcessedState.Equals(default) 
                                                   || !_lastProcessedState.Equals(_lastServerState);

            return isNewServerState && isLastStateUndefinedOrDifferent && !_reconciliationTimer.IsRunning && !_extrapolationTimer.IsRunning;
        }

        protected void ReconcileState(StatePayload rewindState)
        {
            _linkedRB.position = rewindState.Position;
            _linkedRB.rotation = rewindState.Rotation;
            _linkedRB.velocity = rewindState.Velocity;

            if (!rewindState.Equals(_lastServerState)) return;

            _clientStateBuffer.Add(rewindState, rewindState.Tick);

            // replay all inputs from the rewind state to the current state
            int tickToReplay = _lastServerState.Tick;

            while(tickToReplay < _networkTimer.CurrentTick)
            {          
                int bufferIndex = tickToReplay % k_bufferSize;
                StatePayload statePayload = ProcessMovement(_clientInputBuffer.Get(bufferIndex));
                _clientStateBuffer.Add(statePayload, bufferIndex);
                tickToReplay++;
            }
        }

        protected void HandleServerReconciliation()
        {
            if (!ShouldReconcile()) return;

            float positionError;
            int bufferIndex;
            StatePayload rewindState = default;

            bufferIndex = _lastServerState.Tick % k_bufferSize;
            if (bufferIndex - 1 < 0) return; // not enough information to reconcile

            rewindState = IsHost ? _serverStateBuffer.Get(bufferIndex - 1) : _lastServerState; // Host RPCs execute immediately, so we can use the last server state
            positionError = Vector3.Distance(rewindState.Position, _clientStateBuffer.Get(bufferIndex).Position);

            if (positionError > _reconciliationThreshold)
            {
                ReconcileState(rewindState);
                _reconciliationTimer.Start();
            }

            _lastProcessedState = _lastServerState;
        }

        protected StatePayload ProcessMovement(InputPayload input)
        {
            Move();

            return new StatePayload()
            {
                Tick = input.Tick,
                NetworkObjectID = input.NetworkObjectID,
                Position = _linkedRB.transform.position,
                Rotation = _linkedRB.transform.rotation,
                Velocity = _linkedRB.velocity
            };
        }

        protected static float CalculateLatencyInMillis(InputPayload inputPayload)
        {
            return (DateTime.Now - inputPayload.TimeStamp).Milliseconds / 1000f;
        }

        protected void SwitchAuthorityMode(AuthorityMode mode)
        {
            _clientNetworkTransform.authorityMode = mode;
            bool shouldSync = mode == AuthorityMode.Client;
            _clientNetworkTransform.SyncPositionX = shouldSync;
            _clientNetworkTransform.SyncPositionY = shouldSync;
            _clientNetworkTransform.SyncPositionZ = shouldSync;
        }

        #endregion

        #region Movement

        protected void Move()
        {
            bool wasGrounded = IsGrounded;
            bool wasRunning = IsRunning;

            _groundedHitResult = UpdateIsGrounded();

            UpdateSurfaceEffects();
            UpdateCoyoteTime(wasGrounded);
            UpdateRunning(_groundedHitResult);

            if (wasRunning != IsRunning)
                OnRunChanged?.Invoke(IsRunning);

            // switch back to grounded material
            if (!wasGrounded && IsGrounded)
                OnLanded();

            // track how long we have been in the air
            _timeInAir = IsGroundedOrInCoyoteTime ? 0f : (_timeInAir + Time.deltaTime);

            UpdateMovement(_input_Move);
        }

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

        protected void UpdateMovement(Vector2 inputVector)
        {
            // movement locked?
            if (IsMovementLocked)
                inputVector = Vector2.zero;

            _input_Move = inputVector;

            Debug.Log(inputVector);
            // calculate movement input
            Vector3 movementVector = transform.forward * inputVector.y + transform.right * inputVector.x;
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
                movementVector += Vector3.down * _config.FallVelocity * (_networkTimer.MinTimeBetweenTicks / (1f / Time.fixedDeltaTime));

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

                    _staminaComponent.ConsumeStamina(_config.StaminaCost_Jumping);
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

        protected void UpdateCoyoteTime(bool wasGrounded)
        {
            // activate coyote time?
            if (wasGrounded && !IsGrounded)
                CoyoteTimeRemaining = _config.CoyoteTime;

            // reduce coyote time
            else if (CoyoteTimeRemaining > 0)
                CoyoteTimeRemaining -= Time.deltaTime;
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

        protected void OnLanded()
        {
            _linkedCollider.material = _config.Material_Default;
            _linkedRB.drag = OriginalDrag;
            _timeSinceLastFootstepAudio = 0f;
            CoyoteTimeRemaining = 0f;

            if (_timeInAir >= _config.MinAirTimeForLandedSound)
                OnHitGround?.Invoke(_linkedRB.position);
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

        public void SetMovementLock(bool locked)
        {
            IsMovementLocked = locked;
        }

        #endregion

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
                    Debug.Log("Play footstep");
                    OnFootStep?.Invoke(_linkedRB.position, _linkedRB.velocity.magnitude);

                    _timeSinceLastFootstepAudio -= footstepInterval;
                }
            }           
        }
    }
}