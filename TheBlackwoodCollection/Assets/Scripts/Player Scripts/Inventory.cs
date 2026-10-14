using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct InventorySlot
{
    public int itemId;   // -1 means empty
    public int quantity;

    public bool IsEmpty => itemId < 0 || quantity <= 0;
    public static InventorySlot Empty => new InventorySlot { itemId = -1, quantity = 0 };
}

public class Inventory : NetworkBehaviour
{
    public ItemDatabase database;
    public int slotCount = 20;
    public InventorySlot[] slots;

    // Selection is local to the owning player (the server doesn't track it)
    public int selectedSlot = 0;

    public event Action OnChanged;
    public event Action OnSelectionChanged;

    private void Awake()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = InventorySlot.Empty;
    }

    // ---------- SELECTION (local) ----------

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        if (index == selectedSlot) return;

        selectedSlot = index;
        OnSelectionChanged?.Invoke();
    }

    public InventorySlot GetSelectedSlot()
    {
        if (selectedSlot < 0 || selectedSlot >= slots.Length) return InventorySlot.Empty;
        return slots[selectedSlot];
    }

    // ---------- SERVER-ONLY CHANGES ----------

    /// Returns how many items did NOT fit (0 = everything was added).
    public int AddItem(int itemId, int amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Inventory.AddItem can only run on the server.");
            return amount;
        }

        ItemData data = database.GetItem(itemId);
        if (data == null || amount <= 0) return amount;

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (slots[i].IsEmpty || slots[i].itemId != itemId) continue;
            if (slots[i].quantity >= data.maxStack) continue;

            int space = data.maxStack - slots[i].quantity;
            int toAdd = Mathf.Min(space, amount);
            slots[i].quantity += toAdd;
            amount -= toAdd;
        }

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;

            int toAdd = Mathf.Min(data.maxStack, amount);
            slots[i] = new InventorySlot { itemId = itemId, quantity = toAdd };
            amount -= toAdd;
        }

        NotifyChanged();
        return amount;
    }

    public void RemoveFromSlot(int index, int amount)
    {
        if (!IsServer) return;
        if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return;

        slots[index].quantity -= amount;
        if (slots[index].quantity <= 0)
            slots[index] = InventorySlot.Empty;

        NotifyChanged();
    }

    public void SwapSlots(int a, int b)
    {
        if (!IsServer) return;
        if (a < 0 || b < 0 || a >= slots.Length || b >= slots.Length) return;

        (slots[a], slots[b]) = (slots[b], slots[a]);
        NotifyChanged();
    }

    // ---------- READ-ONLY HELPERS ----------

    public int CountItem(int itemId)
    {
        int total = 0;
        foreach (var s in slots)
            if (!s.IsEmpty && s.itemId == itemId) total += s.quantity;
        return total;
    }

    public int FindFirstFilledSlot()
    {
        for (int i = 0; i < slots.Length; i++)
            if (!slots[i].IsEmpty) return i;
        return -1;
    }

    // ---------- SYNC TO THE OWNING PLAYER ----------

    private void NotifyChanged()
    {
        OnChanged?.Invoke();

        // The host's own player already has the real data
        if (!IsServer || IsOwner) return;

        int[] ids = new int[slots.Length];
        int[] qtys = new int[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            ids[i] = slots[i].itemId;
            qtys[i] = slots[i].quantity;
        }

        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        SyncClientRpc(ids, qtys, target);
    }

    [ClientRpc]
    private void SyncClientRpc(int[] ids, int[] qtys, ClientRpcParams rpcParams = default)
    {
        if (IsServer) return;

        int count = Mathf.Min(slots.Length, ids.Length);
        for (int i = 0; i < count; i++)
            slots[i] = new InventorySlot { itemId = ids[i], quantity = qtys[i] };

        OnChanged?.Invoke();
    }
}