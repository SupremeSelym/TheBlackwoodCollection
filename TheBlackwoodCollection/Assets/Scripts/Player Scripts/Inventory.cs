using System;
using UnityEngine;

[Serializable]
public struct InventorySlot
{
    public int itemId;   // -1 means empty
    public int quantity;

    public bool IsEmpty => itemId < 0 || quantity <= 0;
    public static InventorySlot Empty => new InventorySlot { itemId = -1, quantity = 0 };
}

public class Inventory : MonoBehaviour
{
    public ItemDatabase database;
    public int slotCount = 20;
    public InventorySlot[] slots;

    // UI (and later, networking) listens to this
    public event Action OnChanged;

    private void Awake()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = InventorySlot.Empty;
    }

    /// Returns how many items did NOT fit (0 = everything was added).
    public int AddItem(int itemId, int amount)
    {
        ItemData data = database.GetItem(itemId);
        if (data == null || amount <= 0) return amount;

        // 1) Fill existing stacks first
        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (slots[i].IsEmpty || slots[i].itemId != itemId) continue;
            if (slots[i].quantity >= data.maxStack) continue;

            int space = data.maxStack - slots[i].quantity;
            int toAdd = Mathf.Min(space, amount);
            slots[i].quantity += toAdd;
            amount -= toAdd;
        }

        // 2) Then use empty slots
        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;

            int toAdd = Mathf.Min(data.maxStack, amount);
            slots[i] = new InventorySlot { itemId = itemId, quantity = toAdd };
            amount -= toAdd;
        }

        OnChanged?.Invoke();
        return amount;
    }

    public void RemoveFromSlot(int index, int amount)
    {
        if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return;

        slots[index].quantity -= amount;
        if (slots[index].quantity <= 0)
            slots[index] = InventorySlot.Empty;

        OnChanged?.Invoke();
    }

    public void SwapSlots(int a, int b)
    {
        if (a < 0 || b < 0 || a >= slots.Length || b >= slots.Length) return;
        (slots[a], slots[b]) = (slots[b], slots[a]);
        OnChanged?.Invoke();
    }

    public int CountItem(int itemId)
    {
        int total = 0;
        foreach (var s in slots)
            if (!s.IsEmpty && s.itemId == itemId) total += s.quantity;
        return total;
    }

    /// Returns the index of the first slot that has something in it, or -1 if the inventory is empty.
    public int FindFirstFilledSlot()
    {
        for (int i = 0; i < slots.Length; i++)
            if (!slots[i].IsEmpty) return i;
        return -1;
    }
}