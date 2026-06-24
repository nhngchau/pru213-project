using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the between-wave Upgrade Panel (GDD v3.0 - Section VI). Pure presentation: it listens to
/// GameEvents to show/refresh and calls UpgradeManager for queries/purchases. A button goes
/// disabled (interactable = false) when the player cannot afford it or the track is maxed.
/// Lives on the always-active Canvas; it toggles a separate panelRoot child.
/// </summary>
public class UpgradePanelUI : MonoBehaviour
{
    [Header("Root (the panel object this script toggles)")]
    [SerializeField] private GameObject panelRoot;

    [Header("Upgrade columns (order: CPU, RAM, Firewall)")]
    [SerializeField] private Button[] upgradeButtons = new Button[3];
    [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[3];
    [SerializeField] private TMP_Text[] costTexts = new TMP_Text[3];

    [Header("Continue")]
    [SerializeField] private Button continueButton;

    // Column index -> upgrade track.
    private static readonly UpgradeType[] Types =
    {
        UpgradeType.OverclockCPU, UpgradeType.UpgradeRAM, UpgradeType.Firewall
    };

    void Awake()
    {
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            int index = i; // capture per button
            if (upgradeButtons[i] != null)
            {
                upgradeButtons[i].onClick.AddListener(() => OnUpgradeClicked(index));
            }
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    void OnEnable()
    {
        GameEvents.OnWaveEnded += HandleWaveEnded;
        GameEvents.OnDataPackChanged += HandleDataPackChanged;
        GameEvents.OnUpgradePurchased += Refresh;
    }

    void OnDisable()
    {
        GameEvents.OnWaveEnded -= HandleWaveEnded;
        GameEvents.OnDataPackChanged -= HandleDataPackChanged;
        GameEvents.OnUpgradePurchased -= Refresh;
    }

    void Start()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false); // hidden until a wave ends
        }
    }

    private void HandleWaveEnded(int wave)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        Refresh();
    }

    private void HandleDataPackChanged(int _) => Refresh();

    private void Refresh()
    {
        UpgradeManager mgr = UpgradeManager.Instance;
        if (mgr == null)
        {
            return;
        }

        for (int i = 0; i < Types.Length; i++)
        {
            UpgradeType type = Types[i];

            if (descriptionTexts[i] != null)
            {
                descriptionTexts[i].text = mgr.GetDescription(type);
            }

            if (costTexts[i] != null)
            {
                costTexts[i].text = mgr.IsMaxed(type) ? "-" : $"Cost: {mgr.GetCost(type)} DP";
            }

            if (upgradeButtons[i] != null)
            {
                // GDD: not enough DataPack (or maxed) => not clickable.
                upgradeButtons[i].interactable = mgr.CanAfford(type);
            }
        }
    }

    private void OnUpgradeClicked(int index)
    {
        // Refresh is driven by OnUpgradePurchased / OnDataPackChanged.
        UpgradeManager.Instance?.TryPurchase(Types[index]);
    }

    private void OnContinueClicked()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        GameEvents.RaiseContinueRequested(); // WaveManager resumes + starts the next wave
    }
}
