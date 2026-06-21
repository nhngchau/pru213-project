using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Build Progress")]
    // GDD v3.0 - Win Condition: survive exactly 180s (3 min) for BUILD PROGRESS to reach 100%.
    [SerializeField] private float buildDuration = 180f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;

    public bool IsGameOver { get; private set; }
    public bool IsGameWon { get; private set; }
    public bool IsGameEnded => IsGameOver || IsGameWon;
    public float BuildProgressPercent { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        IsGameWon = false;
        BuildProgressPercent = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (IsGameEnded)
        {
            return;
        }

        IncreaseBuildProgress();
    }

    private void IncreaseBuildProgress()
    {
        if (buildDuration <= 0f)
        {
            BuildProgressPercent = 100f;
        }
        else
        {
            BuildProgressPercent += (Time.deltaTime / buildDuration) * 100f;
            BuildProgressPercent = Mathf.Clamp(BuildProgressPercent, 0f, 100f);
        }

        if (BuildProgressPercent >= 100f)
        {
            TriggerWin();
        }
    }

    public void TriggerGameOver()
    {
        if (IsGameEnded)
        {
            return;
        }

        IsGameOver = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // GDD: Lose Condition shows the "System Crashed" panel (gameOverPanel).
        Debug.Log("System Crashed! The central server has been destroyed.");
    }

    public void TriggerWin()
    {
        if (IsGameEnded)
        {
            return;
        }

        IsGameWon = true;
        Time.timeScale = 0f;

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        // TODO (next task): GDD requires disabling all Spawners and remaining Bugs on win.
        // Deferred until spawner/enemy registry wiring is added so it stays a single-purpose commit.
        Debug.Log("Build Complete! You Win!");
    }
}
