using UnityEngine;

namespace Project
{
    public class Pickup_Equipment : Pickup, IInteractable
    {
        [SerializeField] private EquipmentBase _equipment;

        public override void Interact(Interactor interactor)
        {
            if (interactor.TryGetComponent<EquipmentManager>(out var equipmentManager))
            {
                equipmentManager.AddEquipment(_equipment);
                gameObject.SetActive(false);
            }
        }
    }
}
