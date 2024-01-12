using UnityEngine;

public class InventoryHighlight : MonoBehaviour
{
    [SerializeField] private RectTransform _highlighter;

    private void Awake()
    {
        _highlighter = GameObject.Find("/HUD/Inventory/Highlighter").GetComponent<RectTransform>();

        Show(false);
    }

    public void Show(bool value)
    {
        _highlighter.gameObject.SetActive(value);
    }

    public void SetSize(InventoryItem item)
    {
        Vector2 size = new(
            item.Width * ItemGrid.TILE_SIZE_WIDTH,
            item.Height * ItemGrid.TILE_SIZE_HEIGHT
            );
        _highlighter.sizeDelta = size;
    }

    public void SetParent(ItemGrid targetGrid)
    {
        if (targetGrid == null)
        {
            return;
        }

        _highlighter.SetParent(targetGrid.GetComponent<RectTransform>());
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem item)
    {
        Vector2 position = targetGrid.CalculatePositionOnGrid(item, item.onGridPositionX, item.onGridPositionY);

        _highlighter.localPosition = position;
    } 

    public void SetPosition(ItemGrid targetGrid, InventoryItem item, int posX, int posY)
    {
        Vector2 position = targetGrid.CalculatePositionOnGrid(item, posX, posY);

        _highlighter.localPosition = position;
    }
}
