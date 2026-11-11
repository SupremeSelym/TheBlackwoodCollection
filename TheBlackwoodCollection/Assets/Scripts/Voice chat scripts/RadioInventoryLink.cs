using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;


//On player prefab, next to inventory stuff
//While the local player has a radio item in their inventory, they can hear and use the radio
//If they drop the radio they are taken off the channel

[RequireComponent(typeof(Inventory))]

public class RadioInventoryLink : NetworkBehaviour
{

    [Tooltip("The ItemData asset that represents the radio.")]
    [SerializeField] private ItemData radioItem;

    [Tooltip("Everyone with the same frequency can talk to each other.")]
    [SerializeField] private int frequency = 1;

    [Tooltip("OFF: the radio works while its anywhere in the inventory. " +
        "ON: only while it's the selected slot.")]
    [SerializeField] private bool onlyWhenSelected = false;

    private Inventory inventory;
    private bool wantRadio;
    private bool busy;
    private float nextAttemptTime;

    public override void OnNetworkSpawn()
    {
        //Only player you control matters. Everyone else's radio is handled by their machine

        if (!IsOwner)
        {

            enabled = false;
            return;
        }

        inventory = GetComponent<Inventory>();
        inventory.OnChanged += UpdateWantRadio;
        inventory.OnSelectionChanged += UpdateWantRadio;
        UpdateWantRadio();
    }

    public override void OnNetworkDespawn()
    {
        if(inventory != null)
        {
            inventory.OnChanged -= UpdateWantRadio;
            inventory.OnSelectionChanged -= UpdateWantRadio;
        }

        if(IsOwner && Voicechatmanager.Instance != null &&
            Voicechatmanager.Instance.HasRadio)
        {
            _ = Voicechatmanager.Instance.UnequipRadioAsync();
        }
    }


    private void Update()
    {
        var voice = Voicechatmanager.Instance;

        if (voice == null || !voice.InSession || busy) return;
        if (Time.time < nextAttemptTime) return;


        //keep radio channel in step with what inventory says
        //handles picking up radio before voice finishes connecting

        if(wantRadio && !voice.HasRadio)
        {
            _ = Reconcile(true);
        }
        else if(!wantRadio && voice.HasRadio)
        {
            _ = Reconcile(false);
        }

    }


    private async Task Reconcile(bool equip)
    {

        busy = true;

        try
        {

            if (equip)
            {
                await Voicechatmanager.Instance.EquipRadioAsync(frequency);
            }
            else
            {
                await Voicechatmanager.Instance.UnequipRadioAsync();
            }
        }
        finally
        {
            busy = false;

            //if something fails don't hammer retries
            nextAttemptTime = Time.time + 1f;
        }
    }

    private void UpdateWantRadio()
    {
        wantRadio = radioItem != null && InventoryHasRadio();
    }


    private bool InventoryHasRadio()
    {
        int count = Mathf.Min(inventory.slotCount, inventory.slots.Length);

        if (onlyWhenSelected)
        {
            int selected = inventory.selectedSlot;
           
            if (selected < 0 || selected >= count) return false;

            return SlotIsRadio(inventory.slots[selected]);
        }

        for (int i = 0; i < count; i++)
        {
            if (SlotIsRadio(inventory.slots[i])) return true;
        }

        return false;
    }


    private bool SlotIsRadio(InventorySlot slot)
    {

        return !slot.IsEmpty && slot.itemId == radioItem.id;
    }
    
}
