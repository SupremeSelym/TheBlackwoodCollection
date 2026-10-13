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
        if (kb == null) return; // no keyboard connected

        if (kb[pickupKey].wasPressedThisFrame) TryPickup();
        if (kb[dropKey].wasPressedThisFrame) TryDrop();
    }

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

        if (closest != null)
            closest.TryPickUp(inventory);
    }

    private void TryDrop()
    {
        int index = inventory.FindFirstFilledSlot();
        if (index < 0) return; // nothing to drop

        InventorySlot slot = inventory.slots[index];
        ItemData data = inventory.database.GetItem(slot.itemId);
        if (data == null) return;

        // Use the item's own world prefab, or fall back to the default one
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
    }
}