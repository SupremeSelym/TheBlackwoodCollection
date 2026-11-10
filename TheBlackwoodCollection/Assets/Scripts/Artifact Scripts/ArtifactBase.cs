using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public enum ArtifactState { Contained, Agitated, Critical, Breached }

public struct ContainmentModifier
{
    public object Source;           // Who applied (so it can be removed/replaced)
    public float DecayMultiplier;   // 1.5 = drains 50% faster
    public float ExpireTime;        // Time.time to expire; <= 0 means permanent
}

// Set to an abstract class to force it to be inherited and overridden rather than directly instantiated
public abstract class ArtifactBase : NetworkBehaviour
{
    [SerializeField] protected ArtifactData data;

    public NetworkVariable<float> Containment = new(100f);
    public NetworkVariable<ArtifactState> State = new(ArtifactState.Contained);

    public ArtifactData Data => data;
    public float Normalized => Containment.Value / data.MaxContainment;

    // Happens on server AND clients (state is a NetworkVariable)
    public event Action<ArtifactBase, ArtifactState, ArtifactState> StateChanged;

    readonly List<ContainmentModifier> modifiers = new();

    // Lifecycle
    public override void OnNetworkSpawn()
    {
        ArtifactManager.Register(this);
        State.OnValueChanged += HandleStateChanged;

        if (IsServer)
        {
            Containment.Value = data.MaxContainment;
            OnServerSpawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= HandleStateChanged;
        ArtifactManager.Unregister(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        ServerTick(Time.deltaTime);
    }

    void ServerTick(float dt)
    {
        PruneModifiers();

        if (State.Value == ArtifactState.Breached)
        {
            OnBreachedTick(dt);
            return;
        }

        Containment.Value = Mathf.Clamp(Containment.Value - GetDecayRate() * dt, 0f, data.MaxContainment);

        OnServerTick(dt);
        UpdateState();
    }

    // ------- Decay and modifiers -------
    // Override to add artifact-specific conditions
    protected virtual float GetDecayRate()
    {
        float rate = data.BaseDecayPerSecond;
        foreach (var m in modifiers) rate *= m.DecayMultiplier;
        return rate;
    }

    public void AddModifier(object source, float multiplier, float duration = 0f)
    {
        if (!IsServer) return;
        modifiers.RemoveAll(m => m.Source == source); // one per source
        float dur = 0f;
        if (duration > 0f)
            dur = Time.time + duration;
        modifiers.Add(new ContainmentModifier
        {
            Source = source,
            DecayMultiplier = multiplier,
            ExpireTime = dur
        });
    }

    public void RemoveModifier(object source) =>
        modifiers.RemoveAll(m => m.Source == source);

    void PruneModifiers() =>
        modifiers.RemoveAll(m => m.ExpireTime > 0f && Time.time >= m.ExpireTime);

    // ------- Soothing -------
    // Called by player interaction code; actionId lets one artifact have multiple soothe actions
    [Rpc(SendTo.Server)]
    public void RequestSootheRpc(int actionId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client)) return;

        var player = client.PlayerObject.GetComponent<CharacterController>();
        if (player == null || !IsInRange(player)) return;

        float amount = ProcessSoothe(player, actionId); // Subclass decides
        if (amount <= 0f) return;

        if (State.Value == ArtifactState.Breached)
        {
            Recontain();
            return;
        }

        Containment.Value = Mathf.Min(Containment.Value + amount, data.MaxContainment);
        UpdateState();
    }

    protected virtual bool IsInRange(CharacterController player) =>
        Vector3.Distance(player.transform.position, transform.position) <= data.InteractRange;

    /// Return how much containment this action restores (0 = invalid/failed).
    protected abstract float ProcessSoothe(CharacterController player, int actionId);

    // ------- State and Breach -------
    void UpdateState()
    {
        if (State.Value == ArtifactState.Breached) return;

        float n = Normalized;
        ArtifactState next =
            n <= 0f ? ArtifactState.Breached :
            n <= data.CriticalThreshold ? ArtifactState.Critical :
            n <= data.AgitatedThreshold ? ArtifactState.Agitated :
                                          ArtifactState.Contained;

        if (next == State.Value) return;

        State.Value = next;
        if (next == ArtifactState.Breached) Breach();
    }

    void Breach()
    {
        OnBreach();
        ArtifactManager.NotifyBreached(this);
    }

    public void Recontain()
    {
        if (!IsServer || State.Value != ArtifactState.Breached) return;
        Containment.Value = data.RecontainAmount;
        State.Value = ArtifactState.Agitated;
        OnRestoreContainment();
    }

    void HandleStateChanged(ArtifactState prev, ArtifactState next)
    {
        StateChanged?.Invoke(this, prev, next);
        OnStateChangedLocal(prev, next); // VFX/audio on every client
    }


    // ------- Virtual methods to be overridden by subclasses -------
    protected virtual void OnServerSpawn() { }              // What to do when the server (host) spawns
    protected virtual void OnServerTick(float dt) { }       // Updates to artifacts based on server ticks
    protected virtual void OnBreach() { }                   // Immediate effect when artifact breaches containment
    protected virtual void OnBreachedTick(float dt) { }     // Ongoing effects while the artifact is uncontained
    protected virtual void OnRestoreContainment() { }       // Effect when containment of artifact is restored
    // Method for individual clients to have VFX, SFX, or other per client effects trigger when an artifact's state changes
    protected virtual void OnStateChangedLocal(ArtifactState prev, ArtifactState next) { }  
}
