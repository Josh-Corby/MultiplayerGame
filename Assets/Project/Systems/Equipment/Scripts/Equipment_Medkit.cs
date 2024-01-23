using UnityEngine;

namespace Project
{
    [CreateAssetMenu(menuName = "Equipment/Medkit", fileName = "Medkit")]
    public class Equipment_Medkit : EquipmentBase
    {
        [SerializeField] private int _numCharges = 5;
        [SerializeField] private float _healingPerCharge = 20f;

        private int _numChargesRemaining;

        public override void OnPickedUp()
        {
            base.OnPickedUp();

            _numChargesRemaining = _numCharges;
        }

        public override float GetChargesRemaining()
        {
            return (float)_numChargesRemaining / _numCharges;
        }

        public override bool Tick()
        {
            return false;
        }

        public override bool ToggleUse()
        {
            if (_linkedMotor.CurrentHealth >= _linkedMotor.MaxHealth)
                return false;

            _numChargesRemaining--;

            _linkedMotor.OnPerformHeal(_linkedMotor.gameObject, _healingPerCharge);

            return _numChargesRemaining == 0;
        }
    }
}
