using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // для PlayerInput
#endif

#if TMP_PRESENT
using TMPro; // для TMP_Text, если используешь TextMeshPro
#endif

public class StartScreenTimedWithLock : MonoBehaviour
{
    [Header("UI стартового экрана")]
    [Tooltip("Панель/Canvas стартового экрана")]
    [SerializeField] private GameObject startScreenUI;

    [Header("Обратный отсчёт")]
    [Tooltip("Сколько секунд показывать стартовый экран")]
    [Min(0f)] public float showDuration = 3f;

    [Tooltip("Считать время независимо от Time.timeScale")]
    public bool useUnscaledTime = true;

    [Tooltip("UI.Text для вывода оставшихся секунд (необязательно)")]
    [SerializeField] private Text countdownTextUI;

#if TMP_PRESENT
    [Tooltip("TMP_Text для вывода оставшихся секунд (если используешь TextMeshPro)")]
    [SerializeField] private TMP_Text countdownTextTMP;
#endif

    [Header("Блокировка управления")]
    [Tooltip("Отключить эти компоненты на время вступления (к примеру: PlayerController, CameraController, Shooter и т.п.)")]
    [SerializeField] private Behaviour[] behavioursToDisable;

#if ENABLE_INPUT_SYSTEM
    [Tooltip("Если используешь новую Input System — укажи PlayerInput игрока для блокировки ввода")]
    [SerializeField] private PlayerInput playerInput;
#endif

    [Header("Что запустить после скрытия")]
    [Tooltip("Объекты, которые должны включиться после старта (музыка, таймер, спаунеры и т.д.)")]
    public GameObject[] objectsToActivate;

    [Tooltip("События при старте (AudioSource.Play, запуск таймера и т.д.)")]
    public UnityEvent onGameStart;

    [Header("Пауза времени (опционально)")]
    [Tooltip("Замораживать ли игровое время на время вступления (Time.timeScale = 0)")]
    public bool pauseTimeWhileShowing = false;

    private bool started;
    private float prevTimeScale = 1f;

    private void Awake()
    {
        if (startScreenUI != null) startScreenUI.SetActive(true);
        SetGameElementsActive(false);
        LockControls(true);

        if (pauseTimeWhileShowing)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            // чтобы таймер тикал при паузе — рекомендуется useUnscaledTime = true
            if (!useUnscaledTime) useUnscaledTime = true;
        }

        UpdateCountdownLabel(showDuration);
    }

    private void OnEnable()
    {
        if (!started) StartCoroutine(HideAfterDelay());
    }

    private System.Collections.IEnumerator HideAfterDelay()
    {
        started = true;

        float elapsed = 0f;
        while (elapsed < showDuration)
        {
            float remaining = Mathf.Max(0f, showDuration - elapsed);
            UpdateCountdownLabel(remaining);

            yield return null;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        // финальный апдейт метки (0 сек)
        UpdateCountdownLabel(0f);

        if (pauseTimeWhileShowing)
            Time.timeScale = prevTimeScale;

        if (startScreenUI != null) startScreenUI.SetActive(false);

        LockControls(false);
        SetGameElementsActive(true);
        onGameStart?.Invoke();
    }

    private void LockControls(bool locked)
    {
        // Отключаем любые указанные компоненты управления
        if (behavioursToDisable != null)
        {
            foreach (var b in behavioursToDisable)
            {
                if (b != null) b.enabled = !locked;
            }
        }

#if ENABLE_INPUT_SYSTEM
        // Если указан PlayerInput — активируем/деактивируем ввод
        if (playerInput != null)
        {
            if (locked) playerInput.DeactivateInput();
            else playerInput.ActivateInput();
        }
#endif
    }

    private void SetGameElementsActive(bool value)
    {
        if (objectsToActivate == null) return;
        foreach (var obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(value);
        }
    }

    private void UpdateCountdownLabel(float secondsLeft)
    {
        // округлим вверх, чтобы показывать целые секунды: 3,2,1,0
        int secs = Mathf.CeilToInt(secondsLeft);

        if (countdownTextUI != null)
            countdownTextUI.text = secs.ToString();

#if TMP_PRESENT
        if (countdownTextTMP != null)
            countdownTextTMP.text = secs.ToString();
#endif
    }
}
