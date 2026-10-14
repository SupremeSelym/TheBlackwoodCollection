using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]

public class ItemData : ScriptableObject
{
    public int id;
    public string itemName;
    [TextArea] public string description;
    public ItemPickup worldPrefab;
    public Sprite icon;
    public int maxStack = 1;
}