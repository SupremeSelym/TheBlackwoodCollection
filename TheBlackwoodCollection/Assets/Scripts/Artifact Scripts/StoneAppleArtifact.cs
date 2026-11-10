using UnityEngine;

public class StoneAppleArtifact : ArtifactBase
{
    [SerializeField] float GravityRadius = 5f;

    protected override void OnBreach()
    {
        Debug.Log("Artifact has breached");
    }

    protected override float ProcessSoothe(CharacterController player, int ActionId)
    {
        Debug.Log("Applying soothe");
        return 1f;
    }

    protected override void OnBreachedTick(float dt)
    {
        RequestSootheRpc(1);  // Auto-soothes when breached

        // Early implementation of anti-grav effect
        //foreach (var hit in Physics.OverlapSphere(transform.position, GravityRadius))
        //    if (hit.TryGetComponent<RigidBody>(out var rb))
        //    {
        //        rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
        //    }
    }
}
