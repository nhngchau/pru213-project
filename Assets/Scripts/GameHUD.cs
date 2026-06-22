using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top-screen HUD (GDD v3.0 - Section VII): the Build Progress bar with the current wave number
/// nested in its label, plus the DataPack counter. Pure presentation - listens to GameEvents only.
/// Lives on the always-active Canvas.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Build Progress (top center)")]
    [SerializeField] private Image buildProgressFill;   // Image Type = Filled, Horizontal
    [SerializeField] private TMP_Text buildProgressLabel;

    [Header("DataPack (top right)")]
    [SerializeField] private TMP_Text dataPackText;

    private int currentWave = 1;
    private float buildPercent;

    void OnEnable()
    {
        GameEvents.OnBuildProgressChanged += HandleProgress;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnDataPackChanged += HandleDataPack;
    }

    void OnDisable()
    {
        GameEvents.OnBuildProgressChanged -= HandleProgress;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnDataPackChanged -= HandleDataPack;
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
}
