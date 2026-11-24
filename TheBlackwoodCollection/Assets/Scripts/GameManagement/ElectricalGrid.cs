using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ElectricalGrid : NetworkBehaviour
{
    [SerializeField] private Generator[] generators;

    [Tooltip("How many generators must be producing for this zone to have power. " +
             "1 = any one is enough (redundant), set to generators.Length to require all.")]
    [SerializeField, Min(1)] private int minimumActiveGenerators = 1;

    public NetworkVariable<bool> HasPower = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// Fires on server and clients whenever this zone's power changes.
    public event Action<bool> PowerChanged;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (Generator generator in generators)
            {
                if (generator == null)
                {
                    Debug.LogWarning($"{name}: null generator reference.", this);
                    continue;
                }
                generator.IsProducing.OnValueChanged += OnGeneratorChanged;
            }
            RefreshPower();
        }

        HasPower.OnValueChanged += OnPowerValueChanged;
        PowerChanged?.Invoke(HasPower.Value); // initial state, covers late joiners
    }

    public override void OnNetworkDespawn()
    {
        HasPower.OnValueChanged -= OnPowerValueChanged;

        if (IsServer)
        {
            foreach (Generator generator in generators)
            {
                if (generator != null)
                    generator.IsProducing.OnValueChanged -= OnGeneratorChanged;
            }
        }
    }

    private void OnGeneratorChanged(bool previous, bool current) => RefreshPower();

    private void RefreshPower()
    {
        int active = 0;
        foreach (Generator generator in generators)
        {
            if (generator != null && generator.IsSpawned && generator.IsProducing.Value)
                active++;
        }

        int required = Mathf.Clamp(minimumActiveGenerators, 1, Mathf.Max(1, generators.Length));
        HasPower.Value = active >= required;
    }

    private void OnPowerValueChanged(bool previous, bool current) => PowerChanged?.Invoke(current);
}

