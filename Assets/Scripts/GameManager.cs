using UnityEngine;

/// <summary>
/// Game end-state owner (GDD v3.0): win / lose flags. The Build Progress
/// timeline now lives in WaveManager, which calls TriggerWin() on the final wave; ServerCore
/// calls TriggerGameOver() when the Server dies.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;

    public bool IsGameOver { get; private set; }
    public bool IsGameWon { get; private set; }
    public bool IsGameEnded => IsGameOver || IsGameWon;
    public bool IsPaused { get; private set; }

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
        PlayerProgression.EnsureExistsForCurrentScene();
        Time.timeScale = 1f;
        IsGameOver = false;
        IsGameWon = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    public void TriggerGameOver()
    {
        if (IsGameEnded)
        {
            return;
        }

        IsGameOver = true;
        GameAudioManager.Instance?.PlayGameOver();
        Time.timeScale = 0f;
        GameEvents.RaiseGameOver();

        if (!GameUIManager.HasInstance && gameOverPanel != null)
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
        GameAudioManager.Instance?.PlayWin();
        Time.timeScale = 0f;
        GameEvents.RaiseGameWon();

        if (!GameUIManager.HasInstance && winPanel != null)
        {
            winPanel.SetActive(true);
        }

        Debug.Log("Build Complete! You Win!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsGameEnded) return;

        // Modal chọn power-up cũng đang giữ Time.timeScale = 0. Cho phép pause lúc đó thì lần bấm
        // ESC tiếp theo sẽ trả timeScale về 1 và game chạy tiếp NGAY DƯỚI modal đang mở.
        if (PlayerProgression.Instance != null && PlayerProgression.Instance.WaitingForPowerUpChoice)
        {
            return;
        }

        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        GameEvents.RaiseGamePaused(IsPaused);
    }
}
