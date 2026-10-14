using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int quantity = 1;

    /// SERVER ONLY. Called after PlayerInteract validates the request.
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
            if (netObj.IsSpawned) netObj.Despawn(true);
            else Destroy(gameObject);
        }
        else
        {
            quantity = leftover;
        }

        return pickedSomething;
    }
}