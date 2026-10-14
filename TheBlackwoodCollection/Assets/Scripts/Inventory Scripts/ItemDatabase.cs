using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public ItemData[] items;

    private Dictionary<int, ItemData> lookup;

    public ItemData GetItem(int id)
    {
        if (lookup == null) BuildLookup();
        lookup.TryGetValue(id, out ItemData item);
        return item;
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<int, ItemData>();
        foreach (var item in items)
        {
            if (item == null) continue;
            if (lookup.ContainsKey(item.id))
                Debug.LogError($"Duplicate item id {item.id} on {item.name}");
            else
                lookup[item.id] = item;
        }
    }

    private void OnValidate() { lookup = null; }
}