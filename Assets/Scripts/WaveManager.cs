using UnityEngine;

/// <summary>
/// Owns the win timeline (GDD v3.0 - Section II): survive buildDuration seconds for Build Progress
/// to reach 100%, split into waves. At the end of each non-final wave it freezes the game
/// (Time.timeScale = 0) and raises OnWaveEnded so the Upgrade Panel can appear; the final wave
/// triggers the win. Pure gameplay - it only raises GameEvents and calls GameManager.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Timeline (GDD v3.0)")]
    [SerializeField] private float buildDuration = 180f; // total survive time => 100% Build
    [Min(1)]
    [SerializeField] private int numberOfWaves = 6;

    private float elapsed;
    private int wavesCompleted;
    private bool inUpgradeBreak;

    public int CurrentWave => Mathf.Min(wavesCompleted + 1, numberOfWaves);
    private float WaveDuration => numberOfWaves > 0 ? buildDuration / numberOfWaves : buildDuration;

    void OnEnable() => GameEvents.OnContinueRequested += StartNextWave;
    void OnDisable() => GameEvents.OnContinueRequested -= StartNextWave;

    void Start()
    {
        elapsed = 0f;
        wavesCompleted = 0;
        inUpgradeBreak = false;
        Time.timeScale = 1f;

        GameEvents.RaiseWaveStarted(CurrentWave);
        GameEvents.RaiseBuildProgressChanged(0f);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        if (inUpgradeBreak)
        {
            return;
        }

        // Time.deltaTime is 0 while paused, so the timeline naturally freezes during the break.
        elapsed += Time.deltaTime;
        GameEvents.RaiseBuildProgressChanged(Mathf.Clamp01(elapsed / buildDuration) * 100f);

        if (elapsed >= (wavesCompleted + 1) * WaveDuration)
        {
            wavesCompleted++;

            if (wavesCompleted >= numberOfWaves)
            {
                GameEvents.RaiseBuildProgressChanged(100f);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerWin();
                }
            }
            else
            {
                BeginUpgradeBreak();
            }
        }
    }

    private void BeginUpgradeBreak()
    {
        inUpgradeBreak = true;
        Time.timeScale = 0f;                       // freeze enemies, bullets, spawner
        GameEvents.RaiseWaveEnded(wavesCompleted); // UpgradePanelUI opens the panel
    }

    /// <summary>Resumes play for the next wave. Triggered by GameEvents.OnContinueRequested.</summary>
    private void StartNextWave()
    {
        if (!inUpgradeBreak)
        {
            return;
        }

        inUpgradeBreak = false;
        Time.timeScale = 1f;
        GameEvents.RaiseWaveStarted(CurrentWave);
    }
}
