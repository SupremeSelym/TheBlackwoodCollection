using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public enum ArtifactState { Contained, Agitated, Critical, Breached }

// Set to an abstract class to force it to be inherited and overridden rather than directly instantiated
public abstract class ArtifactBase : NetworkBehaviour
{
    [SerializeField] protected ArtifactData data;

    public NetworkVariable<float> Containment = new(100f);
    public NetworkVariable<ArtifactState> State = new(ArtifactState.Contained);

    public ArtifactData Data => data;

    public override void OnNetworkSpawn()
    {
        ArtifactManager.Register(this);
    }

    public override void OnNetworkDespawn()
    {
        ArtifactManager.Unregister(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ServerTick(float dt)
    {
        
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
