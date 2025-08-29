using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Timer Settings")]
    [Min(0f)] public float durationSeconds = 120f;
    public bool startOnEnable = true;
    public bool useUnscaledTime = false;

    [Header("Outro Animation")]
    public Animator outroAnimator;           // Animator to play outro on
    public bool outroUseTrigger = true;      // true = SetTrigger; false = CrossFade to state
    public string outroTriggerName = "Outro";
    public string outroStateName = "";       // used if outroUseTrigger == false
    public int outroLayer = 0;
    public float outroCrossFade = 0.1f;

    [Tooltip("Temporarily force animator to UnscaledTime during outro (useful if timeScale == 0).")]
    public bool forceUnscaledAnimatorDuringOutro = true;

    [Tooltip("If > 0, wait exactly this many seconds after firing the outro, then quit.")]
    [Min(0f)] public float outroWaitSeconds = 0f;

    [Tooltip("If no fixed wait is set, wait until the target state finishes (normalizedTime >= 1).")]
    public bool waitForStateToFinish = true;

    [Tooltip("Safety timeout while waiting for state to finish (seconds).")]
    [Min(0f)] public float outroMaxWait = 6f;

    [Header("Events")]
    public UnityEvent onTimerFinished;   // fires when timer hits 0 (before outro)
    public UnityEvent onOutroStarted;    // fires right after triggering outro
    public UnityEvent onQuit;            // fires right before quitting

    private float timeLeft;
    private bool running;
    private int lastShownSecond = -1;

    private void OnEnable()
    {
        ResetTimer();
        if (startOnEnable) StartTimer();
        UpdateLabel(true);
    }

    public void StartTimer() { running = true; }
    public void StopTimer() { running = false; }

    public void ResetTimer()
    {
        timeLeft = Mathf.Max(0f, durationSeconds);
        running = false;
        lastShownSecond = -1;
    }

    public void AddTime(float seconds)
    {
        timeLeft = Mathf.Max(0f, timeLeft + seconds);
        UpdateLabel(true);
    }

    private void Update()
    {
        if (!running) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        timeLeft -= dt;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateLabel(true);
            running = false;
            StartCoroutine(FinishThenQuit());
            return;
        }

        UpdateLabel(false);
    }

    private void UpdateLabel(bool force)
    {
        if (timerText == null) return;

        int secondsInt = Mathf.CeilToInt(timeLeft);
        if (!force && secondsInt == lastShownSecond) return;
        lastShownSecond = secondsInt;

        int minutes = secondsInt / 60;
        int seconds = secondsInt % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private System.Collections.IEnumerator FinishThenQuit()
    {
        onTimerFinished?.Invoke();

        // Play outro and wait as configured
        yield return StartCoroutine(PlayOutroCoroutine());

        onQuit?.Invoke();
        QuitGame();
    }

    private System.Collections.IEnumerator PlayOutroCoroutine()
    {
        if (outroAnimator == null)
            yield break;

        var prevMode = outroAnimator.updateMode;
        if (forceUnscaledAnimatorDuringOutro)
            outroAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Trigger or crossfade to state
        if (outroUseTrigger)
        {
            if (!string.IsNullOrEmpty(outroTriggerName))
                outroAnimator.SetTrigger(outroTriggerName);
        }
        else
        {
            if (!string.IsNullOrEmpty(outroStateName))
                outroAnimator.CrossFade(outroStateName, outroCrossFade, outroLayer);
        }

        onOutroStarted?.Invoke();

        // Waiting strategy
        if (outroWaitSeconds > 0f)
        {
            float t = 0f;
            while (t < outroWaitSeconds)
            {
                t += (useUnscaledTime || forceUnscaledAnimatorDuringOutro) ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }
        else if (waitForStateToFinish && !string.IsNullOrEmpty(outroStateName))
        {
            int targetHash = Animator.StringToHash(outroStateName);

            // allow animator to enter the state
            yield return null;

            float t = 0f;
            while (t < Mathf.Max(0.1f, outroMaxWait))
            {
                var info = outroAnimator.GetCurrentAnimatorStateInfo(outroLayer);
                bool inTransition = outroAnimator.IsInTransition(outroLayer);
                bool isTarget = info.shortNameHash == targetHash || info.fullPathHash == targetHash;

                if (isTarget && !inTransition && info.normalizedTime >= 1f)
                    break;

                t += (useUnscaledTime || forceUnscaledAnimatorDuringOutro) ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // at least one frame so triggers can fire
            yield return null;
        }

        // restore update mode
        outroAnimator.updateMode = prevMode;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
