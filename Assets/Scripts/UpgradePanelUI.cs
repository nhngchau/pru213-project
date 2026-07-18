using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityScreenNavigator.Runtime.Core.Modal;
using System;

/// <summary>
/// Level-up power-up panel. It shows three random runtime power-ups when the player gains a level.
/// </summary>
public class UpgradePanelUI : Modal
{
    [Header("Root (the panel object this script toggles)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool showOnLevelUpReady = true;
    [SerializeField] private bool hideRootOnStart = true;
    [SerializeField] private bool closeNavigatorModalOnContinue = true;

    [Header("Level Info")]
    [SerializeField] private TMP_Text levelText;

    [Header("Upgrade Choices")]
    [SerializeField] private bool autoBindButtonTexts = true;
    [SerializeField] private Button[] upgradeButtons = new Button[3];
    [SerializeField] private TMP_Text[] titleTexts = new TMP_Text[3];       // upgrade name
    [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[3];
    [SerializeField] private TMP_Text[] costTexts = new TMP_Text[3];        // status text, now "LEVEL UP"

    [Header("Continue")]
    [SerializeField] private Button continueButton;

    private readonly UpgradeType[] currentOptions = new UpgradeType[3];
    private bool hasRolledOptions;
    private int displayedLevel = 1;

    void Awake()
    {
        AutoBindMissingTexts();

        for (int i = 0; i < currentOptions.Length; i++)
        {
            int index = i; // capture per button
            Button button = GetItem(upgradeButtons, i);
            if (button != null)
            {
                button.onClick.AddListener(() => OnUpgradeClicked(index));
            }
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        GameEvents.OnLevelUpReady += HandleLevelUpReady;
    }

    void OnDisable()
    {
        GameEvents.OnLevelUpReady -= HandleLevelUpReady;
    }

    void Start()
    {
        if (panelRoot != null && hideRootOnStart)
        {
            panelRoot.SetActive(false); // hidden until a wave ends
        }

        RollOptions();
        Refresh();
    }

    public override void DidPushEnter()
    {
        displayedLevel = PlayerProgression.Instance != null ? PlayerProgression.Instance.Level : displayedLevel;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        RollOptions();
        Refresh();
    }

    private void HandleLevelUpReady(int level)
    {
        displayedLevel = level;

        if (GameUIManager.UsesNavigatorUpgradeModal)
        {
            RollOptions();
            Refresh();
            return;
        }

        if (panelRoot != null && showOnLevelUpReady)
        {
            panelRoot.SetActive(true);
        }

        RollOptions();
        Refresh();
    }

    private void RollOptions()
    {
        UpgradeManager mgr = UpgradeManager.Instance;
        if (mgr == null)
        {
            return;
        }

        UpgradeType[] options = mgr.GetRandomPowerUpOptions(currentOptions.Length);
        for (int i = 0; i < currentOptions.Length; i++)
        {
            currentOptions[i] = options[i];
        }

        hasRolledOptions = true;
    }

    private void Refresh()
    {
        UpgradeManager mgr = UpgradeManager.Instance;
        if (mgr == null)
        {
            return;
        }

        if (levelText != null)
        {
            levelText.text = $"LEVEL {displayedLevel}";
        }

        if (!hasRolledOptions)
        {
            RollOptions();
        }

        for (int i = 0; i < currentOptions.Length; i++)
        {
            UpgradeType type = currentOptions[i];

            TMP_Text titleText = GetItem(titleTexts, i);
            TMP_Text descriptionText = GetItem(descriptionTexts, i);
            TMP_Text costText = GetItem(costTexts, i);
            Button upgradeButton = GetItem(upgradeButtons, i);

            if (titleText != null)
            {
                titleText.text = mgr.GetTitle(type);
            }

            if (descriptionText != null)
            {
                descriptionText.text = mgr.GetDescription(type);
            }

            if (costText != null)
            {
                costText.text = mgr.IsMaxed(type) ? "MAX" : "LEVEL UP";
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable = mgr.CanChoose(type);
            }
        }
    }

    private void OnUpgradeClicked(int index)
    {
        // Refresh is driven by OnUpgradePurchased / OnDataPackChanged.
        if (index < 0 || index >= currentOptions.Length)
        {
            return;
        }

        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryApplyPowerUp(currentOptions[index]))
        {
            hasRolledOptions = false;
            ClosePanel();
        }
    }

    private void ClosePanel()
    {
        if (closeNavigatorModalOnContinue)
        {
            // Route the pop through GameUIManager's serialized queue so it can't
            // overlap another modal transition (which throws "already in transition").
            if (GameUIManager.HasInstance)
            {
                GameUIManager.Instance.CloseTopModal();
            }
            else
            {
                ModalContainer container = ModalContainer.Of(transform);
                if (container != null && !container.IsInTransition)
                {
                    container.Pop(true);
                }
            }
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private static T GetItem<T>(T[] items, int index) where T : class
    {
        return items != null && index >= 0 && index < items.Length ? items[index] : null;
    }

    private void AutoBindMissingTexts()
    {
        if (!autoBindButtonTexts)
        {
            return;
        }

        EnsureTextArraySizes();

        for (int i = 0; i < currentOptions.Length; i++)
        {
            Button button = GetItem(upgradeButtons, i);
            if (button == null)
            {
                continue;
            }

            if (titleTexts[i] == null)
            {
                titleTexts[i] = FindText(button.transform, "Title", "Name", "UpgradeName");
            }

            if (descriptionTexts[i] == null)
            {
                descriptionTexts[i] = FindText(button.transform, "Description", "Desc", "UpgradeDescription");
            }

            if (costTexts[i] == null)
            {
                costTexts[i] = FindText(button.transform, "Cost", "Status", "UpgradeStatus");
            }
        }

        if (levelText == null)
        {
            levelText = FindText(transform, "LevelText", "LevelLabel", "CurrentLevelText");
        }
    }

    private static TMP_Text FindText(Transform root, params string[] names)
    {
        foreach (string textName in names)
        {
            Transform found = FindChildRecursive(root, textName);
            if (found != null && found.TryGetComponent(out TMP_Text text))
            {
                return text;
            }
        }

        return null;
    }

    private void EnsureTextArraySizes()
    {
        EnsureArraySize(ref titleTexts, currentOptions.Length);
        EnsureArraySize(ref descriptionTexts, currentOptions.Length);
        EnsureArraySize(ref costTexts, currentOptions.Length);
    }

    private static void EnsureArraySize<T>(ref T[] array, int size)
    {
        if (array == null)
        {
            array = new T[size];
            return;
        }

        if (array.Length < size)
        {
            Array.Resize(ref array, size);
        }
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
}
