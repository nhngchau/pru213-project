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
            RaiseExpChanged();
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
        GameEvents.RaiseExpChanged(currentExp, RequiredExp, level);
    }
}
