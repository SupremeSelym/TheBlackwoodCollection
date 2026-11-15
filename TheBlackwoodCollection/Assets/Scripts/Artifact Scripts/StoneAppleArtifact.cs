using System;
using UnityEngine;

public class StoneAppleArtifact : ArtifactBase
{
    [SerializeField][Min(0)] float GravityRadius = 5f;
    [SerializeField][Min(0)] float BaseGravityRadius = 5f;
    [SerializeField][Min(0)] float MaxGravityRadius = 20f;
    [SerializeField][Min(0)] float RadiusIncreaseRate = 1f;
    [SerializeField] float GravityMultiplier = -2f;
    private double sin_value = 0.0;

    protected override void OnBreached()
    {
        Debug.Log("Artifact has breached");
        GravityRadius = BaseGravityRadius;
    }

    protected override void OnBreachedTick(float dt)
    {
        if (GravityRadius < MaxGravityRadius)
            GravityRadius = Math.Clamp(GravityRadius + (dt * RadiusIncreaseRate), BaseGravityRadius, MaxGravityRadius);
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
            sin_value += Time.deltaTime;
            if (sin_value > 2 * Math.PI)
                sin_value -= 2 * Math.PI;

            // Vector of the force to be applied to each object in range;
            // Uses a sin value to have smoothly changing forces from update to update.
            // The non-GravityMultiplier and Physics.gravity section is a multiplier that ranges from 0.5f to 1f
            Vector3 force_from_apple = (0.5f + ((1f + (float)Math.Sin(sin_value)) / 4f)) * GravityMultiplier * Physics.gravity;
            // Early implementation of anti-grav effect
            foreach (var hit in Physics.OverlapSphere(transform.position, GravityRadius))
                if (hit.TryGetComponent<Rigidbody>(out var rb))
                {
                    // dist_mult scales the final force applied to the distance from the artifact;
                    // ranges from 2/3 to 1, where farther objects get a lower value.
                    float dist_mult = 0;
                    if (GravityRadius != 0)
                        dist_mult = ((GravityRadius - Vector3.Distance(transform.position,  rb.position)) / GravityRadius);
                    dist_mult = (2f/3f) + ((1f / 3f) * dist_mult);
                    rb.AddForce(dist_mult * force_from_apple, ForceMode.Acceleration);
                }   // Final force: range(2/3, 1) * range(0.5, 1) * GravityMultiplier * gravity)
        }
    }
}
