using UnityEngine;

public class StoneAppleArtifact : ArtifactBase
{
    [SerializeField] float GravityRadius = 5f;
    [SerializeField] float GravityMultiplier = -2f;

    protected override void OnBreach()
    {
        Debug.Log("Artifact has breached");
    }

    protected override float ProcessSoothe(CharacterController player, int ActionId)
    {
        Debug.Log("Applying soothe");
        return 1f;
    }

    void FixedUpdate()
    {
        if (State.Value == ArtifactState.Breached)
        {
            // Early implementation of anti-grav effect
            foreach (var hit in Physics.OverlapSphere(transform.position, GravityRadius))
                if (hit.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce(GravityMultiplier * Physics.gravity, ForceMode.Acceleration);
                }
        }
        
    }

}
