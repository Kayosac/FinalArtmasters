using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(10000)]
public class BrickHouseAnimator : MonoBehaviour
{
    [Header("Контроль (0 — старый дом, 1 — новый дом)")]
    [Range(0f, 1f)] public float progress = 0f;

    [Header("Родители")]
    public Transform oldParent;
    public Transform newParent;

    [Header("Послойность")]
    [Range(0f, 1f)] public float disassembleStagger = 1f;
    [Range(0f, 1f)] public float assembleStagger = 1f;
    [Tooltip("Толщина слоя (локальный Y). 0 — плавное распределение по высоте.")]
    public float layerHeight = 0f;

    [Header("Эффекты разлёта")]
    public float explosionDistance = 1f;
    public int randomSeed = 12345;

    [Header("Плавность движения внутри окна")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Дуга (вертикальная парабола)")]
    public bool useParabolicPath = true;
    public float disassembleArcHeight = 0.2f;
    public float assembleArcHeight = 0.2f;
    public bool arcHeightRelative = true;

    public enum ArcUpSpace { WorldUp, OldParentUp, NewParentUp, CustomTransform }
    public ArcUpSpace arcUp = ArcUpSpace.WorldUp;
    public Transform customUp;

    // ---------- Wrap Around (облепление вокруг оси) ----------
    [Header("Wrap Around Pivot (облепление вокруг оси)")]
    public bool useWrapAround = true;
    public Transform wrapPivotOld; // по умолчанию = oldParent
    public Transform wrapPivotNew; // по умолчанию = newParent

    public enum WrapAxisSpace { PivotUp, WorldUp, OldParentUp, NewParentUp, CustomTransform }
    public WrapAxisSpace wrapAxis = WrapAxisSpace.PivotUp;
    public Transform wrapCustomAxis;

    public float swirlAngleDegDisassemble = 120f;
    public float swirlAngleDegAssemble = 120f;
    public float radiusBoostFactor = 0.25f;
    public AnimationCurve spinCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    public AnimationCurve radiusCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

    // ---------- УСТОЙЧИВЫЙ СНАПШОТ ----------
    [Header("Снапшот")]
    [Tooltip("Когда включено — скрипт НИКОГДА не пересобирает снапшот сам. Запекайте кнопками ниже.")]
    public bool lockSnapshot = true;

    [System.Serializable]
    private class BrickData
    {
        public Transform tr;
        public Vector3 localPos;         // эталонная локальная позиция (используется для old и new)
        public Quaternion localRot;
        public Vector3 explodeOffsetWS;  // стабильный офсет «взрыва»
        public float yNorm;              // 0..1 (низ..верх) для послойности
        public float spinSign;           // -1/+1 для разнонаправленного свирла
        public float spinJitter;         // разброс угла
        public float radJitter;          // разброс радиуса
    }

    [SerializeField] private List<BrickData> bricks = new List<BrickData>();

    // Animator/Timeline
    private float _lastProgress = -1f;
    private bool _animDirty = false;
    private Vector3 _lastOldPos, _lastNewPos, _lastOldScale, _lastNewScale;
    private Quaternion _lastOldRot, _lastNewRot;

    private const float SNAP_TOL = 0.02f; // допуск для «почти 0/1»

    private void OnEnable()
    {
        if (oldParent == null) oldParent = transform;
        // НИЧЕГО не кэшируем автоматически — доверяем существующему снапшоту.
        CaptureParentsTRS();
        UpdateBricks();
        _lastProgress = progress;
    }

    private void OnValidate() { UpdateBricks(); }
    private void OnDidApplyAnimationProperties() { _animDirty = true; }

    private void LateUpdate()
    {
        bool parentsChanged = ParentsChanged();
        bool progressChanged = !Mathf.Approximately(_lastProgress, progress);

        if (_animDirty || progressChanged || parentsChanged)
        {
            UpdateBricks();
            _lastProgress = progress;
            _animDirty = false;
            CaptureParentsTRS();
        }
    }

    // ========== ПУБЛИЧНЫЕ КНОПКИ-СНАПЫ ==========
    [ContextMenu("Bake Snapshot From OLD (progress≈0)")]
    public void BakeFromOld()
    {
        if (!oldParent) return;
        if (progress > SNAP_TOL)
            Debug.LogWarning("[BrickHouseAnimator] BakeFromOld желательно делать при progress≈0, иначе снимете неверную позу.");

        Random.InitState(randomSeed);
        bricks.Clear();

        // Собираем детей в текущем oldParent
        List<(Transform tr, Vector3 lp, Quaternion lr)> tmp = new();
        foreach (Transform c in oldParent) tmp.Add((c, c.localPosition, c.localRotation));

        // Нормализация по высоте (для послойности)
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        foreach (var t in tmp)
        {
            float y = (layerHeight > 0f) ? Mathf.Round(t.lp.y / Mathf.Max(0.0001f, layerHeight)) * Mathf.Max(0.0001f, layerHeight) : t.lp.y;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        foreach (var t in tmp)
        {
            float y = (layerHeight > 0f) ? Mathf.Round(t.lp.y / Mathf.Max(0.0001f, layerHeight)) * Mathf.Max(0.0001f, layerHeight) : t.lp.y;
            bricks.Add(new BrickData
            {
                tr = t.tr,
                localPos = t.lp,
                localRot = t.lr,
                explodeOffsetWS = Random.insideUnitSphere.normalized * explosionDistance,
                yNorm = (maxY - minY) < 1e-5f ? 0f : Mathf.InverseLerp(minY, maxY, y),
                spinSign = Random.value < 0.5f ? -1f : 1f,
                spinJitter = Random.Range(0.7f, 1.3f),
                radJitter = Random.Range(0.7f, 1.3f),
            });
        }

        Debug.Log("[BrickHouseAnimator] Snapshot baked from OLD.");
    }

    [ContextMenu("Bake Snapshot From NEW (progress≈1)")]
    public void BakeFromNew()
    {
        if (!oldParent || !newParent) return;
        if (progress < 1f - SNAP_TOL)
            Debug.LogWarning("[BrickHouseAnimator] BakeFromNew желательно делать при progress≈1.");

        Random.InitState(randomSeed);
        bricks.Clear();

        // Берём текущие мировые позиции детей и переводим их в локальные координаты НОВОГО родителя
        List<Transform> kids = new();
        foreach (Transform c in oldParent) kids.Add(c);

        // Подготовка для послойности: измеряем Y в локале newParent
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        List<Vector3> newLocal = new();
        foreach (var t in kids)
        {
            Vector3 lpNew = newParent.InverseTransformPoint(t.position);
            if (layerHeight > 0f) lpNew.y = Mathf.Round(lpNew.y / Mathf.Max(0.0001f, layerHeight)) * Mathf.Max(0.0001f, layerHeight);
            newLocal.Add(lpNew);
            minY = Mathf.Min(minY, lpNew.y);
            maxY = Mathf.Max(maxY, lpNew.y);
        }

        for (int i = 0; i < kids.Count; i++)
        {
            Transform tr = kids[i];
            Vector3 lpNew = newLocal[i];

            bricks.Add(new BrickData
            {
                tr = tr,
                // ВАЖНО: локальная поза берётся от NEW, но хранится одна — модель симметрична.
                localPos = lpNew,
                localRot = Quaternion.Inverse(newParent.rotation) * tr.rotation,
                explodeOffsetWS = Random.insideUnitSphere.normalized * explosionDistance,
                yNorm = (maxY - minY) < 1e-5f ? 0f : Mathf.InverseLerp(minY, maxY, lpNew.y),
                spinSign = Random.value < 0.5f ? -1f : 1f,
                spinJitter = Random.Range(0.7f, 1.3f),
                radJitter = Random.Range(0.7f, 1.3f),
            });
        }

        Debug.Log("[BrickHouseAnimator] Snapshot baked from NEW.");
    }

    // Старый RefreshCache оставляем как прокси, чтобы случайный вызов не сломал всё.
    [ContextMenu("Refresh Cache (safe)")]
    public void RefreshCache()
    {
        if (lockSnapshot && bricks != null && bricks.Count > 0)
        {
            Debug.Log("[BrickHouseAnimator] Snapshot is locked; no refresh.");
            return;
        }

        // Если очень нужно обновить автоматически: печём от той стороны, к которой ближе progress
        if (progress <= 0.5f) BakeFromOld(); else BakeFromNew();
    }

    // ========== ГЕОМЕТРИЯ ДВИЖЕНИЯ ==========
    public void UpdateBricks()
    {
        if (!oldParent || !newParent || bricks == null || bricks.Count == 0) return;

        float p = Mathf.Clamp01(progress);
        const float DIS_END = 0.5f, ASM_START = 0.5f;

        Vector3 up = GetUpDir();

        for (int i = 0; i < bricks.Count; i++)
        {
            var b = bricks[i];
            if (!b.tr) continue;

            Vector3 startPos = oldParent.TransformPoint(b.localPos);
            Quaternion startRot = oldParent.rotation * b.localRot;

            Vector3 endPos = newParent.TransformPoint(b.localPos);
            Quaternion endRot = newParent.rotation * b.localRot;

            Vector3 explodedPos = startPos + b.explodeOffsetWS;

            float disStart = (1f - b.yNorm) * (disassembleStagger * DIS_END);
            float dRaw = Mathf.InverseLerp(disStart, DIS_END, p);
            float d = moveCurve.Evaluate(Mathf.Clamp01(dRaw));

            float asmStart = ASM_START + (b.yNorm) * (assembleStagger * (1f - ASM_START));
            float aRaw = Mathf.InverseLerp(asmStart, 1f, p);
            float a = moveCurve.Evaluate(Mathf.Clamp01(aRaw));

            if (p < 0.5f)
            {
                b.tr.position = useWrapAround
                    ? WrapPath(startPos, explodedPos, false, b, up, disassembleArcHeight, d)
                    : Parabola(startPos, explodedPos, up, disassembleArcHeight, arcHeightRelative, d);
                b.tr.rotation = startRot;
            }
            else
            {
                b.tr.position = useWrapAround
                    ? WrapPath(explodedPos, endPos, true, b, up, assembleArcHeight, a)
                    : Parabola(explodedPos, endPos, up, assembleArcHeight, arcHeightRelative, a);
                b.tr.rotation = Quaternion.Slerp(startRot, endRot, a);
            }
        }
    }

    private Vector3 GetUpDir()
    {
        switch (arcUp)
        {
            case ArcUpSpace.OldParentUp: return oldParent ? oldParent.up : Vector3.up;
            case ArcUpSpace.NewParentUp: return newParent ? newParent.up : Vector3.up;
            case ArcUpSpace.CustomTransform: return customUp ? customUp.up : Vector3.up;
            default: return Vector3.up;
        }
    }

    private Transform GetWrapPivot(bool assemblePhase) => assemblePhase ? (wrapPivotNew ? wrapPivotNew : newParent)
                                                                       : (wrapPivotOld ? wrapPivotOld : oldParent);
    private Vector3 GetWrapAxis(bool assemblePhase)
    {
        switch (wrapAxis)
        {
            case WrapAxisSpace.WorldUp: return Vector3.up;
            case WrapAxisSpace.OldParentUp: return oldParent ? oldParent.up : Vector3.up;
            case WrapAxisSpace.NewParentUp: return newParent ? newParent.up : Vector3.up;
            case WrapAxisSpace.CustomTransform: return wrapCustomAxis ? wrapCustomAxis.up : Vector3.up;
            default: return GetWrapPivot(assemblePhase) ? GetWrapPivot(assemblePhase).up : Vector3.up;
        }
    }

    private Vector3 Parabola(Vector3 a, Vector3 b, Vector3 up, float heightParam, bool relative, float t)
    {
        Vector3 p = Vector3.Lerp(a, b, t);
        float dist = Vector3.Distance(a, b);
        float h = relative ? heightParam * dist : heightParam;
        return p + up.normalized * (4f * h * t * (1f - t));
    }

    private Vector3 WrapPath(Vector3 a, Vector3 b, bool assemblePhase, BrickData bd, Vector3 upForArc, float arcHeight, float t)
    {
        Transform pivotT = GetWrapPivot(assemblePhase);
        Vector3 pivot = pivotT ? pivotT.position : Vector3.zero;
        Vector3 axisN = GetWrapAxis(assemblePhase).normalized;

        CylFromPoint(a, pivot, axisN, out float h0, out Vector3 dir0, out float R0);
        CylFromPoint(b, pivot, axisN, out float h1, out Vector3 dir1, out float R1);

        Vector3 dirBase = SafeSlerp(dir0, dir1, t);
        float h = Mathf.Lerp(h0, h1, t);
        float R = Mathf.Lerp(R0, R1, t);

        float swirlDeg = (assemblePhase ? swirlAngleDegAssemble : swirlAngleDegDisassemble) * bd.spinJitter;
        float spin = (spinCurve != null ? spinCurve.Evaluate(t) : 4f * t * (1f - t)) * swirlDeg * Mathf.Deg2Rad * bd.spinSign;
        Quaternion spinRot = Quaternion.AngleAxis(spin * Mathf.Rad2Deg, axisN);
        Vector3 dirSpun = spinRot * dirBase;

        float radBoost = (radiusCurve != null ? radiusCurve.Evaluate(t) : 4f * t * (1f - t))
                         * radiusBoostFactor * bd.radJitter * Mathf.Max(0.0f, R);
        float Rfinal = Mathf.Max(0.0f, R + radBoost);

        Vector3 pos = pivot + axisN * h + dirSpun * Rfinal;

        if (useParabolicPath)
        {
            float dist = Vector3.Distance(a, b);
            float hArc = arcHeightRelative ? arcHeight * dist : arcHeight;
            pos += upForArc.normalized * (4f * hArc * t * (1f - t));
        }

        return pos;
    }

    private static void CylFromPoint(Vector3 p, Vector3 pivot, Vector3 axisN, out float h, out Vector3 dir, out float R)
    {
        Vector3 v = p - pivot;
        h = Vector3.Dot(v, axisN);
        Vector3 radial = v - axisN * h;
        R = radial.magnitude;
        dir = (R > 1e-6f) ? radial / R : Ortho(axisN);
    }

    private static Vector3 Ortho(Vector3 n)
    {
        Vector3 a = Mathf.Abs(n.y) < 0.99f ? Vector3.up : Vector3.right;
        return Vector3.Normalize(Vector3.Cross(n, a));
    }

    private static Vector3 SafeSlerp(Vector3 a, Vector3 b, float t)
    {
        if (a.sqrMagnitude < 1e-10f) return b.normalized;
        if (b.sqrMagnitude < 1e-10f) return a.normalized;
        return Vector3.Slerp(a.normalized, b.normalized, t);
    }

    private void CaptureParentsTRS()
    {
        if (oldParent) { _lastOldPos = oldParent.position; _lastOldRot = oldParent.rotation; _lastOldScale = oldParent.lossyScale; }
        if (newParent) { _lastNewPos = newParent.position; _lastNewRot = newParent.rotation; _lastNewScale = newParent.lossyScale; }
    }
    private bool ParentsChanged()
    {
        if (!oldParent || !newParent) return false;
        return
            (_lastOldPos - oldParent.position).sqrMagnitude > 1e-10f ||
            Quaternion.Angle(_lastOldRot, oldParent.rotation) > 0.01f ||
            (_lastOldScale - oldParent.lossyScale).sqrMagnitude > 1e-10f ||
            (_lastNewPos - newParent.position).sqrMagnitude > 1e-10f ||
            Quaternion.Angle(_lastNewRot, newParent.rotation) > 0.01f ||
            (_lastNewScale - newParent.lossyScale).sqrMagnitude > 1e-10f;
    }
}
