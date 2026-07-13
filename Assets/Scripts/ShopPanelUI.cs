using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityScreenNavigator.Runtime.Core.Modal;
using System.Collections;

public class ShopPanelUI : Modal
{
    [SerializeField] private TMP_Text dataPackText;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private Button[] buyButtons = new Button[4];
    [SerializeField] private TMP_Text[] titleTexts = new TMP_Text[4];
    [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[4];
    [SerializeField] private TMP_Text[] costTexts = new TMP_Text[4];
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button mainMenuButton;

    private ShopBoosterType[] boosters;
    private Coroutine feedbackRoutine;
    private bool isLeavingShop;

    public static GameObject CreateRuntimePanel(Transform parent)
    {
        GameObject panel = new GameObject("RuntimeShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image backdrop = panel.GetComponent<Image>();
        backdrop.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);

        panel.AddComponent<ShopPanelUI>();
        return panel;
    }

    private void Awake()
    {
        EnsureShopManager();
        AutoBindExistingReferences();
        AutoBindBuyButtons();
        EnsureRuntimeLayout();
        AutoBindExistingReferences();
        AutoBindBuyButtons();
        boosters = ShopManager.Instance.GetBoosters();

        for (int i = 0; i < buyButtons.Length; i++)
        {
            int index = i;
            if (buyButtons[i] != null)
            {
                buyButtons[i].onClick.AddListener(() => OnBuyClicked(index));
            }
        }

        if (nextStageButton != null)
        {
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnDataPackChanged += HandleDataPackChanged;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnDataPackChanged -= HandleDataPackChanged;
    }

    public override void DidPushEnter()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void HandleDataPackChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        EnsureShopManager();

        if (dataPackText != null)
        {
            dataPackText.text = $"DataPack: {RunProgress.DataPack}";
        }

        if (stageText != null)
        {
            stageText.text =
                $"Stage {RunProgress.Stage} complete\n" +
                $"Next Stage: {RunProgress.NextStage}\n" +
                RunProgress.GetStageDifficultySummary(RunProgress.NextStage);
        }

        for (int i = 0; i < boosters.Length && i < buyButtons.Length; i++)
        {
            ShopBoosterType booster = boosters[i];

            if (titleTexts != null && i < titleTexts.Length && titleTexts[i] != null)
            {
                titleTexts[i].text = ShopManager.Instance.GetTitle(booster);
            }

            if (descriptionTexts != null && i < descriptionTexts.Length && descriptionTexts[i] != null)
            {
                descriptionTexts[i].text = ShopManager.Instance.GetDescription(booster);
            }

            if (costTexts != null && i < costTexts.Length && costTexts[i] != null)
            {
                costTexts[i].text = ShopManager.Instance.IsMaxed(booster)
                    ? "MAX"
                    : $"Lv {RunProgress.GetBoosterLevel(booster)} | {ShopManager.Instance.GetCost(booster)} DP";
            }

            if (buyButtons[i] != null)
            {
                buyButtons[i].interactable = !ShopManager.Instance.IsMaxed(booster);
                SetButtonLabel(buyButtons[i], GetBuyButtonLabel(booster));
                SetButtonColor(buyButtons[i], ShopManager.Instance.CanBuy(booster)
                    ? new Color(0.13f, 0.42f, 0.9f, 1f)
                    : new Color(0.42f, 0.44f, 0.5f, 1f));
            }
        }
    }

    private void OnBuyClicked(int index)
    {
        if (boosters == null || index < 0 || index >= boosters.Length)
        {
            return;
        }

        ShopBoosterType booster = boosters[index];
        if (ShopManager.Instance.TryBuy(booster))
        {
            Refresh();
            ShowButtonFeedback(index, "BOUGHT!", new Color(0.18f, 0.72f, 0.32f, 1f));
            return;
        }

        int missing = Mathf.Max(0, ShopManager.Instance.GetCost(booster) - RunProgress.DataPack);
        string message = ShopManager.Instance.IsMaxed(booster) ? "MAX" : $"-{missing} DP";
        ShowButtonFeedback(index, message, new Color(0.86f, 0.24f, 0.2f, 1f));
    }

    private void OnNextStageClicked()
    {
        if (isLeavingShop)
        {
            return;
        }

        RunProgress.AdvanceStage();
        LeaveShopAndLoadScene("GameScene");
    }

    private void OnMainMenuClicked()
    {
        if (isLeavingShop)
        {
            return;
        }

        RunProgress.LoadSavedRun();
        LeaveShopAndLoadScene("MainMenuScene");
    }

    private void LeaveShopAndLoadScene(string sceneName)
    {
        isLeavingShop = true;
        Time.timeScale = 1f;

        SetShopButtonsInteractable(false);
        ModalContainer container = ModalContainer.Of(transform, false);
        SceneTransition.LoadSceneAfterClosingModal(sceneName, container);
    }

    private void SetShopButtonsInteractable(bool interactable)
    {
        if (buyButtons != null)
        {
            for (int i = 0; i < buyButtons.Length; i++)
            {
                if (buyButtons[i] != null)
                {
                    buyButtons[i].interactable = interactable;
                }
            }
        }

        if (nextStageButton != null)
        {
            nextStageButton.interactable = interactable;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = interactable;
        }
    }

    private static void EnsureShopManager()
    {
        if (ShopManager.Instance != null)
        {
            return;
        }

        new GameObject("ShopManager").AddComponent<ShopManager>();
    }

    private void EnsureRuntimeLayout()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
        {
            root = gameObject.AddComponent<RectTransform>();
        }

        if (dataPackText != null && stageText != null && buyButtons != null && buyButtons.Length >= 4 && buyButtons[0] != null)
        {
            EnsureActionButtons(root);
            return;
        }

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        RectTransform content = CreateRect("Content", root);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(820f, 560f);
        content.anchoredPosition = Vector2.zero;

        Image contentImage = content.gameObject.AddComponent<Image>();
        contentImage.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);

