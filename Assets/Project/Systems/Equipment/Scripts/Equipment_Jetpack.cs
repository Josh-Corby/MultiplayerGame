using UnityEngine;

namespace Project
{
    [CreateAssetMenu(menuName = "Equipment/Jetpack", fileName = "Jetpack")]
    public class Equipment_Jetpack : EquipmentBase
    {
        [SerializeField] private float _fuel  = 30f;      

        private float _durationUsed = 0f;

        public override bool ToggleUse()
        {
            if(_durationUsed >= _fuel)
            {
                if(IsActive)
                {
                    IsActive = false;
                    OnStopFlying();
                }

                return true;
            }

           IsActive = !IsActive;

            if (IsActive)
                OnStartFlying();
            else
                OnStopFlying();

            return false;
        }

        public override bool Tick()
        {
            if (IsActive)
            {
                _durationUsed += Time.deltaTime;

                // used maximum duration
                if (_durationUsed >= _fuel)
                {
                    OnStopFlying();
                    return true;
                }
            }

            return false;
        }

        protected void OnStartFlying()
        {
            _linkedMotor.SwitchMovementMode<MovementMode_Flying>();
        }

        protected void OnStopFlying()
        {
            _linkedMotor.SwitchMovementMode<MovementMode_Ground>();
        }

        public override float GetChargesRemaining()
        {
            return 1f - (_durationUsed / _fuel);
        }
    }
}
