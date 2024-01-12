using KBCore.Refs;
using UnityEngine;
using UnityEngine.Events;

namespace Project
{
    public class StaminaComponent : ValidatedMonoBehaviour
    {
        [SerializeField, Self] CharacterMotor _motor;

        private float _maxStamina;
        private float _currentStamina;
        private float _previousStamina = 0f;

        private float _staminaRecoveryDelayRemaining = 0f;
        private float _staminaRecoveryRate;
        private float _staminaRecoveryDelay;

        private float _staminaCost_Running;
        private float _staminaCost_Jumping;

        public bool CanRun => _currentStamina > 0f;
        public bool CanJump => _currentStamina >= _staminaCost_Jumping;

        [SerializeField] protected UnityEvent<float, float> OnStaminaChanged = new UnityEvent<float, float>();

        private void Start()
        {
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        }

        private void Update()
        {
            UpdateStamina();

            if (_previousStamina != _currentStamina)
            {
                _previousStamina = _currentStamina;
                OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
            }
        }

        public void Init(CharacterMotorConfig config)
        {
            _maxStamina = config.MaxStamina;
            _previousStamina = _currentStamina = _maxStamina;
            _staminaRecoveryRate = config.StaminaRecoveryRate;
            _staminaRecoveryDelay = config.StaminaRecoveryDelay;

            _staminaCost_Running = config.StaminaCost_Running;
            _staminaCost_Jumping = config.StaminaCost_Jumping;
        }

        private void UpdateStamina()
        {
            // if we're running consume stamina
            if (_motor.IsRunning && _motor.IsGrounded)
                ConsumeStamina(_staminaCost_Running * Time.deltaTime);
            else if (_currentStamina < _maxStamina) // if we're able to recover
            {
                if (_staminaRecoveryDelayRemaining > 0f)
                    _staminaRecoveryDelayRemaining -= Time.deltaTime;

                if (_staminaRecoveryDelayRemaining <= 0f)
                    _currentStamina = Mathf.Min(_currentStamina + _staminaRecoveryRate * Time.deltaTime,
                                              _maxStamina);
            }
        }

        public void ConsumeStamina(float amount)
        {
            _currentStamina = Mathf.Max(_currentStamina - amount, 0f);
            _staminaRecoveryDelayRemaining = _staminaRecoveryDelay;
        }
    }
}
