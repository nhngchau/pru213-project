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
    [SerializeField] private Image playerHPFill;        // Image Type = Filled, Horizontal (green)
    [SerializeField] private TMP_Text playerHPLabel;

    [Header("Build Progress (top center)")]
    [SerializeField] private Image buildProgressFill;   // Image Type = Filled, Horizontal
    [SerializeField] private TMP_Text buildProgressLabel;

    [Header("DataPack (top right)")]
    [SerializeField] private TMP_Text dataPackText;

    [Header("EXP / Level (optional)")]
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text expLabel;

    private int currentWave = 1;
    private float buildPercent;

    void OnEnable()
    {
        GameEvents.OnBuildProgressChanged += HandleProgress;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnDataPackChanged += HandleDataPack;
        GameEvents.OnExpChanged += HandleExp;
        PlayerEvents.OnPlayerHealthChanged += HandlePlayerHP;
    }

    void OnDisable()
    {
        GameEvents.OnBuildProgressChanged -= HandleProgress;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnDataPackChanged -= HandleDataPack;
        GameEvents.OnExpChanged -= HandleExp;
        PlayerEvents.OnPlayerHealthChanged -= HandlePlayerHP;
    }

    // GDD: Player HP bar (green, top-left).
    private void HandlePlayerHP(int current, int max)
    {
        if (playerHPFill != null)
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
        if (buildProgressFill != null)
        {
            buildProgressFill.fillAmount = percent / 100f;
        }
        UpdateLabel();
    }

    private void HandleWaveStarted(int wave)
    {
        currentWave = wave;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (buildProgressLabel != null)
        {
            buildProgressLabel.text = $"WAVE {currentWave}   -   BUILD {Mathf.RoundToInt(buildPercent)}%";
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
        if (expFill != null)
        {
            expFill.fillAmount = required > 0 ? (float)current / required : 0f;
        }

        if (expLabel != null)
        {
            expLabel.text = $"LV {level}  EXP {current}/{required}";
        }
    }
}
