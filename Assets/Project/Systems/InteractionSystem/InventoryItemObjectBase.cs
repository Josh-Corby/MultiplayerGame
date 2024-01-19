using UnityEngine;

public class InventoryItemObjectBase : MonoBehaviour, IInteractable
{
    [SerializeField] private string _prompt;
    [SerializeField] private ItemDataSO _itemData;
    public string InteractionPrompt => _prompt;

    public void Interact(Interactor interactor)
    {
        if (interactor.TryGetComponent<InventoryController>(out var inventory))
        {
            if (inventory.AddToInventory(_itemData))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
