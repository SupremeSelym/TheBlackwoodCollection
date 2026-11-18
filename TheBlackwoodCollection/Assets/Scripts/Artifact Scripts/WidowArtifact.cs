using System.Collections;
using UnityEngine;

public class WidowArtifact : ArtifactBase
{
    public enum ThrowMode { GrabNearby, SpawnPrefab, GrabThenSpawn }

    [Header("Mode")]
    public ThrowMode mode = ThrowMode.GrabThenSpawn;   // GrabThenSpawn = grab if something is nearby, otherwise spawn
    public string playerTag = "Player";
    public float detectionRange = 20f;
    public float minThrowDistance = 3f;                // won't throw if the player is closer than this
    public float aimHeight = 1f;                       // aim at chest instead of feet
    public bool requireLineOfSight = true;
    public LayerMask lineOfSightBlockers = ~0;         // set to your environment layers
    public bool predictPlayerMovement = true;

    [Header("Grabbing Nearby Objects")]
    public LayerMask throwableMask;
    public float grabRadius = 8f;
    public float maxObjectMass = 20f;

    [Header("Spawning")]
    public GameObject[] spawnPrefabs;
    public Transform spawnPoint;                       // falls back to hold point

    [Header("Throw Settings")]
    public Transform holdPoint;                        // where the object floats during wind-up
    public float throwSpeed = 18f;                     // used to estimate flight time
    public float windUpTime = 0.8f;
    public float cooldown = 3f;
    public float spinTorque = 5f;
    public float inaccuracy = 0.5f;                    // random offset in meters at the target

    [Header("Events (hook up animations/sounds)")]
    public UnityEngine.Events.UnityEvent onWindUpStart;
    public UnityEngine.Events.UnityEvent onThrow;

    Transform player;
    CharacterController playerController;
    Collider[] myColliders;
    float nextThrowTime;
    bool isThrowing;

    protected override void OnBreached()
    {
        Debug.Log("Artifact has breached");

        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p)
        {
            player = p.transform;
            playerController = p.GetComponent<CharacterController>();
        }
        myColliders = GetComponentsInChildren<Collider>();
        if (!holdPoint) holdPoint = transform;
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
            throwUpdate();

        }
    }

    void throwUpdate()
    {
        if (!player || isThrowing || Time.time < nextThrowTime) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange || dist < minThrowDistance) return;
        if (requireLineOfSight && !HasLineOfSight()) return;

        StartCoroutine(ThrowRoutine());
    }
    bool HasLineOfSight()
    {
        Vector3 from = holdPoint.position;
        Vector3 to = player.position + Vector3.up * aimHeight;
        if (Physics.Linecast(from, to, out RaycastHit hit, lineOfSightBlockers, QueryTriggerInteraction.Ignore))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return true;
    }

    IEnumerator ThrowRoutine()
    {
        isThrowing = true;

        Rigidbody obj = AcquireObject();
        if (obj == null) { isThrowing = false; nextThrowTime = Time.time + 1f; yield break; }

        onWindUpStart?.Invoke();

        // Hold the object (no physics) while winding up
        obj.isKinematic = true;
        obj.interpolation = RigidbodyInterpolation.None;
        Vector3 startPos = obj.position;
        float t = 0f;

        while (t < windUpTime)
        {
            if (obj == null) { isThrowing = false; yield break; }   // object got destroyed
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / windUpTime);
            obj.position = Vector3.Lerp(startPos, holdPoint.position, k);
            FacePlayer();
            yield return null;
        }

        if (obj == null) { isThrowing = false; yield break; }

        // Release and launch
        Vector3 velocity = CalculateLaunchVelocity(obj.position);
        obj.isKinematic = false;
        obj.linearVelocity = velocity;   // Unity 6. On older versions use obj.velocity
        obj.angularVelocity = Random.insideUnitSphere * spinTorque;

        PrepareProjectile(obj.gameObject);
        onThrow?.Invoke();

        nextThrowTime = Time.time + cooldown;
        isThrowing = false;
    }

    // ---------- Getting an object ----------

    Rigidbody AcquireObject()
    {
        if (mode != ThrowMode.SpawnPrefab)
        {
            Rigidbody grabbed = FindNearbyObject();
            if (grabbed) return grabbed;
            if (mode == ThrowMode.GrabNearby) return null;
        }
        return SpawnObject();
    }

    Rigidbody FindNearbyObject()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRadius, throwableMask, QueryTriggerInteraction.Ignore);
        Rigidbody best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            Rigidbody rb = h.attachedRigidbody;
            if (!rb || rb.isKinematic || rb.mass > maxObjectMass) continue;
            if (rb.transform.IsChildOf(transform)) continue;                 // don't grab yourself
            if (rb.GetComponent<ThrownObject>() is ThrownObject t && t.IsArmed) continue; // already flying

            float d = (rb.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = rb; }
        }
        return best;
    }

    Rigidbody SpawnObject()
    {
        if (spawnPrefabs == null || spawnPrefabs.Length == 0) return null;

        GameObject prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
        Vector3 pos = spawnPoint ? spawnPoint.position : holdPoint.position;
        GameObject go = Instantiate(prefab, pos, Random.rotation);

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (!rb) rb = go.AddComponent<Rigidbody>();
        return rb;
    }

    Vector3 CalculateLaunchVelocity(Vector3 from)
    {
        Vector3 target = player.position + Vector3.up * aimHeight;
        float flightTime = Mathf.Max(0.25f, Vector3.Distance(from, target) / throwSpeed);

        if (predictPlayerMovement)
            target += GetPlayerVelocity() * flightTime;

        target += Random.insideUnitSphere * inaccuracy;

        // Solve for initial velocity so the object lands on 'target' after 'flightTime' under gravity:
        // target = from + v*t + 0.5*g*t^2   =>   v = (target - from - 0.5*g*t^2) / t
        Vector3 g = Physics.gravity;
        return (target - from - 0.5f * g * flightTime * flightTime) / flightTime;
    }

    Vector3 GetPlayerVelocity()
    {
        if (playerController) return playerController.velocity;
        return Vector3.zero;
    }

    void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    // ---------- Projectile setup ----------

    void PrepareProjectile(GameObject go)
    {
        ThrownObject proj = go.GetComponent<ThrownObject>();
        if (!proj) proj = go.AddComponent<ThrownObject>();
        proj.Arm();

        // Briefly ignore the thrower's colliders so the object doesn't smack the enemy on release
        foreach (var c in go.GetComponentsInChildren<Collider>())
            foreach (var mine in myColliders)
                Physics.IgnoreCollision(c, mine, true);

        StartCoroutine(ReenableCollision(go, 0.4f));
    }

    IEnumerator ReenableCollision(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!go) yield break;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            foreach (var mine in myColliders)
                if (c && mine) Physics.IgnoreCollision(c, mine, false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, grabRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

}