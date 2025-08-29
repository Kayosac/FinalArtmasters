using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Keyboard.current
#endif

#if TMP_PRESENT
using TMPro;
#endif

public class LookInteractorController : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private LayerMask interactableMask = ~0;

    [Header("UI Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private string promptTextValue = "Press E";

#if TMP_PRESENT
    [SerializeField] private TMP_Text promptTMP;
#else
    [SerializeField] private UnityEngine.UI.Text promptText;
#endif

    private Camera cam;
    private Interactable current;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;

#if TMP_PRESENT
        if (promptTMP) promptTMP.text = promptTextValue;
#else
        if (promptText) promptText.text = promptTextValue;
#endif
        SetPromptVisible(false);
    }

    private void Update()
    {
        UpdateFocus();
        if (current != null && InteractPressed())
        {
            current.Interact();
        }
    }

    private void UpdateFocus()
    {
        Interactable hitInteractable = null;

        if (cam)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, maxDistance, interactableMask, QueryTriggerInteraction.Ignore))
            {
                hitInteractable = hit.collider.GetComponentInParent<Interactable>();
            }
        }

        if (hitInteractable != current)
        {
            current = hitInteractable;
            SetPromptVisible(current != null);
        }
    }

    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
        return Input.GetKeyDown(KeyCode.E);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot && promptRoot.activeSelf != visible)
            promptRoot.SetActive(visible);
    }
}
