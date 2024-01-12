using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryController _inventoryController;
    private ItemGrid _itemGrid;

    private void Awake()
    {
        _itemGrid = GetComponent<ItemGrid>();
    }

    public void SetInventoryController(InventoryController controller)
    {
        _inventoryController = controller;
    }

    #region Event System
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_inventoryController != null)
        {
            _inventoryController.SelectedItemGrid = _itemGrid;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(_inventoryController != null)
        {
            _inventoryController.SelectedItemGrid = null;
        }
    }
    #endregion
}
