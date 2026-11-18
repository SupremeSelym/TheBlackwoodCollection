using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public class ThrownObject : MonoBehaviour
{
    public float baseDamage = 10f;
    public float minImpactSpeed = 3f;       // ignore slow bumps
    public float armedLifetime = 6f;        // stops being dangerous after this long
    public bool damageScalesWithMass = true;

    float armedUntil;

    public bool IsArmed => Time.time < armedUntil;

    public void Arm() => armedUntil = Time.time + armedLifetime;

    void OnCollisionEnter(Collision col)
    {
        if (!IsArmed) return;
        if (col.relativeVelocity.magnitude < minImpactSpeed) return;

        var target = col.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            float dmg = baseDamage;
            if (damageScalesWithMass) dmg *= Mathf.Clamp(GetComponent<Rigidbody>().mass, 0.5f, 5f);
            target.TakeDamage(dmg);
            armedUntil = 0f;                // only hit once
        }
        else if (!col.collider.CompareTag("Player"))
        {
            // Hit the world: stop being dangerous shortly after
            armedUntil = Mathf.Min(armedUntil, Time.time + 0.2f);
        }
    }
}
