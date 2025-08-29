using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DistanceDrivenAnimation : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Игрок. Если не задан, попробует найти объект с тегом Player.")]
    [SerializeField] private Transform player;
    [Tooltip("Целевая точка, к которой измеряем дистанцию.")]
    [SerializeField] private Transform targetPoint;

    [Header("Диапазон дистанций (м)")]
    [Min(0f)] public float nearDistance = 0f;   // при этой дистанции анимация на 100%
    [Min(0f)] public float farDistance = 10f;  // при этой дистанции анимация на 0%

    [Header("Преобразование кривой")]
    [Tooltip("Кривая ремапа: вход 0..1 (далеко->близко), выход 0..1 (нормализ. время клипа)")]
    public AnimationCurve remap = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Animator")]
    [Tooltip("Имя состояния с нужным клипом (на соответствующем слое).")]
    public string stateName = "YourState";
    [Tooltip("Слой аниматора, обычно 0.")]
    public int layerIndex = 0;
    [Tooltip("Останавливать Animator, чтобы управлять временем вручную.")]
    public bool pauseAnimator = true;

    [Header("Прочее")]
    [Tooltip("Игнорировать высоту (XY-плоскость). Полезно для 2D/топ-даун.")]
    public bool use2D = false;

    private Animator _anim;
    private int _stateHash;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        if (pauseAnimator) _anim.speed = 0f;

        _stateHash = Animator.StringToHash(stateName);

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    private void OnValidate()
    {
        if (farDistance < nearDistance)
            farDistance = nearDistance;
    }

    private void Update()
    {
        if (_anim == null || targetPoint == null || player == null || string.IsNullOrEmpty(stateName))
            return;

        // Дистанция
        float dist = use2D
            ? Vector2.Distance(new Vector2(player.position.x, player.position.y),
                               new Vector2(targetPoint.position.x, targetPoint.position.y))
            : Vector3.Distance(player.position, targetPoint.position);

        // 0..1: 0 = далеко (>= farDistance), 1 = близко (<= nearDistance)
        float proximity01 = 1f - Mathf.InverseLerp(nearDistance, farDistance, dist);

        // Ремап в нормализованное время клипа
        float t = Mathf.Clamp01(remap.Evaluate(Mathf.Clamp01(proximity01)));

        // Скрубим состояние (без проигрывания, кадр в кадр)
        _anim.Play(_stateHash, layerIndex, t);
        _anim.Update(0f); // применить мгновенно
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (targetPoint == null) return;
        Gizmos.matrix = Matrix4x4.identity;

        // Внутренняя сфера (near)
        Gizmos.DrawWireSphere(targetPoint.position, Mathf.Max(0f, nearDistance));
        // Внешняя сфера (far)
        Gizmos.DrawWireSphere(targetPoint.position, Mathf.Max(nearDistance, farDistance));
    }
#endif
}
