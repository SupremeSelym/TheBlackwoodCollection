using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Inventory))]
public class PlayerInteract : NetworkBehaviour
{
    [Header("Keys")]
    public Key pickupKey = Key.E;
    public Key dropKey = Key.Q;

    [Header("Pickup")]
    public float pickupRange = 2f;

    [Header("Drop")]
    [Tooltip("Fallback used when an item has no World Prefab of its own.")]
    public ItemPickup pickupPrefab;
    public float dropDistance = 1.5f;
    public float dropHeightOffset = 0.5f;
    public int dropAmount = 1;
    public bool dropWholeStack = false;

    private Inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        // Only the player who owns this character reads input.
        if (!IsOwner) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb[pickupKey].wasPressedThisFrame) TryPickup();
        if (kb[dropKey].wasPressedThisFrame) DropServerRpc();
    }

    // ---------- PICKUP ----------

    // Runs on the player's own machine: find the nearest pickup, then ask the server.
    private void TryPickup()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, pickupRange, ~0, QueryTriggerInteraction.Collide);

        ItemPickup closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            ItemPickup pickup = hit.GetComponentInParent<ItemPickup>();
            if (pickup == null) continue;

            float dist = (pickup.transform.position - transform.position).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = pickup;
            }
        }

        if (closest == null) return;

        NetworkObject netObj = closest.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsSpawned)
        {
            Debug.LogWarning("That pickup isn't a spawned NetworkObject, so it can't be synced.", closest);
            return;
        }

        PickupServerRpc(netObj);
    }

    // Runs on the server.
    [ServerRpc]
    private void PickupServerRpc(NetworkObjectReference pickupRef)
    {
        // If another player grabbed it first, this fails and we just stop
        if (!pickupRef.TryGet(out NetworkObject netObj)) return;

        ItemPickup pickup = netObj.GetComponent<ItemPickup>();
        if (pickup == null) return;

        // Anti-cheat / lag tolerance: make sure the player is actually near it
        float dist = Vector3.Distance(transform.position, pickup.transform.position);
        if (dist > pickupRange + 2f) return;

        pickup.TryPickUp(inventory);
    }

    // ---------- DROP ----------

    // Runs on the server.
    [ServerRpc]
    private void DropServerRpc()
    {
        int index = inventory.FindFirstFilledSlot();
        if (index < 0) return; // nothing to drop

        InventorySlot slot = inventory.slots[index];
        ItemData data = inventory.database.GetItem(slot.itemId);
        if (data == null) return;

        ItemPickup prefabToSpawn = data.worldPrefab != null ? data.worldPrefab : pickupPrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogError($"No world prefab for {data.name} and no default Pickup Prefab assigned.", this);
            return;
        }

        int amount = dropWholeStack ? slot.quantity : Mathf.Min(dropAmount, slot.quantity);

        inventory.RemoveFromSlot(index, amount);

        Vector3 spawnPos = transform.position
                         + transform.forward * dropDistance
                         + Vector3.up * dropHeightOffset;

        ItemPickup dropped = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        dropped.item = data;
        dropped.quantity = amount;

        // This is what makes it exist for every player
        dropped.GetComponent<NetworkObject>().Spawn();
    }
}