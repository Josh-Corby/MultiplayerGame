using UnityEngine;
using UnityEngine.UI;

public class InventoryHighlight : MonoBehaviour
{
    private RectTransform _highlighter;
    private Image _highlighterImage;

    private void Start()
    {
        //InitHighlight();
    }

    private void InitHighlight()
    {
        _highlighter = GameObject.Find("Highlighter").GetComponent<RectTransform>();
        _highlighterImage = _highlighter.GetComponent<Image>();
        Show(false);
    }

    public void Show(bool value)
    {
        _highlighter.gameObject.SetActive(value);
        _highlighterImage.enabled = value;
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
