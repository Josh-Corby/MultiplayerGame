using KBCore.Refs;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class InventoryController : ValidatedMonoBehaviour
{
    [SerializeField] private Transform _ItemDropTransform;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private List<ItemDataSO> _items;

    private ItemGrid _selectedItemGrid;
    private ItemGrid _playerInventoryGrid;
    private InventoryItem _selectedItem;
    private InventoryItem _overlapItem;
    private RectTransform _rectTransform;

    private Transform _canvasTransform;

    [SerializeField, Self] private InventoryHighlight _highlight;
    private InventoryItem _itemToHighlight;
    private Vector2Int _oldPosition;

    public bool InventoryIsOpen { get; private set; } = false;
    public ItemGrid SelectedItemGrid
    {
        get => _selectedItemGrid;
        set
        {
            _selectedItemGrid = value;
            _highlight.SetParent(SelectedItemGrid);
        }
    }

    public event Action<bool> OnInventoryToggled = null;

    private void Awake()
    {
        _playerInventoryGrid = GameObject.Find("PlayerInventory").GetComponent<ItemGrid>();

        GridInteract playerInteractGrid = _playerInventoryGrid.GetComponent<GridInteract>();
        playerInteractGrid.SetInventoryController(this);

        _canvasTransform = _playerInventoryGrid.transform.parent.GetComponent<RectTransform>();

        ToggleInventory(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }

        if (InventoryIsOpen)
        {
            ItemIconDrag();

            if (_selectedItemGrid == null)
            {
                _highlight.Show(false);
                return;
            }

            HandleHighlight();
        }
    }

    private void ToggleInventory()
    {
        InventoryIsOpen = !InventoryIsOpen;
        _playerInventoryGrid.ToggleGrid(InventoryIsOpen);
        OnInventoryToggled?.Invoke(InventoryIsOpen);
    }

    private void ToggleInventory(bool enabled)
    {
        InventoryIsOpen = enabled;
        _playerInventoryGrid.ToggleGrid(InventoryIsOpen);

        OnInventoryToggled?.Invoke(InventoryIsOpen);
    }

    private void RotateItem()
    {
        if (_selectedItem == null)
        {
            return;
        }

        _selectedItem.Rotate();
        HandleHighlight();
    }

    private InventoryItem CreateItemPrefab()
    {
        InventoryItem item = Instantiate(_itemPrefab).GetComponent<InventoryItem>();
        _rectTransform = item.GetComponent<RectTransform>();
        _rectTransform.SetParent(_canvasTransform);
        _rectTransform.SetAsLastSibling();

        return item;
    }

    private void CreateRandomItem()
    {
        InventoryItem item = CreateItemPrefab();
        _selectedItem = item;
        int selectedItemID = Random.Range(0, _items.Count);
        item.Set(_items[selectedItemID]);
    }
  
    private void InsertRandomItem()
    {
        if (_selectedItemGrid == null)
        {
            return;
        }

        CreateRandomItem();
        InventoryItem itemToInsert = _selectedItem;
        _selectedItem = null;
        if(InsertItem(itemToInsert) == false)
        {
            Destroy(itemToInsert.gameObject);
        }
    }

    public bool AddToInventory(ItemDataSO itemData)
    {
        InventoryItem item = CreateItemPrefab();
        item.Set(itemData);
        return InsertItemToInventory(item);
    }

    public bool InsertItemToInventory(InventoryItem itemToInsert)
    {
        Vector2Int? positionOnGrid = _playerInventoryGrid.FindSpaceForObject(itemToInsert);

        if (positionOnGrid == null)
        {
            return false;
        }

        _playerInventoryGrid.PlaceItem(itemToInsert, positionOnGrid.Value.x, positionOnGrid.Value.y);
        return true;
    }

    public bool InsertItem(InventoryItem itemToInsert)
    {
        Vector2Int? positionOnGrid = _selectedItemGrid.FindSpaceForObject(itemToInsert);

        if (positionOnGrid == null)
        {
            return false;
        }

        SelectedItemGrid.PlaceItem(itemToInsert, positionOnGrid.Value.x, positionOnGrid.Value.y);
        return true;
    }

    private void HandleHighlight()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();

        if(_oldPosition == positionOnGrid)
        {
            return;
        }

        _oldPosition = positionOnGrid;

        if (_selectedItem == null)
        {
            _itemToHighlight = _selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);

            if(_itemToHighlight != null)
            {
                //highlight hovered item
                _highlight.Show(true);
                _highlight.SetSize(_itemToHighlight);
                _highlight.SetPosition(_selectedItemGrid, _itemToHighlight);
            }

            else
            {
                _highlight.Show(false);
            }
        }

        else
        {
            //only show hightlight when moving items to valid move positions
            _highlight.Show(_selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y, _selectedItem.Width, _selectedItem.Height));
            _highlight.SetSize(_selectedItem);
            _highlight.SetPosition(_selectedItemGrid, _selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }

    private void ItemIconDrag()
    {
        if (_selectedItem != null)
        {
            _rectTransform.position = Input.mousePosition;
        }
    }

    private void PickUpItem(Vector2Int tileGridPosition)
    {
        //select new item
        _selectedItem = _selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);

        //store rect transform of selected item
        if (_selectedItem != null)
        {
            _rectTransform = _selectedItem.GetComponent<RectTransform>();
        }
    }

    private void PlaceItem(Vector2Int tileGridPosition)
    {
        if (_selectedItemGrid.PlaceItem(_selectedItem, tileGridPosition.x, tileGridPosition.y, ref _overlapItem))
        {
            _selectedItem = null;
            if(_overlapItem != null)
            {
                _selectedItem = _overlapItem;
                _overlapItem = null;
                _rectTransform = _selectedItem.RectTransform;
                _rectTransform.SetAsLastSibling();
            }
        }     
    }

    private Vector2Int GetTileGridPosition()
    {
        //offset item position
        Vector2 mousePosition = Input.mousePosition;
        if (_selectedItem != null)
        {
            mousePosition.x -= (_selectedItem.Width - 1) * ItemGrid.TILE_SIZE_WIDTH / 2;
            mousePosition.y += (_selectedItem.Height - 1) * ItemGrid.TILE_SIZE_HEIGHT / 2;
        }

        return _selectedItemGrid.GetTileGridPosition(mousePosition);
    }

    private void DropItem()
    {
        GameObject DroppedItem = Instantiate(_selectedItem.ItemPrefab, _ItemDropTransform.position, Quaternion.identity);

        Destroy(_selectedItem.gameObject);
    }

    #region Input System

    public void OnUseItem()
    {
        if (!InventoryIsOpen)
        {
            return;
        }

        if (_selectedItemGrid == null)
        {
            if(_selectedItem != null)
            {
                DropItem();
                return;
            }
        }

        Vector2Int tileGridPosition = GetTileGridPosition();

        if (_selectedItem == null)
        {
            PickUpItem(tileGridPosition);
        }

        else
        {
            PlaceItem(tileGridPosition);
        }
    }

    #endregion
}
