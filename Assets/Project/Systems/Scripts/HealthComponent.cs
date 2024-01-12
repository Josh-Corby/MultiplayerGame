using UnityEngine;
using UnityEngine.Events;

namespace Project
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        private float _maxHealth;
        private float _currentHealth;
        private float _previousHealth;
        private float _healthRecoveryDelayRemaining = 0f;
        private float _healthRecoveryRate;
        private float _healthRecoveryDelay;

        [SerializeField] private UnityEvent<float> OnTookDamage = new UnityEvent<float>();
        [SerializeField] private UnityEvent<float, float> OnHealthChanged = new UnityEvent<float, float>();
        [SerializeField] private UnityEvent<HealthComponent> OnDied = new UnityEvent<HealthComponent>();

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Update()
        {
            UpdateHealth();
            if (_previousHealth != _currentHealth)
            {
                _previousHealth = _currentHealth;
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            }
        }

        public void Init(CharacterMotorConfig config)
        {
            _maxHealth = config.MaxHealth;
            _previousHealth = _currentHealth = _maxHealth;
            _healthRecoveryRate = config.HealthRecoveryRate;
            _healthRecoveryDelay = config.HealthRecoveryDelay;
        }

        private void UpdateHealth()
        {
            if (_currentHealth < _maxHealth) // if we're able to recover
            {
                if (_healthRecoveryDelayRemaining > 0f)
                    _healthRecoveryDelayRemaining -= Time.deltaTime;

                if (_healthRecoveryDelayRemaining <= 0f)
                    _currentHealth = Mathf.Min(_currentHealth + _healthRecoveryRate * Time.deltaTime,
                                              _maxHealth);
            }
        }

        public void OnTakeDamage(GameObject source, float amount)
        {
            OnTookDamage?.Invoke(amount);

            _currentHealth = Mathf.Max(_currentHealth - amount, 0);
            _healthRecoveryDelayRemaining = _healthRecoveryDelay;
            // have we died?
            if (_currentHealth <= 0 && _previousHealth > 0)
            {
                OnDied?.Invoke(this);
            }
        }

        public void OnPerformHeal(GameObject source, float amount)
        {
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        }
    }
}
