using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Target Animation (on another object)")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private string triggerName = "Use";
    [SerializeField] private string stateName = "";
    [SerializeField] private int stateLayer = 0;
    [SerializeField] private float crossFadeDuration = 0.1f;

    [Header("Rules")]
    [SerializeField] private bool oneShot = false;
    [SerializeField] private float cooldown = 0f;

    [Header("Events")]
    public UnityEvent onInteract;

    private bool consumed = false;
    private float lastInteractTime = -999f;

    public void Interact()
    {
        if (oneShot && consumed) return;
        if (Time.time - lastInteractTime < cooldown) return;

        lastInteractTime = Time.time;

        if (targetAnimator)
        {
            if (useTrigger)
            {
                if (!string.IsNullOrEmpty(triggerName))
                    targetAnimator.SetTrigger(triggerName);
            }
            else
            {
                if (!string.IsNullOrEmpty(stateName))
                    targetAnimator.CrossFade(stateName, crossFadeDuration, stateLayer);
            }
        }

        onInteract?.Invoke();
        if (oneShot) consumed = true;
    }
}
