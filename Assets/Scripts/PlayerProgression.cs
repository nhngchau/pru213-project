using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("Leveling")]
    [SerializeField] private int startingLevel = 1;
    [SerializeField] private int baseExpRequired = 30;
    [SerializeField] private int expRequiredGrowth = 20;

    private int level;
    private int currentExp;
    private bool waitingForPowerUpChoice;

    public int Level => level;
    public int CurrentExp => currentExp;
    public int RequiredExp => GetRequiredExp(level);
    public bool WaitingForPowerUpChoice => waitingForPowerUpChoice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInGameScene()
    {
        EnsureExistsForCurrentScene();
    }

    public static void EnsureExistsForCurrentScene()
    {
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            return;
        }

        if (Instance == null)
        {
            Instance = FindFirstObjectByType<PlayerProgression>();
        }

        if (Instance != null)
        {
            return;
        }

        GameObject progressionObject = new GameObject("PlayerProgression");
        progressionObject.AddComponent<PlayerProgression>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnExpAwarded += AddExp;
        GameEvents.OnUpgradePurchased += ResumeAfterPowerUp;
    }

    private void OnDisable()
    {
        GameEvents.OnExpAwarded -= AddExp;
        GameEvents.OnUpgradePurchased -= ResumeAfterPowerUp;
    }

    private void Start()
    {
        // Khôi phục level/EXP của run thay vì reset về 1 — sang stage mới vẫn giữ
        // nguyên tiến độ và ngưỡng lên cấp (RunProgress.ClearPowerUps mới reset về 1).
        level = Mathf.Max(Mathf.Max(1, startingLevel), RunProgress.PlayerLevel);
        currentExp = Mathf.Max(0, RunProgress.PlayerExp);
        RaiseExpChanged();
    }

    private void AddExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentExp += amount;
        Debug.Log($"PlayerProgression: +{amount} EXP ({currentExp}/{RequiredExp})");

        if (waitingForPowerUpChoice)
        {
            RaiseExpChanged();
            return;
        }

        if (currentExp >= RequiredExp)
        {
            currentExp -= RequiredExp;
            level++;
            waitingForPowerUpChoice = true;
            Time.timeScale = 0f;
            RunProgress.SetPlayerProgress(level, currentExp, persist: true); // ghi đĩa mỗi lần lên cấp
            RaiseExpChanged();
            GameAudioManager.Instance?.PlayLevelUp(); // sound khi lên cấp
            Debug.Log($"PlayerProgression: Level up to {level}");
            GameEvents.RaiseLevelUpReady(level);
            return;
        }

        RaiseExpChanged();
    }

    private void ResumeAfterPowerUp()
    {
        if (!waitingForPowerUpChoice)
        {
            return;
        }

        waitingForPowerUpChoice = false;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameEnded)
        {
            Time.timeScale = 1f;
        }
    }

    private int GetRequiredExp(int targetLevel)
    {
        return baseExpRequired + Mathf.Max(0, targetLevel - 1) * expRequiredGrowth;
    }

    private void RaiseExpChanged()
    {
        RunProgress.SetPlayerProgress(level, currentExp); // giữ trong bộ nhớ, không ghi đĩa mỗi lần giết quái
        GameEvents.RaiseExpChanged(currentExp, RequiredExp, level);
    }
}
