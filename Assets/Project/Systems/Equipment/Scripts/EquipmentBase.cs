using System;
using UnityEngine;

namespace Project
{
    public abstract class EquipmentBase : ScriptableObject
    {
        public string DisplayName;
        public Sprite Icon;
        public bool HasCharge;
        public bool AddToInventory;
        protected CharacterMotor _linkedMotor;

        public bool IsActive { get; protected set; } = false;

        public abstract bool ToggleUse();

        public abstract bool Tick();

        public abstract float GetChargesRemaining();

        public virtual void OnPickedUp() { }

        public virtual void LinkTo(EquipmentManager equipmentManager)
        {
            _linkedMotor = equipmentManager.GetComponent<CharacterMotor>();
        }
    }
}
