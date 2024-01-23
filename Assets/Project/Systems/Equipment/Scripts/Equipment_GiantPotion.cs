using UnityEngine;

namespace Project
{
    [CreateAssetMenu(menuName = "Equipment/Potions/Giant", fileName = "Potion_Giant")]
    public partial class Equipment_GiantPotion : EquipmentBase
    {

        [SerializeField] private float _duration = 30f;
        [SerializeField] private float _heightMultiplier = 2f;
        [SerializeField] private float _jumpHeightMultiplier = 1f;

        public override void OnPickedUp()
        {
            base.OnPickedUp();

            _linkedMotor.AddParameterEffector(new HeightEffector(_heightMultiplier, _duration));
            _linkedMotor.AddParameterEffector(new JumpHeightEffector(_jumpHeightMultiplier, _duration));
        }

        public override float GetChargesRemaining()
        {
            return 0f;
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