        TMP_Text title = CreateText("Title", content, "BUILD COMPLETE", 38f, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-48f, 60f));

        dataPackText = CreateText("DataPackText", content, string.Empty, 24f, TextAlignmentOptions.Left);
        SetRect(dataPackText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(34f, -108f), new Vector2(-20f, 48f));

        stageText = CreateText("StageText", content, string.Empty, 20f, TextAlignmentOptions.Right);
        SetRect(stageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(20f, -124f), new Vector2(-34f, 82f));

        buyButtons = new Button[4];
        titleTexts = new TMP_Text[4];
        descriptionTexts = new TMP_Text[4];
        costTexts = new TMP_Text[4];

        for (int i = 0; i < buyButtons.Length; i++)
        {
            RectTransform row = CreateRect($"BoosterRow{i + 1}", content);
            SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -188f - i * 72f), new Vector2(-34f, 58f));
            row.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.14f, 0.22f, 0.95f);

            titleTexts[i] = CreateText("Title", row, string.Empty, 20f, TextAlignmentOptions.Left);
            SetRect(titleTexts[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0.35f, 1f), new Vector2(16f, -4f), new Vector2(-8f, -4f));

            descriptionTexts[i] = CreateText("Description", row, string.Empty, 16f, TextAlignmentOptions.Left);
            SetRect(descriptionTexts[i].rectTransform, new Vector2(0.35f, 0f), new Vector2(0.74f, 1f), new Vector2(8f, 6f), new Vector2(-8f, -6f));

            costTexts[i] = CreateText("Cost", row, string.Empty, 17f, TextAlignmentOptions.Center);
            SetRect(costTexts[i].rectTransform, new Vector2(0.74f, 0f), new Vector2(0.85f, 1f), new Vector2(4f, 4f), new Vector2(-4f, -4f));

            buyButtons[i] = CreateButton("BuyButton", row, "BUY");
            SetRect((RectTransform)buyButtons[i].transform, new Vector2(0.86f, 0.16f), new Vector2(1f, 0.84f), new Vector2(0f, 0f), new Vector2(-12f, 0f));
        }

        nextStageButton = CreateButton("NextStageButton", content, "NEXT STAGE");
        SetRect((RectTransform)nextStageButton.transform, new Vector2(0.52f, 0f), new Vector2(0.78f, 0f), new Vector2(0f, 32f), new Vector2(0f, 56f));

        mainMenuButton = CreateButton("MainMenuButton", content, "MAIN MENU");
        SetRect((RectTransform)mainMenuButton.transform, new Vector2(0.22f, 0f), new Vector2(0.48f, 0f), new Vector2(0f, 32f), new Vector2(0f, 56f));
    }

    private void EnsureActionButtons(RectTransform parent)
    {
        if (nextStageButton == null)
        {
            nextStageButton = CreateButton("NextStageButton", parent, "NEXT STAGE");
            SetRect((RectTransform)nextStageButton.transform, new Vector2(0.52f, 0f), new Vector2(0.78f, 0f), new Vector2(0f, 32f), new Vector2(0f, 56f));
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = CreateButton("MainMenuButton", parent, "MAIN MENU");
            SetRect((RectTransform)mainMenuButton.transform, new Vector2(0.22f, 0f), new Vector2(0.48f, 0f), new Vector2(0f, 32f), new Vector2(0f, 56f));
        }
    }

    private void AutoBindExistingReferences()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            string objectName = button.name.ToLowerInvariant();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string labelText = label != null ? label.text.ToLowerInvariant() : string.Empty;

            if (nextStageButton == null && (objectName.Contains("next") || labelText.Contains("next")))
            {
                nextStageButton = button;
            }

            if (mainMenuButton == null && (objectName.Contains("mainmenu") || objectName.Contains("main menu") || labelText.Contains("main menu")))
            {
                mainMenuButton = button;
            }
        }
    }

    private void AutoBindBuyButtons()
    {
        bool needsBind = buyButtons == null || buyButtons.Length < 4;
        if (!needsBind)
        {
            for (int i = 0; i < buyButtons.Length; i++)
            {
                if (buyButtons[i] == null)
                {
                    needsBind = true;
                    break;
                }
            }
        }

        if (!needsBind)
        {
            return;
        }

        Button[] allButtons = GetComponentsInChildren<Button>(true);
        buyButtons = new Button[4];
        int count = 0;

        for (int i = 0; i < allButtons.Length && count < buyButtons.Length; i++)
        {
            Button button = allButtons[i];
            if (button == null || button == nextStageButton || button == mainMenuButton)
            {
                continue;
            }

            string objectName = button.name.ToLowerInvariant();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string labelText = label != null ? label.text.ToLowerInvariant() : string.Empty;

            if (objectName.Contains("next") || objectName.Contains("menu") || labelText.Contains("next") || labelText.Contains("menu"))
            {
                continue;
            }

            buyButtons[count] = button;
            count++;
        }
    }

    private string GetBuyButtonLabel(ShopBoosterType booster)
    {
        if (ShopManager.Instance.IsMaxed(booster))
        {
            return "MAX";
        }

        return ShopManager.Instance.CanBuy(booster) ? "BUY" : "NEED DP";
    }

    private void ShowButtonFeedback(int index, string label, Color color)
    {
        if (index < 0 || buyButtons == null || index >= buyButtons.Length || buyButtons[index] == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        Button button = buyButtons[index];
        SetButtonLabel(button, label);
        SetButtonColor(button, color);
        feedbackRoutine = StartCoroutine(ClearButtonFeedback());
    }

    private IEnumerator ClearButtonFeedback()
    {
        yield return new WaitForSecondsRealtime(0.6f);
        feedbackRoutine = null;
        Refresh();
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = color;
        }
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.13f, 0.42f, 0.9f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText("Text", rect, label, 18f, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
