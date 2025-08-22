#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrickHouseAnimator))]
public class BrickHouseAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var anim = (BrickHouseAnimator)target;

        // Важные подсказки
        if (anim.oldParent == null || anim.newParent == null)
        {
            EditorGUILayout.HelpBox("Укажи oldParent (где сейчас кирпичи) и newParent (куда собираем).", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // Слайдер прогресса
        EditorGUI.BeginChangeCheck();
        float p = EditorGUILayout.Slider("Анимация", anim.progress, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(anim, "Change progress");
            anim.progress = p;
            anim.UpdateBricks(); // только обновляем позы, не трогаем кэш
            EditorUtility.SetDirty(anim);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();

        // Кнопки
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("⟳ Refresh Cache"))
            {
                Undo.RecordObject(anim, "Refresh Cache");
                anim.RefreshCache();
                anim.UpdateBricks();
                EditorUtility.SetDirty(anim);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("⏮ Snap 0"))
            {
                Undo.RecordObject(anim, "Snap 0");
                anim.progress = 0f;
                anim.UpdateBricks();
                EditorUtility.SetDirty(anim);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("⏭ Snap 1"))
            {
                Undo.RecordObject(anim, "Snap 1");
                anim.progress = 1f;
                anim.UpdateBricks();
                EditorUtility.SetDirty(anim);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.Space();
        DrawDefaultInspector(); // остальные поля (родители/кривая/дистанция)
    }
}
#endif
