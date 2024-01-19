using KBCore.Refs;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class CharacterUI_HealthBar : ValidatedMonoBehaviour
    {
        [SerializeField, Self] private RectTransform _healthBarTransform;
        [SerializeField, Self] private Image _healthBarImage;
        [SerializeField] Gradient _healthBarGradient;

        protected float _maxBarLength;

        private void Start()
        {
            if(_healthBarTransform.rect.width > 0)
                _maxBarLength = _healthBarTransform.rect.width;
        }

        public void OnHealthChanged(float currentHealth, float maxHealth)
        {
            if(_maxBarLength == 0)
                _maxBarLength = _healthBarTransform.rect.width;

            float staminaPercentage = currentHealth / maxHealth;
            float newLength = _maxBarLength * staminaPercentage;

            _healthBarTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newLength);
            _healthBarImage.color = _healthBarGradient.Evaluate(staminaPercentage);
        }
    }
}
