using UnityEngine;

/// <summary>The three upgrade tracks (GDD v3.0 - Section VI).</summary>
public enum UpgradeType
{
    OverclockCPU, // damage
    UpgradeRAM,   // fire rate
    Firewall,     // server HP
    DoubleShot,   // more bullets per shot
    Explosive,    // bullets explode on impact, dealing AoE damage
    PiercingBeam  // bullets pierce enemies
}

/// <summary>
/// Runtime power-up upgrades. DataPack is still collected as currency, but combat power-ups are
/// granted by level-up choices and do not spend DataPack.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Component refs (auto-found if empty)")]
    [SerializeField] private PlayerShooting playerShooting;
    [SerializeField] private ServerCore serverCore;

    [Header("Currency")]
    [SerializeField] private int startingDataPack = 0;

    [Header("Upgrade Values")]
    [SerializeField] private int damagePerCpuLevel = 5;
    [SerializeField] private float fireRateReductionPerRamLevel = 0.05f;
    [SerializeField] private float minFireRate = 0.1f;
    [SerializeField] private int firewallHpPerLevel = 100;
    [SerializeField] private int bulletsPerDoubleShotLevel = 1;
    [SerializeField] private float explosionRadiusPerLevel = 1.25f;
    [SerializeField] private int piercesPerBeamLevel = 1;

    private int baseDamage = 10;
    private float baseFireRate = 0.15f;
    private int baseBulletsPerShot = 1;
    private float baseExplosionRadius = 0f;
    private int baseBulletPierces;

    private int cpuLevel;
    private int ramLevel;
    private int firewallLevel;
    private int doubleShotLevel;
    private int explosiveLevel;
    private int piercingBeamLevel;

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
        CacheBasePlayerStats();
        LoadPersistedPowerUps();
        ApplyPersistedPowerUps();
        DataPack = RunProgress.DataPack + startingDataPack;
        RunProgress.SetDataPack(DataPack);
    }

    private void CacheBasePlayerStats()
    {
        if (playerShooting == null)
        {
            return;
        }

        baseDamage = playerShooting.BulletDamage;
        baseFireRate = playerShooting.FireRate;
        baseBulletsPerShot = playerShooting.BulletsPerShot;
        baseExplosionRadius = playerShooting.ExplosionRadius;
        baseBulletPierces = playerShooting.BulletPierces;
    }

    private void AddDataPack(int amount)
    {
        RunProgress.AddDataPack(amount);
        DataPack = RunProgress.DataPack;
    }

    // --- Queries used by the UI ------------------------------------------

    public bool IsMaxed(UpgradeType type) => type switch
    {
        UpgradeType.UpgradeRAM => Mathf.Approximately(GetCurrentFireRate(), minFireRate),
        _ => false,
    };

    public bool CanChoose(UpgradeType type) => !IsMaxed(type);

    public string GetDescription(UpgradeType type) => type switch
    {
        UpgradeType.OverclockCPU => IsMaxed(type)
            ? "MAX LEVEL"
            : $"Damage {GetCurrentDamage()} -> {GetNextDamage()}  (Lv {cpuLevel + 1})",
        UpgradeType.UpgradeRAM => IsMaxed(type)
            ? "MAX LEVEL"
            : $"Fire Rate {GetCurrentFireRate():0.00}s -> {GetNextFireRate():0.00}s  (Lv {ramLevel + 1})",
        UpgradeType.Firewall => $"Server +{firewallHpPerLevel} HP",
        UpgradeType.DoubleShot => IsMaxed(type)
            ? "MAX LEVEL"
            : $"Bullets {GetCurrentBulletsPerShot()} -> {GetNextBulletsPerShot()}  (Lv {doubleShotLevel + 1})",
        UpgradeType.Explosive => IsMaxed(type)
            ? "MAX LEVEL"
            : $"Explosion Radius {GetCurrentExplosionRadius():0.0}m -> {GetNextExplosionRadius():0.0}m  (Lv {explosiveLevel + 1})",
        UpgradeType.PiercingBeam => IsMaxed(type)
            ? "MAX LEVEL"
            : $"Pierce {GetCurrentPierces()} -> {GetNextPierces()}  (Lv {piercingBeamLevel + 1})",
        _ => string.Empty,
    };

    public string GetTitle(UpgradeType type) => type switch
    {
        UpgradeType.OverclockCPU => "Overclock CPU",
        UpgradeType.UpgradeRAM => "Upgrade RAM",
        UpgradeType.Firewall => "Firewall",
        UpgradeType.DoubleShot => "Double Shot",
        UpgradeType.Explosive => "Explosive Bullet",
        UpgradeType.PiercingBeam => "Piercing Beam",
        _ => string.Empty,
    };

    public UpgradeType[] GetRandomPowerUpOptions(int count)
    {
        UpgradeType[] pool =
        {
            UpgradeType.OverclockCPU,
            UpgradeType.UpgradeRAM,
            UpgradeType.Firewall,
            UpgradeType.DoubleShot,
            UpgradeType.Explosive,
            UpgradeType.PiercingBeam,
        };

        int targetCount = Mathf.Max(1, count);
        UpgradeType[] options = new UpgradeType[targetCount];
        int[] indices = new int[pool.Length];

        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        for (int i = 0; i < indices.Length; i++)
        {
            int swapIndex = Random.Range(i, indices.Length);
            (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
        }

        int optionIndex = 0;
        for (int i = 0; i < indices.Length && optionIndex < targetCount; i++)
        {
            UpgradeType type = pool[indices[i]];
            if (CanChoose(type))
            {
                options[optionIndex] = type;
                optionIndex++;
            }
        }

        while (optionIndex < targetCount)
        {
            options[optionIndex] = UpgradeType.Firewall;
            optionIndex++;
        }

        return options;
    }

    // --- Purchase --------------------------------------------------------

    public bool TryApplyPowerUp(UpgradeType type)
    {
        if (!CanChoose(type))
        {
            return false;
        }

        switch (type)
        {
            case UpgradeType.OverclockCPU:
                RunProgress.AddPowerUpLevel(type);
                cpuLevel = RunProgress.PowerUpCpuLevel;
                // GDD: bulletDamage = baseDamage + (upgradeLevel * 5)
                playerShooting?.SetBulletDamage(GetCurrentDamage());
                break;

            case UpgradeType.UpgradeRAM:
                RunProgress.AddPowerUpLevel(type);
                ramLevel = RunProgress.PowerUpRamLevel;
                // GDD: fireRate = baseFireRate - (upgradeLevel * 0.05f), clamped at 0.1s
                playerShooting?.SetFireRate(GetCurrentFireRate());
                break;

            case UpgradeType.Firewall:
                RunProgress.AddPowerUpLevel(type);
                firewallLevel = RunProgress.PowerUpFirewallLevel;
                // GDD: serverMaxHP += 100; serverCurrentHP += 100;
                serverCore?.IncreaseMaxHP(firewallHpPerLevel);
                break;

            case UpgradeType.DoubleShot:
                RunProgress.AddPowerUpLevel(type);
                doubleShotLevel = RunProgress.PowerUpDoubleShotLevel;
                playerShooting?.SetBulletsPerShot(GetCurrentBulletsPerShot());
                break;

            case UpgradeType.Explosive:
                RunProgress.AddPowerUpLevel(type);
                explosiveLevel = RunProgress.PowerUpExplosiveLevel;
                playerShooting?.SetExplosionRadius(GetCurrentExplosionRadius());
                break;

            case UpgradeType.PiercingBeam:
                RunProgress.AddPowerUpLevel(type);
                piercingBeamLevel = RunProgress.PowerUpPiercingBeamLevel;
                playerShooting?.SetBulletPierces(GetCurrentPierces());
                break;
        }

        GameEvents.RaiseUpgradePurchased();
        return true;
    }

    private int GetCurrentDamage() => baseDamage + cpuLevel * damagePerCpuLevel;
    private int GetNextDamage() => baseDamage + (cpuLevel + 1) * damagePerCpuLevel;
    private int GetCurrentBulletsPerShot() => baseBulletsPerShot + doubleShotLevel * bulletsPerDoubleShotLevel;
    private int GetNextBulletsPerShot() => baseBulletsPerShot + (doubleShotLevel + 1) * bulletsPerDoubleShotLevel;
    private float GetCurrentExplosionRadius() => baseExplosionRadius + explosiveLevel * explosionRadiusPerLevel;
    private float GetNextExplosionRadius() => baseExplosionRadius + (explosiveLevel + 1) * explosionRadiusPerLevel;
    private int GetCurrentPierces() => baseBulletPierces + piercingBeamLevel * piercesPerBeamLevel;
    private int GetNextPierces() => baseBulletPierces + (piercingBeamLevel + 1) * piercesPerBeamLevel;

    private float GetCurrentFireRate()
    {
        return Mathf.Max(minFireRate, baseFireRate - ramLevel * fireRateReductionPerRamLevel);
    }

    private float GetNextFireRate()
    {
        return Mathf.Max(minFireRate, baseFireRate - (ramLevel + 1) * fireRateReductionPerRamLevel);
    }

    private void LoadPersistedPowerUps()
    {
        cpuLevel = RunProgress.PowerUpCpuLevel;
        ramLevel = RunProgress.PowerUpRamLevel;
        firewallLevel = RunProgress.PowerUpFirewallLevel;
        doubleShotLevel = RunProgress.PowerUpDoubleShotLevel;
        explosiveLevel = RunProgress.PowerUpExplosiveLevel;
        piercingBeamLevel = RunProgress.PowerUpPiercingBeamLevel;
    }

    private void ApplyPersistedPowerUps()
    {
        playerShooting?.SetBulletDamage(GetCurrentDamage());
        playerShooting?.SetFireRate(GetCurrentFireRate());
        playerShooting?.SetBulletsPerShot(GetCurrentBulletsPerShot());
        playerShooting?.SetExplosionRadius(GetCurrentExplosionRadius());
        playerShooting?.SetBulletPierces(GetCurrentPierces());

        if (firewallLevel > 0)
        {
            serverCore?.IncreaseMaxHP(firewallLevel * firewallHpPerLevel);
        }
    }
}
