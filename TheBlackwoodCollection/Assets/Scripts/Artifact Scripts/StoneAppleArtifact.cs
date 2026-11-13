using UnityEngine;

public class StoneAppleArtifact : ArtifactBase
{
    [SerializeField] float GravityRadius = 5f;
    [SerializeField] float GravityMultiplier = -2f;

    protected override void OnBreached()
    {
        Debug.Log("Artifact has breached");
    }

    protected override float ProcessSoothe(CharacterController player, int ActionId)
    {
        if (ActionId == 1)
        {
            return 20f;
        }
        else if (ActionId == 2)
        {
            return 10f;
        }
        else return 0;
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
