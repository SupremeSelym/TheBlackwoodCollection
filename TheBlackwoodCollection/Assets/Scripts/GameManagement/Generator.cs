using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Generator : NetworkBehaviour
{
    [Header("Generator Settings")]
    [SerializeField] private float secondsPerFuelUnit = 30f;
    [SerializeField] private int startingStoredFuel = 0;
    [SerializeField] private float startingCondition = 100f;
    [SerializeField] private float conditionLossPerSecond = 0.1f;
    [SerializeField] private float repairAmount = 40f;
    [SerializeField] private float interactionRange = 3f;

    [Header("Interaction Point")]
    [SerializeField] private Transform interactionPoint;

    public NetworkVariable<int> StoredFuel = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> Condition = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsProducing = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float fuelTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        StoredFuel.Value = Mathf.Max(0, startingStoredFuel);
        Condition.Value = Mathf.Clamp(
            startingCondition, 0f, 100f
        );

        fuelTimer = Mathf.Max(0.1f, secondsPerFuelUnit);

        UpdateProductionState();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (Condition.Value > 0f && StoredFuel.Value > 0)
        {
            fuelTimer -= Time.deltaTime;

            Condition.Value = Mathf.Max(
                0f,
                Condition.Value -
                conditionLossPerSecond * Time.deltaTime
            );

            if (fuelTimer <= 0f)
            {
                StoredFuel.Value--;
                fuelTimer = Mathf.Max(
                    0.1f, secondsPerFuelUnit
                );
            }
        }

        UpdateProductionState();
    }

    private void UpdateProductionState()
    {
        IsProducing.Value =
            StoredFuel.Value > 0 &&
            Condition.Value > 0f;
    }

    public void RequestRefuel()
    {
        RefuelServerRpc();
    }

    public void RequestMaintenance()
    {
        MaintainServerRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RefuelServerRpc(
        RpcParams rpcParams = default)
    {
        if (!IsServer || Condition.Value <= 0f)
            return;

        if (!IsPlayerInRange(
            rpcParams.Receive.SenderClientId))
            return;

        /*if (!TryGetPlayerInventory(
            rpcParams.Receive.SenderClientId,
            out PlayerInventory inventory))
            return;

        if (!inventory.TryUseFuel())
            return;*/

        StoredFuel.Value++;
        UpdateProductionState();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void MaintainServerRpc(
        RpcParams rpcParams = default)
    {
        if (!IsServer || Condition.Value >= 100f)
            return;

        if (!IsPlayerInRange(
            rpcParams.Receive.SenderClientId))
            return;

        /*if (!TryGetPlayerInventory(
            rpcParams.Receive.SenderClientId,
            out PlayerInventory inventory))
            return;

        if (!inventory.TryUseRepairKit())
            return;*/

        Condition.Value = Mathf.Min(
            100f,
            Condition.Value + repairAmount
        );

        UpdateProductionState();
    }

    private bool IsPlayerInRange(ulong clientId)
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(
            clientId, out var client))
            return false;

        if (client.PlayerObject == null)
            return false;

        Vector3 point = interactionPoint != null
            ? interactionPoint.position
            : transform.position;

        Vector3 playerPosition =
            client.PlayerObject.transform.position;

        return Vector3.Distance(playerPosition, point)
            <= interactionRange;
    }

  /*  private bool TryGetPlayerInventory(
        ulong clientId,
        out PlayerInventory inventory)
    {
        inventory = null;

        if (!NetworkManager.ConnectedClients.TryGetValue(
            clientId, out var client))
            return false;

        if (client.PlayerObject == null)
            return false;

        inventory = client.PlayerObject
            .GetComponent<PlayerInventory>();

        return inventory != null && inventory.IsSpawned;
    }*/
}

