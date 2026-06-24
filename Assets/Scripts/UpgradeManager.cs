using UnityEngine;

/// <summary>The three upgrade tracks (GDD v3.0 - Section VI).</summary>
public enum UpgradeType
{
    OverclockCPU, // damage
    UpgradeRAM,   // fire rate
    Firewall      // server HP
}

/// <summary>
/// DataPack economy + stat upgrades (GDD v3.0 - Section VI). Owns the wallet and upgrade levels,
/// computes costs/effects and applies them to the relevant components. Pure gameplay/logic: the UI
/// talks to it only through the public query/purchase API and refreshes via GameEvents.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Component refs (auto-found if empty)")]
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private ServerCore serverCore;

    [Header("Economy")]
    [SerializeField] private int startingDataPack = 0;

    // GDD base values.
    private const int BaseDamage = 10;
    private const float BaseFireRate = 0.5f;
    private const float MinFireRate = 0.1f;

    // GDD costs: index = current level -> cost of the NEXT purchase. CPU/RAM cap at 3 levels.
    private static readonly int[] CpuCosts = { 50, 100, 150 };
    private static readonly int[] RamCosts = { 75, 125, 175 };
    private const int FirewallCost = 100;       // flat price, repeatable
    private const int FirewallHpPerLevel = 100;

    private int cpuLevel;
    private int ramLevel;
    private int firewallLevel;

    public int DataPack { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (playerShooting == null) playerShooting = FindFirstObjectByType<PlayerShooting>();
        if (serverCore == null) serverCore = FindFirstObjectByType<ServerCore>();
    }

    void OnEnable() => GameEvents.OnDataPackAwarded += AddDataPack;
    void OnDisable() => GameEvents.OnDataPackAwarded -= AddDataPack;

    void Start()
    {
        DataPack = startingDataPack;
        GameEvents.RaiseDataPackChanged(DataPack);
    }

    private void AddDataPack(int amount)
    {
        DataPack += Mathf.Max(0, amount);
        GameEvents.RaiseDataPackChanged(DataPack);
    }

    // --- Queries used by the UI ------------------------------------------

    public bool IsMaxed(UpgradeType type) => type switch
    {
        UpgradeType.OverclockCPU => cpuLevel >= CpuCosts.Length,
        UpgradeType.UpgradeRAM => ramLevel >= RamCosts.Length,
        _ => false, // Firewall is repeatable
    };

    public int GetCost(UpgradeType type) => type switch
    {
        UpgradeType.OverclockCPU => IsMaxed(type) ? 0 : CpuCosts[cpuLevel],
        UpgradeType.UpgradeRAM => IsMaxed(type) ? 0 : RamCosts[ramLevel],
        UpgradeType.Firewall => FirewallCost,
        _ => 0,
    };

    public bool CanAfford(UpgradeType type) => !IsMaxed(type) && DataPack >= GetCost(type);

    public string GetDescription(UpgradeType type) => type switch
    {
        UpgradeType.OverclockCPU => IsMaxed(type) ? "MAX LEVEL" : $"Damage +5  (Lv {cpuLevel + 1})",
        UpgradeType.UpgradeRAM => IsMaxed(type) ? "MAX LEVEL" : $"Fire Rate -0.05s  (Lv {ramLevel + 1})",
        UpgradeType.Firewall => $"Server +{FirewallHpPerLevel} HP",
        _ => string.Empty,
    };

    // --- Purchase --------------------------------------------------------

    public bool TryPurchase(UpgradeType type)
    {
        if (!CanAfford(type))
        {
            return false;
        }

        DataPack -= GetCost(type);

        switch (type)
        {
            case UpgradeType.OverclockCPU:
                cpuLevel++;
                // GDD: bulletDamage = baseDamage + (upgradeLevel * 5)
                playerShooting?.SetBulletDamage(BaseDamage + cpuLevel * 5);
                break;

            case UpgradeType.UpgradeRAM:
                ramLevel++;
                // GDD: fireRate = baseFireRate - (upgradeLevel * 0.05f), clamped at 0.1s
                playerShooting?.SetFireRate(Mathf.Max(MinFireRate, BaseFireRate - ramLevel * 0.05f));
                break;

            case UpgradeType.Firewall:
                firewallLevel++;
                // GDD: serverMaxHP += 100; serverCurrentHP += 100;
                serverCore?.IncreaseMaxHP(FirewallHpPerLevel);
                break;
        }

        GameEvents.RaiseDataPackChanged(DataPack);
        GameEvents.RaiseUpgradePurchased();
        return true;
    }
}
