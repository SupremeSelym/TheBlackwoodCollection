using Unity.Netcode;
using UnityEngine;


public class Playervoicelink : NetworkBehaviour
{

    [Tooltip("The player's head or camera. Used for voice position and facing direction. Defaults to this object.")]
    [SerializeField] private Transform head;

    private Transform Head => head != null ? head : transform;

    public override void OnNetworkSpawn()
    {
        //Only the player you control matters
        //Other players' voices are placed by their own clients

        if (!IsOwner) return;

        if (Voicechatmanager.Instance != null)
            Voicechatmanager.Instance.SetLocalPlayer(Head);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (Voicechatmanager.Instance != null)
            Voicechatmanager.Instance.ClearLocalPlayer(Head);
    }
}
