using KBCore.Refs;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemGrid : ValidatedMonoBehaviour
{
    public const float TILE_SIZE_WIDTH = 32;
    public const float TILE_SIZE_HEIGHT = 32;

    [SerializeField] private int _gridSizeWidth = 10;
    [SerializeField] private int _gridSizeHeight = 10;

    [SerializeField, Parent] private Canvas _rootCanvas;
    [SerializeField, Self] private Image _gridImage;

    private InventoryItem[,] _inventoryItemSlot;

    [SerializeField] private GameObject _inventoryContents;
    private RectTransform _rectTransform;

    private Vector2 _positionOnGrid = new();
    private Vector2Int _tileGridPosition  = new();

    [SerializeField] private GameObject _itemPrefab;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Init(_gridSizeWidth, _gridSizeHeight);
    }

    private void Start()
    {
        _gridImage.enabled = false;
    }

    private void Init(int width, int height)
    {
        //initialize the size of the item grid
        _inventoryItemSlot = new InventoryItem[width, height];
        Vector2 size = new(width * TILE_SIZE_WIDTH, height * TILE_SIZE_HEIGHT);
        _rectTransform.sizeDelta = size;
    }
    
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        //get tile on grid at mouse position
        _positionOnGrid.x = mousePosition.x - _rectTransform.position.x;
        _positionOnGrid.y = _rectTransform.position.y - mousePosition.y;

        _tileGridPosition.x = (int)(_positionOnGrid.x / (TILE_SIZE_WIDTH * _rootCanvas.scaleFactor));
        _tileGridPosition.y = (int)(_positionOnGrid.y / (TILE_SIZE_HEIGHT * _rootCanvas.scaleFactor));

        return _tileGridPosition;
    }

    public InventoryItem PickUpItem(int x, int y)
    {
        InventoryItem pickedUpItem = _inventoryItemSlot[x, y];

        if (pickedUpItem == null)
        {
            return null;
        }

        CleanGridReference(pickedUpItem);

        return pickedUpItem;
    }

    public bool PlaceItem(InventoryItem item, int posX, int posY, ref InventoryItem overlapItem)
    {
        //check if desired position is valid in grid
        if (BoundryCheck(posX, posY, item.Width, item.Height) == false)
        {
            return false;
        }

        //check if item is overlapping another item
        if (OverlapCheck(posX, posY, item.Width, item.Height, ref overlapItem) == false)
        {
            overlapItem = null;
            return false;
        }

        //clean up the grid from overlap item
        if (overlapItem != null)
        {
            CleanGridReference(overlapItem);
        }

        PlaceItem(item, posX, posY);

        return true;
    }

    public void PlaceItem(InventoryItem item, int posX, int posY)
    {
        //get item rect
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(_inventoryContents.transform);

        //add the item slots to grid
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                _inventoryItemSlot[posX + x, posY + y] = item;
            }
        }

        item.onGridPositionX = posX;
        item.onGridPositionY = posY;
        Vector2 position = CalculatePositionOnGrid(item, posX, posY);

        //set item position
        itemRect.localPosition = position;
    }

    public Vector2 CalculatePositionOnGrid(InventoryItem item, int posX, int posY)
    {
        //get desired position with offset
        return new()
        {
            x = (posX * TILE_SIZE_WIDTH + TILE_SIZE_WIDTH * item.Width / 2),
            y = -(posY * TILE_SIZE_HEIGHT + TILE_SIZE_HEIGHT * item.Height / 2)
        };
    }

    private void CleanGridReference(InventoryItem Item)
    {
        for (int x = 0; x < Item.Width; x++)
        {
            for (int y = 0; y < Item.Height; y++)
            {
                _inventoryItemSlot[Item.onGridPositionX + x, Item.onGridPositionY + y] = null;
            }
        }
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        for (int x = 0; x < width;x++)
        {
            for(int y = 0; y < height ; y++)
            {
                if (_inventoryItemSlot[posX + x, posY + y] != null)
                {
                    //assign new overlap item
                    if(overlapItem == null)
                    {
                        overlapItem = _inventoryItemSlot[posX + x, posY + y];
                    }

                    //items have overlapped
                    else
                    {
                        if(overlapItem != _inventoryItemSlot[posX + x, posY + y])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_inventoryItemSlot[posX + x, posY + y] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0)
        {
            return false;
        }

        if(posX >= _gridSizeWidth || posY >= _gridSizeHeight) 
        { 
            return false;
        }

        return true;
    }

    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if(PositionCheck(posX, posY) == false)
        {
            return false;
        }

        if(PositionCheck(posX + width - 1, posY + height - 1) == false)
        {
            return false;
        }

        return true;
    }

    internal InventoryItem GetItem(int x, int y)
    {
        return _inventoryItemSlot[x,y];
    }

    public Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        int height = _gridSizeHeight - itemToInsert.Height + 1;
        int width = _gridSizeWidth - itemToInsert.Width + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if(CheckAvailableSpace(x, y, itemToInsert.Width, itemToInsert.Height) == true)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return null;
    }

    public void ToggleGrid(bool isVisible)
    {
        _gridImage.enabled = isVisible;
        _inventoryContents.SetActive(isVisible);
    }
}
