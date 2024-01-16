using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    [SerializeField] private ItemDataSO _itemData;
    private RectTransform _rectTransform;

    public int Height
    {
        get
        {
            if(_rotationIndex == 0 || _rotationIndex == 2)
            {
                return _itemData.Height;
            }
            return _itemData.Width;
        }
    }

    public int Width
    {
        get
        {
            if(_rotationIndex == 0 || _rotationIndex == 2)
            {
                return _itemData.Width;
            }
            return _itemData.Height;
        }
    }

    private int _rotationIndex;

    public int onGridPositionX;
    public int onGridPositionY;

    public RectTransform RectTransform { get => _rectTransform; }

    public GameObject ItemPrefab { get => _itemData.ItemPrefab; }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();     
    }

    internal void Set(ItemDataSO itemDataSO, float scaleFactor)
    {
        _itemData = itemDataSO;

        GetComponent<Image>().sprite = itemDataSO.ItemIcon;
        Vector2 size = new()
        {
            x = _itemData.Width * ItemGrid.TILE_SIZE_WIDTH * scaleFactor,
            y = _itemData.Height * ItemGrid.TILE_SIZE_HEIGHT * scaleFactor
        };

        _rectTransform.sizeDelta = size;

        _rotationIndex = Random.Range(0, 4);
        RotateSprite();
    }

    internal void Rotate()
    {
        _rotationIndex += 1;

        if(_rotationIndex > 3)
        {
            _rotationIndex = 0;
        }

        RotateSprite();
    }

    private void RotateSprite()
    {
        _rectTransform.rotation = Quaternion.Euler(0, 0, -90f * _rotationIndex);
    }
}
