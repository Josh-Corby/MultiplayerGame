using KBCore.Refs;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class CharacterUI_StaminaBar : ValidatedMonoBehaviour
    {
        [SerializeField, Self] private RectTransform _staminaBarTransform;
        [SerializeField, Self] private Image _staminaBarImage;
        [SerializeField] Gradient _staminaBarGradient;

        protected float _maxBarLength;

        private void Start()
        {
            if(_staminaBarTransform.rect.width > 0)
                _maxBarLength = _staminaBarTransform.rect.width;
        }

        public void OnStaminaChanged(float currentStamina, float maxStamina)
        {
            if (_maxBarLength == 0)
                _maxBarLength = _staminaBarTransform.rect.width;

            float staminaPercentage = currentStamina / maxStamina;
            float newLength = _maxBarLength * staminaPercentage;

            _staminaBarTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newLength);
            _staminaBarImage.color = _staminaBarGradient.Evaluate(staminaPercentage);
        }
    }
}
