#if UNITY_EDITOR
using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-time helper that creates the penalty countdown Text (TMP) under the existing Canvas and
/// wires it into a PenaltyUI component (GDD v3.0 - Section V). Safe and idempotent: it skips
/// creation if the objects already exist, and uses Undo so it is fully reversible.
/// Run from the menu: "GDD Tools > Create Penalty Countdown UI".
/// </summary>
public static class GDDRespawnUITool
{
    private const string TextObjectName = "PenaltyCountdownText";

    [MenuItem("GDD Tools/Create Penalty Countdown UI")]
    public static void CreatePenaltyUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GDD Respawn UI] No Canvas found in the open scene. Add a Canvas first.");
            return;
        }

        // 1. Create (or reuse) the TMP text object centred on screen.
        Transform existing = canvas.transform.Find(TextObjectName);
        TextMeshProUGUI tmp;

        if (existing != null)
        {
            tmp = existing.GetComponent<TextMeshProUGUI>();
            Debug.Log("[GDD Respawn UI] Reusing existing PenaltyCountdownText.");
        }
        else
        {
            GameObject textGO = new GameObject(TextObjectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGO, "Create Penalty Countdown UI");
            textGO.transform.SetParent(canvas.transform, false);

            RectTransform rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600f, 120f);

            tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "REBOOTING... 5";
            tmp.fontSize = 48f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.85f, 0.2f); // amber "system" warning tone

            textGO.SetActive(false); // hidden until a penalty starts
            Debug.Log("[GDD Respawn UI] Created PenaltyCountdownText.");
        }

        // 2. Ensure a PenaltyUI lives on the (always-active) Canvas and references the text.
        PenaltyUI penaltyUI = canvas.GetComponent<PenaltyUI>();
        if (penaltyUI == null)
        {
            penaltyUI = Undo.AddComponent<PenaltyUI>(canvas.gameObject);
        }

        SerializedObject so = new SerializedObject(penaltyUI);
        SerializedProperty prop = so.FindProperty("penaltyText");
        prop.objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(penaltyUI);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[GDD Respawn UI] PenaltyUI linked to PenaltyCountdownText. Review and save the scene (Ctrl+S).");
    }
}
#endif
