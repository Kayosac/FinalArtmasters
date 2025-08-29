using System.Collections;
using UnityEngine;

public class AnimationSwapObjects : MonoBehaviour
{
    [Header("Что отключить / включить")]
    [SerializeField] private GameObject objectToHide;
    [SerializeField] private GameObject objectToShow;

    [Tooltip("Необязательно: задержка с момента Animation Event, сек.")]
    [SerializeField] private float delaySeconds = 0f;

    private bool _done;

    /// <summary>
    /// Вызывается из Animation Event (без параметров).
    /// </summary>
    public void SwapActive()
    {
        if (delaySeconds <= 0f) DoSwap();
        else StartCoroutine(SwapAfterDelay());
    }

    private IEnumerator SwapAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);
        DoSwap();
    }

    private void DoSwap()
    {
        if (_done) return; // чтобы не триггерить дважды
        if (objectToHide != null) objectToHide.SetActive(false);
        if (objectToShow != null) objectToShow.SetActive(true);
        _done = true;
    }

    // Дополнительно: методы на случай, если захочешь 2 отдельных события в один кадр
    public void Hide(GameObject go) { if (go) go.SetActive(false); }
    public void Show(GameObject go) { if (go) go.SetActive(true); }

    // Удобно тестировать из контекстного меню в инспекторе
    [ContextMenu("Test Swap Now")]
    private void _TestNow() => DoSwap();
}
