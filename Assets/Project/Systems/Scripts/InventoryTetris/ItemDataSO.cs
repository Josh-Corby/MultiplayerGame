using UnityEngine;

[CreateAssetMenu]
public class ItemDataSO : ScriptableObject
{
    public int Width = 1;
    public int Height = 1;

    public Sprite ItemIcon;

    public GameObject ItemPrefab;
}
