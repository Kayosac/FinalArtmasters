using UnityEngine;

public class WaterTeleport : MonoBehaviour
{
    [Header("Teleport Destination")]
    [Tooltip("Shore point (an empty Transform placed on land)")]
    public Transform shorePoint;

    [Tooltip("Vertical offset above the ground at spawn")]
    public float heightOffset = 0.5f;

    [Header("Who To Teleport")]
    [Tooltip("If assigned, this exact Transform will be teleported (player root)")]
    public Transform playerOverride;

    [Tooltip("If playerOverride is null, try to match by tag")]
    public string playerTag = "Player";

    [Header("Extra Options")]
    [Tooltip("Reset player's Rigidbody velocity on teleport")]
    public bool resetRigidbodyVelocity = true;

    [Tooltip("Temporarily disable CharacterController while teleporting")]
    public bool disableCharacterControllerWhileTeleport = true;

    [Tooltip("Small cooldown to avoid immediate re-entry, seconds")]
    public float reentryGraceTime = 0.3f;

    [Tooltip("Also handle OnTriggerStay (useful if player can spawn in water)")]
    public bool alsoTeleportOnStay = false;

    private float lastTeleportTime = -999f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true; // water should be a trigger
    }

    private void OnTriggerEnter(Collider other) => TryHandle(other.transform);
    private void OnCollisionEnter(Collision c) => TryHandle(c.transform);
    private void OnTriggerStay(Collider other) { if (alsoTeleportOnStay) TryHandle(other.transform); }

    private void TryHandle(Transform entered)
    {
        if (Time.unscaledTime - lastTeleportTime < reentryGraceTime) return;
        if (shorePoint == null) { Debug.LogWarning("[WaterTeleport] ShorePoint is not assigned."); return; }

        Transform target = GetTargetTransform(entered);
        if (target == null) return;

        Teleport(target);
    }

    private Transform GetTargetTransform(Transform entered)
    {
        if (playerOverride) return playerOverride;

        if (!string.IsNullOrEmpty(playerTag) && entered.CompareTag(playerTag))
            return entered.root;

        // Try to find a likely player root by common components up the hierarchy
        var t = entered;
        while (t != null)
        {
            if (t.GetComponent<CharacterController>() || t.GetComponent<Rigidbody>())
                return t;
            t = t.parent;
        }
        return null;
    }

    private void Teleport(Transform target)
    {
        Vector3 dest = shorePoint.position + Vector3.up * heightOffset;

        var cc = target.GetComponent<CharacterController>();
        if (cc && disableCharacterControllerWhileTeleport)
        {
            bool wasEnabled = cc.enabled;
            cc.enabled = false;
            target.position = dest;
            if (wasEnabled) cc.enabled = true;
        }
        else
        {
            target.position = dest;
        }

        if (resetRigidbodyVelocity)
        {
            var rb = target.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }

        lastTeleportTime = Time.unscaledTime;
    }
}
