using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    public GameObject panel;
    public Transform slotContainer;
    public SlotUI slotPrefab;
    public Key toggleKey = Key.Tab;

    private Inventory inventory;
    private readonly List<SlotUI> slotUIs = new List<SlotUI>();

    private void Start()
    {
        panel.SetActive(true);
    }

    private void Update()
    {
        if (inventory == null) TryFindLocalPlayer();

        Keyboard kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            panel.SetActive(!panel.activeSelf);
    }

    private void TryFindLocalPlayer()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || nm.LocalClient == null) return;

        NetworkObject playerObject = nm.LocalClient.PlayerObject;
        if (playerObject == null) return;

        Inventory inv = playerObject.GetComponent<Inventory>();
        if (inv != null) Bind(inv);
    }

    public void Bind(Inventory newInventory)
    {
        if (inventory != null)
        {
            inventory.OnChanged -= Refresh;
            inventory.OnSelectionChanged -= RefreshSelection;
        }

        inventory = newInventory;
        inventory.OnChanged += Refresh;
        inventory.OnSelectionChanged += RefreshSelection;

        foreach (Transform child in slotContainer) Destroy(child.gameObject);
        slotUIs.Clear();

        for (int i = 0; i < inventory.slotCount; i++)
            slotUIs.Add(Instantiate(slotPrefab, slotContainer));

        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= Refresh;
            inventory.OnSelectionChanged -= RefreshSelection;
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            InventorySlot s = inventory.slots[i];
            ItemData data = s.IsEmpty ? null : inventory.database.GetItem(s.itemId);
            slotUIs[i].Set(data);
        }
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].SetSelected(i == inventory.selectedSlot);
    }
}