using System;
using UnityEngine;

public class WidowArtifact : ArtifactBase
{
    protected override void OnBreached()
    {
        Debug.Log("Artifact has breached");
        
    }

    protected override void OnBreachedTick(float dt)
    {
        
    }

    protected override float ProcessSoothe(CharacterController player, int ActionId)
    {
        if (ActionId == 1)
        {
            return 50f;
        }
        else if (ActionId == 2)
        {
            return 30f;
        }
        else return 0;
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        if (State.Value == ArtifactState.Breached)
        {
        }
    }
}