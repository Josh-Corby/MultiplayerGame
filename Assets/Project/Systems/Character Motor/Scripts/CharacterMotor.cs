using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
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

    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMotor : NetworkBehaviour, IDamageable
    {
        public enum EJumpState
        {
            Neutral,
            Rising,
            Falling
        }

        [Header("Character Motor")]

        [SerializeField] protected CharacterMotorConfig _config;

        protected MotorState _state = new MotorState();
        protected ClientNetworkTransform _clientNetworkTransform;
        protected IMovementMode _movementMode;

        protected float _currentSurfaceLastTickTime;

        private float _previousHeight;

        protected float _previousStamina = 0f;
        protected float _staminaRecoveryDelayRemaining = 0f;

        protected float _previousHealth = 0f;
        protected float _healthRecoveryDelayRemaining = 0f;

        #region Properties

        public float CurrentStamina { get; protected set; } = 0f;
        public float CurrentHealth { get; protected set; } = 0f;
        public float MaxHealth => _config.MaxHealth;
        public bool CanCurrentlyJump => _config.CanJump && CurrentStamina >= _config.StaminaCost_Jumping;
        public bool CanCurrentlyRun => _config.CanRun && CurrentStamina > 0;
        #endregion

        #region Unity Events
        [Header("Unity Events")]
        [SerializeField] protected UnityEvent OnPrimaryAction = new();
        [SerializeField] protected UnityEvent<Vector3> OnPlayImpactGroundSound = new();
        [SerializeField] protected UnityEvent<float> OnTookDamage = new();
        [SerializeField] protected UnityEvent<float, float> OnHealthChanged = new();
        [SerializeField] protected UnityEvent<float, float> OnStaminaChanged = new();
        [SerializeField] protected UnityEvent<CharacterMotor> OnPlayerDied = new();
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

        protected virtual void Awake()
        {
            _state.LinkedRB = GetComponent<Rigidbody>();
            _state.LinkedCollider = GetComponent<CapsuleCollider>();
            _state.LocalGravity = GetComponent<GravityTracker>();
            _state.LinkedConfig = _config;

            SwitchMovementMode<MovementMode_Ground>();

            if (_movementMode == null)
                throw new NullReferenceException($"There is no IMovementMode attached to {gameObject.name}");


            _clientNetworkTransform = GetComponent<ClientNetworkTransform>();

            _state.LinkedRB.isKinematic = false;
            _previousStamina = CurrentStamina = _config.MaxStamina;
            _previousHealth = CurrentHealth = _config.MaxHealth;

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

            /*
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
            */
        }

        protected virtual void Start()
        {
            OnStaminaChanged.Invoke(CurrentStamina, _config.MaxStamina);
            OnHealthChanged.Invoke(CurrentHealth, _config.MaxHealth);
        }

        protected virtual void Update()
        {
            _networkTimer.Update(Time.deltaTime);
            _reconciliationTimer.Tick(Time.deltaTime);
            _extrapolationTimer.Tick(Time.deltaTime);
            _state.Tick(Time.deltaTime);

            UpdateStamina();
            UpdateHealth();

            if (_previousStamina != CurrentStamina)
            {
                _previousStamina = CurrentStamina;
                OnStaminaChanged.Invoke(CurrentStamina, _config.MaxStamina);
            }

            if (_previousHealth != CurrentHealth)
            {
                _previousHealth = CurrentHealth;
                OnHealthChanged?.Invoke(CurrentHealth, _config.MaxHealth);
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
            _movementMode.LateUpdate_Tick();

            float currentHeight = _state.CurrentHeight;
            if (_previousHeight != currentHeight)
            {
                _state.LinkedCollider.height = _state.CurrentHeight;
                _state.LinkedCollider.center = Vector3.up * (_state.CurrentHeight * 0.5f);
                _previousHeight = currentHeight;
            }
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

                StatePayload statePayload = new StatePayload()
                {
                    Tick = inputPayload.Tick,
                    NetworkObjectID = NetworkObjectId,
                    Rotation = _state.LinkedRB.rotation,
                    Position = _state.LinkedRB.position,
                    Velocity = _state.LinkedRB.velocity
                };

                _serverStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;
            SendToClientRPC(_serverStateBuffer.Get(bufferIndex));
            HandleExtrapolation(_serverStateBuffer.Get(bufferIndex), CalculateLatencyInMillis(inputPayload));
        }

        #region Server
        [ServerRpc]
        protected void SendToServerRPC(InputPayload input)
        {
            _serverInputQueue.Enqueue(input);
        }

        protected void HandleClientTick()
        {
            if (!IsClient || !IsOwner) return;

            var currentTick = _networkTimer.CurrentTick;
            var bufferIndex = currentTick % k_bufferSize;

            InputPayload inputPayload = new InputPayload()
            {
                Tick = currentTick,
                NetworkObjectID = NetworkObjectId,
                TimeStamp = DateTime.Now,
                InputVector = _state.Input_Move,
                Position = _state.LinkedRB.position
            };

            _clientInputBuffer.Add(inputPayload, bufferIndex);
            SendToServerRPC(inputPayload);
            StatePayload statePayload = ProcessMovement(inputPayload);
            _clientStateBuffer.Add(statePayload, bufferIndex);

            HandleServerReconciliation();
        }

        protected bool ShouldExtrapolate(float latency) => latency < _extrapolationLimit && latency > Time.fixedDeltaTime;

        protected void Extrapolate()
        {
            if (IsServer && _extrapolationTimer.IsRunning)
            {
                _state.LinkedRB.position += _extrapolationState.Position.With(y: 0);
            }
        }

        protected void HandleExtrapolation(StatePayload latest, float latency)
        {
            if (ShouldExtrapolate(latency))
            {
                if (_extrapolationState.Position != default)
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
        #endregion

        #region Client
        [ClientRpc]
        protected void SendToClientRPC(StatePayload statePayload)
        {
            if (!IsOwner) return;
            _lastServerState = statePayload;
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
            _state.LinkedRB.position = rewindState.Position;
            _state.LinkedRB.rotation = rewindState.Rotation;
            _state.LinkedRB.velocity = rewindState.Velocity;

            if (!rewindState.Equals(_lastServerState)) return;

            _clientStateBuffer.Add(rewindState, rewindState.Tick % k_bufferSize);

            // replay all inputs from the rewind state to the current state
            int tickToReplay = _lastServerState.Tick;

            while (tickToReplay < _networkTimer.CurrentTick)
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

            bufferIndex = _lastServerState.Tick % k_bufferSize;
            if (bufferIndex - 1 < 0) return; // not enough information to reconcile

            StatePayload rewindState = IsHost ? _serverStateBuffer.Get(bufferIndex - 1) : _lastServerState; // Host RPCs execute immediately, so we can use the last server state
            StatePayload clientState = IsHost ? _clientStateBuffer.Get(bufferIndex - 1) : _clientStateBuffer.Get(bufferIndex);
            positionError = Vector3.Distance(rewindState.Position, clientState.Position);

            if (positionError > _reconciliationThreshold)
            {
                ReconcileState(rewindState);
                _reconciliationTimer.Start();
            }

            _lastProcessedState = rewindState;
        }
        #endregion

        protected StatePayload ProcessMovement(InputPayload input)
        {
            Move(input.InputVector);

            return new StatePayload()
            {
                Tick = input.Tick,
                NetworkObjectID = input.NetworkObjectID,
                Position = _state.LinkedRB.transform.position,
                Rotation = _state.LinkedRB.transform.rotation,
                Velocity = _state.LinkedRB.velocity
            };
        }

        protected static float CalculateLatencyInMillis(InputPayload inputPayload)
        {
            return (DateTime.Now - inputPayload.TimeStamp).Milliseconds / 1000f;
        }

        // implementations commented out as extrapolation isn't working properly
        protected void SwitchAuthorityMode(AuthorityMode mode)
        {
            _clientNetworkTransform.authorityMode = mode;
            bool shouldSync = mode == AuthorityMode.Client;
            _clientNetworkTransform.SyncPositionX = shouldSync;
            _clientNetworkTransform.SyncPositionY = shouldSync;
            _clientNetworkTransform.SyncPositionZ = shouldSync;
        }
        #endregion

        protected void Move(Vector2 inputVector)
        {
            // movement locked?
            if (_state.IsMovementLocked)
                inputVector = Vector2.zero;

            _movementMode.FixedUpdate_PreGroundedCheck();

            bool wasGrounded = _state.IsGrounded;

            RaycastHit groundHitResult = _movementMode.FixedUpdate_GroundedCheck();

            if (_state.IsGrounded)
            {
                // check for a surface effect
                if (groundHitResult.collider.gameObject.TryGetComponent<SurfaceEffectSource>(out var surfaceEffectSource))
                    SetSurfaceEffectSource(surfaceEffectSource);
                else
                    SetSurfaceEffectSource(null);

                UpdateSurfaceEffects();

                // have we returned to the ground
                if (!wasGrounded)
                    _movementMode.FixedUpdate_OnBecameGrounded();
            }
            else
                SetSurfaceEffectSource(null);

            _movementMode.FixedUpdate_TickMovement(groundHitResult, inputVector);

            _state.LastRequestedVelocity = _state.LinkedRB.velocity;
        }


        public void AddParameterEffector(IParameterEffector newEffector)
        {
            _state.AddParameterEffector(newEffector);
        }

        public void OnPerformHeal(GameObject source, float amount)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, _config.MaxHealth);
            Debug.Log(CurrentHealth);
        }

        public void OnTakeDamage(GameObject source, float amount)
        {
            OnTookDamage.Invoke(amount);

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
            _healthRecoveryDelayRemaining = _config.HealthRecoveryDelay;

            // have we died?
            if (CurrentHealth <= 0f && _previousHealth > 0f)
                OnPlayerDied.Invoke(this);

            Debug.Log(CurrentHealth);
        }

        protected void UpdateHealth()
        {
            // do we have health to recover?
            if (CurrentHealth < _config.MaxHealth)
            {
                if (_healthRecoveryDelayRemaining > 0f)
                    _healthRecoveryDelayRemaining -= Time.deltaTime;

                if (_healthRecoveryDelayRemaining <= 0f)
                    CurrentHealth = Mathf.Min(CurrentHealth + _config.HealthRecoveryRate * Time.deltaTime,
                                              _config.MaxHealth);
            }
        }

        protected void UpdateStamina()
        {
            if (_state.IsRunning && _state.IsGrounded)
                ConsumeStamina(_config.StaminaCost_Running * Time.deltaTime);
            else if (CurrentStamina < _config.MaxStamina) // if we're able to recover
            {
                if (_staminaRecoveryDelayRemaining > 0f)
                    _staminaRecoveryDelayRemaining -= Time.deltaTime;

                if (_staminaRecoveryDelayRemaining <= 0f)
                    CurrentStamina = Mathf.Min(CurrentStamina + _config.StaminaRecoveryRate * Time.deltaTime,
                                               _config.MaxStamina);
            }
        }

        public void ConsumeStamina(float amount)
        {
            CurrentStamina = Mathf.Max(CurrentStamina - amount, 0f);
            _staminaRecoveryDelayRemaining = _config.StaminaRecoveryDelay;
        }

        protected void UpdateSurfaceEffects()
        {
            // no surface effect
            if (_state.CurrentSurfaceSource == null)
                return;

            // expire the surface effect?
            if (_currentSurfaceLastTickTime + _state.CurrentSurfaceSource.PersistenceTime < Time.time)
            {
                _state.CurrentSurfaceSource = null;
                return;
            }
        }

        protected void SetSurfaceEffectSource(SurfaceEffectSource newSource)
        {
            // changing to a new effect?
            if (newSource != null && newSource != _state.CurrentSurfaceSource)
            {
                _state.CurrentSurfaceSource = newSource;
                _currentSurfaceLastTickTime = Time.time;
            }
            // on the same source?
            else if (newSource != null && newSource == _state.CurrentSurfaceSource)
            {
                _currentSurfaceLastTickTime = Time.time;
            }
        }

        public void SwitchMovementMode<T>() where T : IMovementMode
        {
            _movementMode = GetComponent<T>();

            _movementMode.Initialise(_config, this, _state);
        }

        public void OnHitGround(Vector3 location, float impactSpeed)
        {
            if (_state.TimeInAir >= _config.MinAirTimeForLandedSound)
            {
                OnPlayImpactGroundSound?.Invoke(location);
            }

            if (impactSpeed >= _config.MinFallSpeedToTakeDamage)
            {
                float speedProportion = Mathf.InverseLerp(_config.MinFallSpeedToTakeDamage,
                                                          _config.FallSpeedForMaximumDamage,
                                                          impactSpeed);

                float damagePercentage = Mathf.Lerp(_config.MinimumFallDamagePercentage,
                                                    _config.MaximumFallDamagePercentage,
                                                    speedProportion);
                float actualDamageToApply = _config.MaxHealth * damagePercentage;
                OnTakeDamage(null, actualDamageToApply);
            }
        }
    }
}