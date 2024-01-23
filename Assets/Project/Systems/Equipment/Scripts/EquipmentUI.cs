using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class EquipmentUI : MonoBehaviour
    {
        [SerializeField] private Image _equipmentImage;
        [SerializeField] private TextMeshProUGUI _equipmentName;
        [SerializeField] private Color _inUseColour = Color.green;
        [SerializeField] private Color _notInUseColour = Color.white;
        [SerializeField] private Slider _equipmentTimeRemainingSlider;

        public void SetEquipment(EquipmentBase equipment)
        {
            if(equipment == null)
            {
                _equipmentImage.sprite = null;
                _equipmentName.text = "";

                SetEquipmentTimeRemaining(false, 0);
                SetEquipmentInUse(false);
                return;
            }

            _equipmentImage.sprite = equipment.Icon;
            _equipmentName.text = equipment.DisplayName;

            SetEquipmentInUse(equipment.IsActive);
            SetEquipmentTimeRemaining(equipment.HasCharge, equipment.GetChargesRemaining());
        }

        public void SetEquipmentInUse(bool inUse)
        {
            _equipmentImage.color = inUse ? _inUseColour : _notInUseColour;
        }

        public void SetEquipmentTimeRemaining(bool show, float timeRemaining)
        {
            if(show)
            {
                _equipmentTimeRemainingSlider.gameObject.SetActive(true);
                _equipmentTimeRemainingSlider.SetValueWithoutNotify(timeRemaining);
            }
            else
                _equipmentTimeRemainingSlider.gameObject.SetActive(false);
        }
    }
}
