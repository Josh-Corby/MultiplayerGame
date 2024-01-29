using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Project
{
    public class InteractableButton : MonoBehaviour, IInteractable
    {
        public string InteractionPrompt => throw new System.NotImplementedException();
        public event UnityAction OnInteracted = delegate { };

        public void Interact(Interactor interactor)
        {
            OnInteracted?.Invoke();
        }
    }
}
