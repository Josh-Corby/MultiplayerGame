using KBCore.Refs;
using UnityEngine;

namespace Project
{
    public class LightSwitch : ValidatedMonoBehaviour, IInteractable
    {
        [SerializeField, Parent] private SceneLight _linkedLight;
        public string InteractionPrompt => throw new System.NotImplementedException();

        public bool Interact(Interactor interactor)
        {
            Debug.Log("Interact");
            _linkedLight.ToggleLight();
            return true;
        }
    }
}
