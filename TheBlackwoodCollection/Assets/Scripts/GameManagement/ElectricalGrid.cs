using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ElectricalGrid : NetworkBehaviour
{
    [SerializeField] private Generator[] generators;

    public NetworkVariable<bool> HasPower = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Update()
    {
        if (!IsServer)
            return;

        bool anyGeneratorProducing = false;

        foreach (Generator generator in generators)
        {
            if (generator != null &&
                generator.IsSpawned &&
                generator.IsProducing.Value)
            {
                anyGeneratorProducing = true;
                break;
            }
        }

        HasPower.Value = anyGeneratorProducing;
    }
}

