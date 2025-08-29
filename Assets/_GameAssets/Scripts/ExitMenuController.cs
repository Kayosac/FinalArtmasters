using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class ExitMenuController : MonoBehaviour
{
    [Header("UI диалога")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Поведение")]
    public bool pauseTimeWhileOpen = true;
    public Behaviour[] behavioursToDisable;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System (опц.)")]
    public PlayerInput playerInput;
    public bool useActionMapsSwitch = true;
    public string gameplayActionMap = "Player"; // твоя геймплейная карта
    public string uiActionMap = "UI";           // карта для UI (если есть)
#endif

    public UnityEvent onDialogOpened;
    public UnityEvent onDialogClosed;

    private bool isOpen;
    private float prevTimeScale = 1f;
    private bool prevCursorVisible;
    private CursorLockMode prevCursorLock;
#if ENABLE_INPUT_SYSTEM
    private string prevActionMapName;
#endif

    void Awake()
    {
        if (dialogPanel) dialogPanel.SetActive(false);
        if (yesButton) { yesButton.onClick.RemoveAllListeners(); yesButton.onClick.AddListener(OnConfirmExit); }
        if (noButton) { noButton.onClick.RemoveAllListeners(); noButton.onClick.AddListener(CloseDialog); }
    }

    void Update()
    {
        if (EscPressed())
        {
            if (isOpen) CloseDialog();
            else OpenDialog();
        }
    }

    private bool EscPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void OpenDialog()
    {
        if (isOpen) return;
        isOpen = true;

        if (pauseTimeWhileOpen) { prevTimeScale = Time.timeScale; Time.timeScale = 0f; }

        SetControlsLocked(true);

        prevCursorVisible = Cursor.visible;
        prevCursorLock = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

#if ENABLE_INPUT_SYSTEM
        prevActionMapName = playerInput && playerInput.currentActionMap != null
            ? playerInput.currentActionMap.name : null;

        if (playerInput && useActionMapsSwitch)
            SwitchActionMapSafe(playerInput, uiActionMap); // НЕ бросит исключение, если карты нет
#endif

        if (dialogPanel) dialogPanel.SetActive(true);
        if (noButton) EventSystem.current?.SetSelectedGameObject(noButton.gameObject);
        else if (yesButton) EventSystem.current?.SetSelectedGameObject(yesButton.gameObject);

        onDialogOpened?.Invoke();

        if (EventSystem.current == null)
            Debug.LogError("Нет EventSystem в сцене — UI не получит ввод.");
#if ENABLE_INPUT_SYSTEM
        else
            Debug.Log($"UI Module: {EventSystem.current.currentInputModule?.GetType().Name}");
#endif
    }

    public void CloseDialog()
    {
        if (!isOpen) return;
        isOpen = false;

        if (pauseTimeWhileOpen) Time.timeScale = prevTimeScale;

        SetControlsLocked(false);

        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevCursorLock;

#if ENABLE_INPUT_SYSTEM
        if (playerInput && useActionMapsSwitch)
        {
            // вернём то, что было, либо gameplayActionMap
            if (!string.IsNullOrEmpty(prevActionMapName))
                SwitchActionMapSafe(playerInput, prevActionMapName);
            else
                SwitchActionMapSafe(playerInput, gameplayActionMap);
        }
#endif

        if (dialogPanel) dialogPanel.SetActive(false);
        onDialogClosed?.Invoke();
    }

    private void SetControlsLocked(bool locked)
    {
        if (behavioursToDisable != null)
            foreach (var b in behavioursToDisable) if (b) b.enabled = !locked;
    }

    private void OnConfirmExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void SwitchActionMapSafe(PlayerInput pi, string mapName)
    {
        if (pi == null || pi.actions == null || string.IsNullOrEmpty(mapName)) return;

        var map = pi.actions.FindActionMap(mapName, throwIfNotFound: false);
        if (map != null)
        {
            pi.SwitchCurrentActionMap(mapName);
        }
        else
        {
            // не падаем, просто предупреждаем 1 раз
            Debug.LogWarning($"[ExitMenuController] Карта действий '{mapName}' не найдена в '{pi.actions.name}'. Переключение пропущено.");
        }
    }
#endif
}
