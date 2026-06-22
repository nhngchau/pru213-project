#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-time, SAFE builder for the Wave HUD + Upgrade Panel (GDD v3.0 - Sections VI &amp; VII).
/// Uses only default Unity UI (Image / TextMeshPro / Button) with flat colors - no project sprites.
/// Everything is wrapped in Undo and the GameHUD / UpgradePanelUI references are wired automatically,
/// so you never have to hand-edit the scene file. Run from: "GDD Tools > Build Upgrade UI".
/// </summary>
public static class UpgradeUIBuilder
{
    private static readonly string[] UpgradeNames = { "Overclock CPU", "Upgrade RAM", "Firewall System" };
    private static Sprite UiSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    private static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.12f, 0.96f);
    private static readonly Color BarBg = new Color(0.15f, 0.15f, 0.18f, 1f);
    private static readonly Color BarFill = new Color(0.20f, 0.55f, 0.95f, 1f);   // build = blue (GDD)
    private static readonly Color BtnColor = new Color(0.22f, 0.45f, 0.30f, 1f);
    private static readonly Color SkipColor = new Color(0.45f, 0.22f, 0.22f, 1f);

    [MenuItem("GDD Tools/Build Upgrade UI")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[UpgradeUIBuilder] No Canvas in the open scene. Add one first.");
            return;
        }

        if (canvas.transform.Find("UpgradePanel") != null)
        {
            Debug.LogWarning("[UpgradeUIBuilder] 'UpgradePanel' already exists - delete it (and BuildProgressBar/DataPackText) before rebuilding.");
            return;
        }

        // ---------- HUD: Build Progress bar (top center) ----------
        RectTransform bar = CreateChild("BuildProgressBar", canvas.transform);
        SetAnchored(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(520f, 40f));
        AddImage(bar.gameObject, BarBg, UiSprite);

        RectTransform fill = CreateChild("Fill", bar);
        Stretch(fill, 2f);
        Image fillImg = AddImage(fill.gameObject, BarFill, UiSprite);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;

        RectTransform barLabelRt = CreateChild("Label", bar);
        Stretch(barLabelRt, 0f);
        TextMeshProUGUI barLabel = AddLabel(barLabelRt, "WAVE 1   -   BUILD 0%", 20f, Color.white, TextAlignmentOptions.Center);

        // ---------- HUD: DataPack counter (top right) ----------
        RectTransform dpRt = CreateChild("DataPackText", canvas.transform);
        SetAnchored(dpRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(320f, 40f));
        TextMeshProUGUI dpText = AddLabel(dpRt, "DataPack: 0", 22f, new Color(0.3f, 0.9f, 0.4f), TextAlignmentOptions.Right);

        // ---------- Upgrade Panel (center, hidden by default) ----------
        RectTransform panel = CreateChild("UpgradePanel", canvas.transform);
        SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 400f));
        AddImage(panel.gameObject, PanelBg, UiSprite);

        RectTransform titleRt = CreateChild("Title", panel);
        SetAnchored(titleRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(740f, 50f));
        AddLabel(titleRt, "WAVE COMPLETE  -  CHOOSE AN UPGRADE", 26f, Color.white, TextAlignmentOptions.Center);

        var buttons = new Button[3];
        var descTexts = new TMP_Text[3];
        var costTexts = new TMP_Text[3];
        float[] columnX = { -250f, 0f, 250f };

        for (int i = 0; i < 3; i++)
        {
            buttons[i] = CreateButton(panel, $"UpgradeButton_{i}", new Vector2(columnX[i], 70f), new Vector2(210f, 64f), UpgradeNames[i], BtnColor);

            RectTransform descRt = CreateChild($"Desc_{i}", panel);
            SetAnchored(descRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(columnX[i], -10f), new Vector2(220f, 60f));
            descTexts[i] = AddLabel(descRt, "-", 16f, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Center);

            RectTransform costRt = CreateChild($"Cost_{i}", panel);
            SetAnchored(costRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(columnX[i], -60f), new Vector2(220f, 30f));
            costTexts[i] = AddLabel(costRt, "Cost: -", 16f, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);
        }

        Button continueButton = CreateButton(panel, "ContinueButton", new Vector2(0f, -150f), new Vector2(260f, 52f), "Skip / Continue", SkipColor);

        // ---------- Attach + wire the UI controllers on the (always-active) Canvas ----------
        GameHUD hud = Undo.AddComponent<GameHUD>(canvas.gameObject);
        SerializedObject hudSO = new SerializedObject(hud);
        WireRef(hudSO, "buildProgressFill", fillImg);
        WireRef(hudSO, "buildProgressLabel", barLabel);
        WireRef(hudSO, "dataPackText", dpText);
        hudSO.ApplyModifiedProperties();

        UpgradePanelUI panelUI = Undo.AddComponent<UpgradePanelUI>(canvas.gameObject);
        SerializedObject panelSO = new SerializedObject(panelUI);
        WireRef(panelSO, "panelRoot", panel.gameObject);
        WireRef(panelSO, "continueButton", continueButton);
        WireArray(panelSO, "upgradeButtons", buttons);
        WireArray(panelSO, "descriptionTexts", descTexts);
        WireArray(panelSO, "costTexts", costTexts);
        panelSO.ApplyModifiedProperties();

        panel.gameObject.SetActive(false); // hidden until a wave ends

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[UpgradeUIBuilder] Built HUD + Upgrade Panel and wired GameHUD / UpgradePanelUI on the Canvas. Review and save (Ctrl+S).");
    }

    // ------------------------------------------------------------ helpers

    private static RectTransform CreateChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void SetAnchored(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    private static TextMeshProUGUI AddLabel(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color)
    {
        RectTransform rt = CreateChild(name, parent);
        SetAnchored(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        Image img = AddImage(rt.gameObject, color, UiSprite);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        RectTransform labelRt = CreateChild("Label", rt);
        Stretch(labelRt, 0f);
        AddLabel(labelRt, label, 18f, Color.white, TextAlignmentOptions.Center);

        return button;
    }

    private static void WireRef(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
        else Debug.LogWarning($"[UpgradeUIBuilder] Property '{prop}' not found.");
    }

    private static void WireArray(SerializedObject so, string prop, Object[] values)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning($"[UpgradeUIBuilder] Array '{prop}' not found."); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
