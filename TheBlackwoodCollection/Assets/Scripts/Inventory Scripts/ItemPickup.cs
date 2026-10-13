using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int quantity = 1;

    /// SERVER ONLY. Called by PlayerInteract after it validates the request.
    /// Returns true if at least some items were picked up.
    public bool TryPickUp(Inventory inv)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return false;

        if (item == null)
        {
            Debug.LogError("ItemPickup has no Item assigned", this);
            return false;
        }

        int leftover = inv.AddItem(item.id, quantity);
        bool pickedSomething = leftover < quantity;

        if (leftover <= 0)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj.IsSpawned) netObj.Despawn(true); // removes it for everyone
            else Destroy(gameObject);
        }
        else
        {
            quantity = leftover; // inventory was full, remainder stays in the world
        }

        return pickedSomething;
    }
}