using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Transform _interactionStart;
    [SerializeField] private float _interactionDistance;
    [SerializeField] private LayerMask _interactableMask;

    private void Update()
    {
        Debug.DrawRay(_interactionStart.position, _interactionStart.transform.forward, Color.red, _interactionDistance);
        if (Physics.Raycast(_interactionStart.position, _interactionStart.transform.forward, out RaycastHit hit, _interactionDistance, _interactableMask))
        {
            var interactable = hit.collider.gameObject.GetComponent<IInteractable>();

            if(interactable != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactable.Interact(this);
            }
        }
    }
}
