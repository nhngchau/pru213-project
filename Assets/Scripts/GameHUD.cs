using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top-screen HUD (GDD v3.0 - Section VII): Player HP (top-left, green), the Build Progress bar with
/// the current wave number nested in its label (top-center), and the DataPack counter (top-right).
/// Pure presentation - listens to GameEvents + PlayerEvents only. Lives on the always-active Canvas.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Player HP (top left)")]
    [SerializeField] private Slider playerHPSlider;
    [SerializeField] private Image playerHPFill;        // Image Type = Filled, Horizontal (green)
    [SerializeField] private TMP_Text playerHPLabel;

    [Header("Server HP")]
    [SerializeField] private Slider serverHPSlider;
    [SerializeField] private TMP_Text serverHPLabel;

    [Header("Build Progress (top center)")]
    [SerializeField] private Slider buildProgressSlider;
    [SerializeField] private Image buildProgressFill;   // Image Type = Filled, Horizontal
    [SerializeField] private TMP_Text buildProgressLabel;

    [Header("Build Blocked Warning")]
    [SerializeField] private bool autoCreateBlockedBanner = true;
    [SerializeField] private Color blockedColor = new Color(1f, 0.32f, 0.28f, 1f);

    [Header("DataPack (top right)")]
    [SerializeField] private TMP_Text dataPackText;

    [Header("EXP / Level (optional)")]
    [SerializeField] private bool autoCreateExpBar = true;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text expLabel;
    [SerializeField] private Color expBackgroundColor = new Color(0.03f, 0.07f, 0.11f, 0.88f);
    [SerializeField] private Color expFillColor = new Color(0.1f, 0.75f, 1f, 0.95f);
    [SerializeField] private Color expTextColor = Color.white;

    private int currentWave = 1;
    private float buildPercent;

    private TMP_Text blockedBanner;
    private int blockedCount;
    private string blockerName = "BUG";
    private Image cachedBuildFill;
    private Color buildFillNormalColor;
    private bool hasCachedBuildFillColor;

    void Awake()
    {
        BindOptionalServerHPUI();
        EnsureExpUI();
    }

    void Start()
    {
        ShowStageIntro();
    }

    void OnEnable()
    {
        BindOptionalServerHPUI();
        EnsureExpUI();
        GameEvents.OnBuildProgressChanged += HandleProgress;
        GameEvents.OnBuildBlockedChanged += HandleBuildBlocked;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnDataPackChanged += HandleDataPack;
        GameEvents.OnExpChanged += HandleExp;
        GameEvents.OnServerHealthChanged += HandleServerHP;
        PlayerEvents.OnPlayerHealthChanged += HandlePlayerHP;
    }

    void OnDisable()
    {
        GameEvents.OnBuildProgressChanged -= HandleProgress;
        GameEvents.OnBuildBlockedChanged -= HandleBuildBlocked;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnDataPackChanged -= HandleDataPack;
        GameEvents.OnExpChanged -= HandleExp;
        GameEvents.OnServerHealthChanged -= HandleServerHP;
        PlayerEvents.OnPlayerHealthChanged -= HandlePlayerHP;
    }

    void Update()
    {
        // Nhấp nháy nhẹ để kéo mắt về cảnh báo — thanh build đứng im mà không có gì động
        // thì người chơi sẽ tưởng game bị treo.
        if (blockedBanner == null || !blockedBanner.gameObject.activeSelf)
        {
            return;
        }

        float pulse = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
        blockedBanner.color = new Color(blockedColor.r, blockedColor.g, blockedColor.b, pulse);
    }

    // GDD: Player HP bar (green, top-left).
    private void HandlePlayerHP(int current, int max)
    {
        if (playerHPSlider != null)
        {
            playerHPSlider.minValue = 0f;
            playerHPSlider.maxValue = Mathf.Max(1, max);
            playerHPSlider.value = Mathf.Clamp(current, 0, max);
        }

        if (playerHPSlider == null && playerHPFill != null)
        {
            playerHPFill.fillAmount = max > 0 ? (float)current / max : 0f;
        }
        if (playerHPLabel != null)
        {
            playerHPLabel.text = $"HP {current}/{max}";
        }
    }

    private void HandleProgress(float percent)
    {
        buildPercent = percent;
        if (buildProgressSlider != null)
        {
            buildProgressSlider.minValue = 0f;
            buildProgressSlider.maxValue = 100f;
            buildProgressSlider.value = Mathf.Clamp(percent, 0f, 100f);
        }

        if (buildProgressSlider == null && buildProgressFill != null)
        {
            buildProgressFill.fillAmount = percent / 100f;
        }
        UpdateLabel();
    }

    private void HandleServerHP(int current, int max)
    {
        if (serverHPSlider != null)
        {
            serverHPSlider.minValue = 0f;
            serverHPSlider.maxValue = Mathf.Max(1, max);
            serverHPSlider.value = Mathf.Clamp(current, 0, max);
        }

        if (serverHPLabel != null)
        {
            serverHPLabel.text = $"SERVER {current}/{max}";
        }
    }

    private void HandleWaveStarted(int wave)
    {
        currentWave = wave;
        UpdateLabel();
    }

    private void HandleBuildBlocked(int count, string bugName)
    {
        blockedCount = Mathf.Max(0, count);
        if (!string.IsNullOrEmpty(bugName))
        {
            blockerName = bugName;
        }

        EnsureBlockedBanner();

        if (blockedBanner != null)
        {
            blockedBanner.gameObject.SetActive(blockedCount > 0);
            if (blockedCount > 0)
            {
                blockedBanner.text = $"BUILD BLOCKED   -   {blockedCount}x {blockerName}";
            }
        }

        TintBuildFill(blockedCount > 0);
        UpdateLabel();
    }

    /// <summary>Nhuộm đỏ thanh build khi bị chặn, trả lại màu gốc khi thông.</summary>
    private void TintBuildFill(bool blocked)
    {
        if (cachedBuildFill == null)
        {
            cachedBuildFill = buildProgressFill;

            if (cachedBuildFill == null && buildProgressSlider != null && buildProgressSlider.fillRect != null)
            {
                cachedBuildFill = buildProgressSlider.fillRect.GetComponent<Image>();
            }
        }

        if (cachedBuildFill == null)
        {
            return;
        }

        if (!hasCachedBuildFillColor)
        {
            buildFillNormalColor = cachedBuildFill.color;
            hasCachedBuildFillColor = true;
        }

        cachedBuildFill.color = blocked ? blockedColor : buildFillNormalColor;
    }

    private void EnsureBlockedBanner()
    {
        if (blockedBanner != null || !autoCreateBlockedBanner)
        {
            return;
        }

        Transform existing = FindChildRecursive(transform, "BuildBlockedBanner");
        if (existing != null)
        {
            blockedBanner = existing.GetComponent<TMP_Text>();
            return;
        }

        RectTransform root = CreateUIObject("BuildBlockedBanner", transform);
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -64f);
        root.sizeDelta = new Vector2(640f, 38f);

        blockedBanner = root.gameObject.AddComponent<TextMeshProUGUI>();
        blockedBanner.alignment = TextAlignmentOptions.Center;
        blockedBanner.fontSize = 24f;
        blockedBanner.fontStyle = FontStyles.Bold;
        blockedBanner.color = blockedColor;
        blockedBanner.raycastTarget = false;
        blockedBanner.text = string.Empty;
        blockedBanner.gameObject.SetActive(false);
    }

    private void UpdateLabel()
    {
        if (buildProgressLabel != null)
        {
            string buildState = blockedCount > 0 ? "BLOCKED" : $"{Mathf.RoundToInt(buildPercent)}%";
            buildProgressLabel.text = $"STAGE {RunProgress.Stage}/{RunProgress.MaxStage}   |   WAVE {currentWave}   |   BUILD {buildState}";
        }
    }

    private void HandleDataPack(int total)
    {
        if (dataPackText != null)
        {
            dataPackText.text = $"DataPack: {total}";
        }
    }

    private void HandleExp(int current, int required, int level)
    {
        EnsureExpUI();

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = Mathf.Max(1, required);
            expSlider.value = Mathf.Clamp(current, 0, required);
        }

        if (expSlider == null && expFill != null)
        {
            expFill.fillAmount = required > 0 ? (float)current / required : 0f;
        }

        if (expLabel != null)
        {
            // Hai đại lượng khác nhau, không gộp làm một nữa:
            // POWER = tổng power-up đã lấy cả run (thước đo sức mạnh thật, không reset mỗi stage).
            // NEXT UPGRADE = quãng đường tới lần chọn nâng cấp kế tiếp trong stage này.
            expLabel.text = $"POWER {RunProgress.TotalPowerUpLevels}   |   NEXT UPGRADE {current}/{required}";
        }
    }

    private void EnsureExpUI()
    {
        if (!autoCreateExpBar || (expSlider != null && expLabel != null))
        {
            return;
        }

        Transform existingRoot = FindChildRecursive(transform, "EXPBar");
        if (existingRoot != null)
        {
            BindExpChildren(existingRoot);
            return;
        }

        RectTransform root = CreateUIObject("EXPBar", transform);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(24f, -88f);
        root.sizeDelta = new Vector2(280f, 34f);

        Image background = root.gameObject.AddComponent<Image>();
        background.color = expBackgroundColor;
        background.raycastTarget = false;

        expSlider = root.gameObject.AddComponent<Slider>();
        expSlider.minValue = 0f;
        expSlider.maxValue = 120f; // khớp baseExpRequired; bị ghi đè ngay ở HandleExp đầu tiên
        expSlider.value = 0f;
        expSlider.transition = Selectable.Transition.None;

        RectTransform fillArea = CreateUIObject("Fill Area", root);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(3f, 3f);
        fillArea.offsetMax = new Vector2(-3f, -3f);

        RectTransform fillRoot = CreateUIObject("EXPFill", fillArea);
        fillRoot.anchorMin = Vector2.zero;
        fillRoot.anchorMax = Vector2.one;
        fillRoot.offsetMin = Vector2.zero;
        fillRoot.offsetMax = Vector2.zero;

        expFill = fillRoot.gameObject.AddComponent<Image>();
        expFill.color = expFillColor;
        expFill.raycastTarget = false;
        expSlider.fillRect = fillRoot;
        expSlider.targetGraphic = expFill;
        expSlider.interactable = false;

        RectTransform labelRoot = CreateUIObject("EXPLabel", root);
        labelRoot.anchorMin = Vector2.zero;
        labelRoot.anchorMax = Vector2.one;
        labelRoot.offsetMin = Vector2.zero;
        labelRoot.offsetMax = Vector2.zero;

        expLabel = labelRoot.gameObject.AddComponent<TextMeshProUGUI>();
        expLabel.alignment = TextAlignmentOptions.Center;
        expLabel.fontSize = 16f;
        expLabel.fontStyle = FontStyles.Bold;
        expLabel.color = expTextColor;
        expLabel.raycastTarget = false;
        expLabel.text = "POWER 0   |   NEXT UPGRADE 0/120";
    }

    private void BindExpChildren(Transform root)
    {
        Transform fill = FindChildRecursive(root, "EXPFill");
        Transform label = FindChildRecursive(root, "EXPLabel");
        expSlider = root.GetComponent<Slider>();

        if (expFill == null && fill != null)
        {
            expFill = fill.GetComponent<Image>();
        }

        if (expLabel == null && label != null)
        {
            expLabel = label.GetComponent<TMP_Text>();
        }
    }

    private void BindOptionalServerHPUI()
    {
        if (serverHPSlider == null)
        {
            Transform sliderRoot = FindChildRecursive(transform, "ServerHPSlider");
            if (sliderRoot == null)
            {
                sliderRoot = FindChildRecursive(transform, "ServerHPBar");
            }

            if (sliderRoot != null)
            {
                serverHPSlider = sliderRoot.GetComponent<Slider>();
            }
        }

        if (serverHPLabel == null)
        {
            Transform labelRoot = FindChildRecursive(transform, "ServerHPLabel");
            if (labelRoot == null)
            {
                labelRoot = FindChildRecursive(transform, "ServerHPText");
            }

            if (labelRoot != null)
            {
                serverHPLabel = labelRoot.GetComponent<TMP_Text>();
            }
        }
    }

    private static RectTransform CreateUIObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void ShowStageIntro()
    {
        RectTransform root = CreateUIObject("StageIntro", transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        TMP_Text text = root.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = RunProgress.Stage >= RunProgress.MaxStage ? "FINAL STAGE" : $"STAGE {RunProgress.Stage}";
        text.fontSize = 90f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0f);
        text.raycastTarget = false;
        
        // Thêm outline cho chữ nổi bật
        text.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(0, 0, 0, 255);

        StartCoroutine(StageIntroRoutine(text));
    }

    private System.Collections.IEnumerator StageIntroRoutine(TMP_Text text)
    {
        float elapsed = 0f;
        float fadeTime = 0.6f;
        float holdTime = 1.5f;

        // Fade in
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            text.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, elapsed / fadeTime));
            yield return null;
        }

        // Hold
        yield return new WaitForSecondsRealtime(holdTime);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            text.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, elapsed / fadeTime));
            yield return null;
        }

        if (text != null && text.gameObject != null)
        {
            Destroy(text.gameObject);
        }
    }
}
