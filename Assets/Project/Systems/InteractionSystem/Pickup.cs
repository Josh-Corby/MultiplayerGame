using UnityEngine;

namespace Project
{
    public abstract class Pickup : MonoBehaviour, IInteractable
    {
        [field:SerializeField] public bool PickupOnContact { get; private set; } = false;

        public string InteractionPrompt => throw new System.NotImplementedException();

        public abstract void Interact(Interactor interactor);
    }
}
