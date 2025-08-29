using UnityEngine;

public class TriggerPlayAnimation : MonoBehaviour
{
    [Header("Target Animator")]
    [Tooltip("Animator to play the animation on. If null, will try GetComponentInParent<Animator>().")]
    public Animator targetAnimator;

    [Header("Play Mode")]
    [Tooltip("If true, uses SetTrigger(triggerName). If false, CrossFade to stateName.")]
    public bool useTrigger = true;
    public string triggerName = "Use";
    public string stateName = "";   // used if useTrigger == false
    public int stateLayer = 0;
    public float crossFadeDuration = 0.1f;

    [Header("Filter")]
    [Tooltip("If not empty, only objects with this tag will trigger.")]
    public string requiredTag = "";
    [Tooltip("Allowed layers to trigger this. Default = Everything.")]
    public LayerMask allowedLayers = ~0;

    [Header("Rules")]
    [Tooltip("Fire only once.")]
    public bool oneShot = false;
    [Tooltip("Cooldown between triggers, seconds.")]
    public float cooldown = 0f;

    private bool consumed = false;
    private float lastTime = -999f;

    private void Reset()
    {
        // Ensure this collider is a trigger
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        if (!targetAnimator)
            targetAnimator = GetComponentInParent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAllowed(other)) return;
        TryPlay();
    }

    private bool IsAllowed(Collider other)
    {
        if (consumed) return false;

        if (cooldown > 0f && (Time.time - lastTime) < cooldown)
            return false;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return false;

        if (((1 << other.gameObject.layer) & allowedLayers.value) == 0)
            return false;

        return true;
    }

    private void TryPlay()
    {
        lastTime = Time.time;

        if (targetAnimator == null)
        {
            Debug.LogWarning("[TriggerPlayAnimation] No Animator assigned or found.");
            return;
        }

        if (useTrigger)
        {
            if (!string.IsNullOrEmpty(triggerName))
                targetAnimator.SetTrigger(triggerName);
            else
                Debug.LogWarning("[TriggerPlayAnimation] useTrigger is true but triggerName is empty.");
        }
        else
        {
            if (!string.IsNullOrEmpty(stateName))
                targetAnimator.CrossFade(stateName, crossFadeDuration, stateLayer);
            else
                Debug.LogWarning("[TriggerPlayAnimation] useTrigger is false but stateName is empty.");
        }

        if (oneShot) consumed = true;
    }
}
