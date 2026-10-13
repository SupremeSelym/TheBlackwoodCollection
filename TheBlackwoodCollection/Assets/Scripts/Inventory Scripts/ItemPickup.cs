using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int quantity = 1;

    /// Called by PlayerInteract when the player presses the pickup key.
    /// Returns true if at least some items were picked up.
    public bool TryPickUp(Inventory inv)
    {
        if (item == null)
        {
            Debug.LogError("ItemPickup has no Item assigned", this);
            return false;
        }

        int leftover = inv.AddItem(item.id, quantity);
        bool pickedSomething = leftover < quantity;

        if (leftover <= 0)
            Destroy(gameObject);
        else
            quantity = leftover; // inventory was full, remainder stays in the world

        return pickedSomething;
    }
}