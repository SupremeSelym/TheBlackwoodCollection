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
    private Generator generator;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private Camera playerCamera;


    private static readonly Key[] numberKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        playerCamera = GetComponentInChildren<Camera>();
        generator = GetComponent<Generator>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (JournalUI.IsOpen) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        HandleSelection(kb);

        if (kb[pickupKey].wasPressedThisFrame) TryPickup();
        if (kb[dropKey].wasPressedThisFrame) DropServerRpc(inventory.selectedSlot);
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {

            if (generator != null)
                generator.RequestRefuel();
        }

    }

    private void HandleSelection(Keyboard kb)
    {
        for (int i = 0; i < numberKeys.Length; i++)
            if (kb[numberKeys[i]].wasPressedThisFrame)
                inventory.SelectSlot(i);

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.1f) CycleSelection(-1);
            else if (scroll < -0.1f) CycleSelection(1);
        }
    }

    private void CycleSelection(int direction)
    {
        int count = inventory.slots.Length;
        inventory.SelectSlot((inventory.selectedSlot + direction + count) % count);
    }

    // ---------- PICKUP ----------

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

    [ServerRpc]
    private void PickupServerRpc(NetworkObjectReference pickupRef)
    {
        if (!pickupRef.TryGet(out NetworkObject netObj)) return;

        ItemPickup pickup = netObj.GetComponent<ItemPickup>();
        if (pickup == null) return;

        float dist = Vector3.Distance(transform.position, pickup.transform.position);
        if (dist > pickupRange + 2f) return;

        pickup.TryPickUp(inventory);
    }

    // ---------- DROP ----------

    [ServerRpc]
    private void DropServerRpc(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.slots.Length) return;

        InventorySlot slot = inventory.slots[slotIndex];
        if (slot.IsEmpty) return;

        ItemData data = inventory.database.GetItem(slot.itemId);
        if (data == null) return;

        ItemPickup prefabToSpawn = data.worldPrefab != null ? data.worldPrefab : pickupPrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogError($"No world prefab for {data.name} and no default Pickup Prefab assigned.", this);
            return;
        }

        int amount = dropWholeStack ? slot.quantity : Mathf.Min(dropAmount, slot.quantity);

        inventory.RemoveFromSlot(slotIndex, amount);

        Vector3 spawnPos = transform.position
                         + transform.forward * dropDistance
                         + Vector3.up * dropHeightOffset;

        ItemPickup dropped = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        dropped.item = data;
        dropped.quantity = amount;

        dropped.GetComponent<NetworkObject>().Spawn();
    }

   /* private Generator FindLookedAtGenerator()
    {
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            interactionLayers))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<Generator>();
    }*/

}