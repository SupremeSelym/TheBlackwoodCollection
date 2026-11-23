using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HeadlessMaiden : ArtifactBase
{
    [Header("Headless Maiden - Movement")]
    [Tooltip("Movement speed in units/sec when not being watched.")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Tooltip("Extra speed multiplier when in Critical or Breached states.")]
    [SerializeField] private float agitatedOrBreachedSpeedMultiplier = 1.35f;

    [Tooltip("How close the Maiden gets to a player before stopping.")]
    [SerializeField] private float stoppingDistance = 1.75f;

    [Tooltip("If true, moves even while Contained; if false, starts slow and speeds up as containment drops.")]
    [SerializeField] private bool activeFromSpawn = true;

    [Header("Headless Maiden - Gaze Detection")]
    [Tooltip("Half-angle in degrees of a player's view cone (e.g., 55 = ~110 deg total FOV).")]
    [SerializeField] private float viewHalfAngleDegrees = 55f;

    [Tooltip("Maximum distance a player can spot the Maiden from.")]
    [SerializeField] private float maxSightDistance = 30f;

    [Tooltip("Layers that block a player's line of sight (e.g., walls/obstacles). Leave as Default/Nothing if not using layers.")]
    [SerializeField] private LayerMask obstructionMask = 0;

    [Tooltip("Height offset for eye/target checks so raycasts don't clip the floor.")]
    [SerializeField] private float eyeHeightOffset = 1.5f;

    private readonly List<CharacterController> playerCache = new();

    protected override void OnServerSpawn()
    {
        // Containment starts at Data.MaxContainment in ArtifactBase.OnNetworkSpawn().
        // Add any spawn-time server setup here if needed.
    }

    protected override void OnServerTick(float dt)
    {
        if (!activeFromSpawn && State.Value == ArtifactState.Contained)
            return;

        // If ANY connected player is currently looking at the Maiden, freeze movement.
        if (IsAnyPlayerLookingAtMe())
            return;

        CharacterController targetPlayer = FindNearestPlayer();
        if (targetPlayer == null)
            return;

        MoveTowardPlayer(targetPlayer, dt);
    }

    private void MoveTowardPlayer(CharacterController targetPlayer, float dt)
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = targetPlayer.transform.position;

        // Keep movement planar on XZ so she doesn't float or sink into the floor
        Vector3 flatTarget = new Vector3(targetPos.x, currentPos.y, targetPos.z);
        Vector3 toTarget = flatTarget - currentPos;
        float distance = toTarget.magnitude;

        if (distance <= stoppingDistance)
            return;

        Vector3 direction = toTarget / distance;
        float speed = moveSpeed;

        if (State.Value == ArtifactState.Critical || State.Value == ArtifactState.Breached)
        {
            speed *= agitatedOrBreachedSpeedMultiplier;
        }

        // Move via CharacterController if attached, otherwise transform
        if (TryGetComponent<CharacterController>(out var selfController))
        {
            selfController.Move(direction * (speed * dt));
        }
        else
        {
            transform.position += direction * (speed * dt);
        }

        // Face the player while moving (when not being watched)
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 12f * dt);
        }
    }

    private bool IsAnyPlayerLookingAtMe()
    {
        RefreshPlayerCache();

        Vector3 maidenTargetPoint = transform.position + Vector3.up * (transform.localScale.y * 0.5f);
        float cosThreshold = Mathf.Cos(viewHalfAngleDegrees * Mathf.Deg2Rad);

        for (int i = 0; i < playerCache.Count; i++)
        {
            CharacterController player = playerCache[i];
            if (player == null || !player.enabled || !player.gameObject.activeInHierarchy)
                continue;

            Vector3 playerEyePos = player.transform.position + Vector3.up * eyeHeightOffset;
            Vector3 toMaiden = maidenTargetPoint - playerEyePos;
            float dist = toMaiden.magnitude;

            if (dist > maxSightDistance || dist <= 0.001f)
                continue;

            Vector3 dirToMaiden = toMaiden / dist;
            Vector3 lookForward = player.transform.forward;
            lookForward.y = 0f;
            if (lookForward.sqrMagnitude < 0.0001f)
                lookForward = player.transform.forward;
            else
                lookForward.Normalize();

            // Check if Maiden is inside player's forward view cone
            float dot = Vector3.Dot(lookForward, dirToMaiden);
            if (dot < cosThreshold)
                continue;

            // Optional wall/obstacle check: if obstructionMask is set, block vision behind walls
            if (obstructionMask.value != 0)
            {
                if (Physics.Linecast(playerEyePos, maidenTargetPoint, obstructionMask, QueryTriggerInteraction.Ignore))
                    continue;
            }

            return true;
        }

        return false;
    }

    private CharacterController FindNearestPlayer()
    {
        RefreshPlayerCache();

        CharacterController nearest = null;
        float bestDistSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        for (int i = 0; i < playerCache.Count; i++)
        {
            CharacterController player = playerCache[i];
            if (player == null || !player.enabled || !player.gameObject.activeInHierarchy)
                continue;

            float distSqr = (player.transform.position - myPos).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                nearest = player;
            }
        }

        return nearest;
    }

    private void RefreshPlayerCache()
    {
        playerCache.Clear();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                NetworkClient client = kvp.Value;
                if (client?.PlayerObject != null)
                {
                    var cc = client.PlayerObject.GetComponent<CharacterController>();
                    if (cc != null)
                        playerCache.Add(cc);
                }
            }
        }
        else
        {
            // Fallback for offline/single-scene testing
            var found = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
            playerCache.AddRange(found);
        }
    }

    // Required override from ArtifactBase: determines containment restored per actionId.
    protected override float ProcessSoothe(CharacterController player, int actionId)
    {
        if (data == null)
            return 0f;

        // Placeholder action mapping; customize actionIds (e.g., repair kit vs. ward kit)
        return actionId switch
        {
            1 => data.RecontainAmount,
            _ => data.RecontainAmount * 0.5f
        };
    }

    protected override void OnStateChangedLocal(ArtifactState prev, ArtifactState next)
    {
        // Client-side VFX/SFX hooks go here when containment state changes across the network.
    }
}