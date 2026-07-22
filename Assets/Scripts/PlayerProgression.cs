using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("Leveling")]
    [SerializeField] private int startingLevel = 1;
    [Tooltip("EXP cần để lên cấp 2. Các cấp sau nhân dồn theo expGrowthMultiplier.")]
    [SerializeField] private int baseExpRequired = 150;
    [Tooltip("Mỗi cấp yêu cầu EXP gấp ngần này lần cấp trước. Lớn hơn 1 = lên cấp thưa dần.\n" +
             "Phải là cấp số nhân: số quái mỗi nhóm và tốc độ spawn đều tăng theo stage, nên " +
             "đường cong cộng tuyến tính sẽ bị lượng EXP kiếm được vượt mặt rất nhanh.\n" +
             "Lưu ý: EXP reset về cấp 1 mỗi stage (xem Start) nhưng level power-up thì giữ nguyên " +
             "cả run, nên đường cong này quyết định TỔNG số power-up người chơi gom được.")]
    [Min(1.05f)]
    [SerializeField] private float expGrowthMultiplier = 1.6f;

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
        level = Mathf.Max(1, startingLevel);
        currentExp = 0;
        RaiseExpChanged();
    }

    private void AddExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Cố ý KHÔNG log ở đây: hàm này chạy mỗi lần hạ một con quái (~130 lần/stage), mà Debug.Log
        // kèm stack trace mỗi lần gọi. Thanh EXP trên HUD đã hiện đúng current/required rồi.
        currentExp += amount;

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
        int steps = Mathf.Max(0, targetLevel - 1);
        return Mathf.Max(1, Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expGrowthMultiplier, steps)));
    }

    private void RaiseExpChanged()
    {
        GameEvents.RaiseExpChanged(currentExp, RequiredExp, level);
    }
}
