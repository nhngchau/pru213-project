using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityScreenNavigator.Runtime.Core.Modal;

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

    private void Awake()
    {
        EnsureShopManager();
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
            stageText.text = $"Next Stage: {RunProgress.Stage + 1}";
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
                costTexts[i].text = $"{ShopManager.Instance.GetCost(booster)} DP";
            }

            if (buyButtons[i] != null)
            {
                buyButtons[i].interactable = ShopManager.Instance.CanBuy(booster);
            }
        }
    }

    private void OnBuyClicked(int index)
    {
        if (boosters == null || index < 0 || index >= boosters.Length)
        {
            return;
        }

        if (ShopManager.Instance.TryBuy(boosters[index]))
        {
            Refresh();
        }
    }

    private void OnNextStageClicked()
    {
        RunProgress.AdvanceStage();
        Time.timeScale = 1f;
        SceneTransition.LoadScene("GameScene");
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene("MainMenuScene");
    }

    private static void EnsureShopManager()
    {
        if (ShopManager.Instance != null)
        {
            return;
        }

        new GameObject("ShopManager").AddComponent<ShopManager>();
    }
}
