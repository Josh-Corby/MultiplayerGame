using UnityEngine;

namespace Project
{
    [CreateAssetMenu(menuName = "Equipment/Instant Health", fileName = "Instant Health")]
    public class Equipment_InstantHealth : EquipmentBase
    {
        [SerializeField] private float _healAmount = 20f;
        public override float GetChargesRemaining()
        {
            return 0;
        }

        public override void OnPickedUp()
        {
            _linkedMotor.OnPerformHeal(_linkedMotor.gameObject, _healAmount);
        }

        public override bool Tick()
        {
            return true;
        }

        public override bool ToggleUse()
        {
            return true;
        }
    }
}
