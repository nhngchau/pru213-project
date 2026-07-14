using UnityEngine;

public enum ShopBoosterType
{
    StarterDamage,
    StarterFireRate,
    ServerArmor,
    ExtraBullets
}

public static class RunProgress
{
    private const string SaveResourcePath = "RunSaveData";
    private static RunSaveData saveDataCache;

    private const int DamageBonusPerLevel = 5;
    private const float FireRateReductionPerLevel = 0.02f;
    private const int ServerHpBonusPerLevel = 150;
    private const int ExtraBulletsPerLevel = 1;
    private const float EnemySpawnGrowthPerStage = 0.12f;

    public static int Stage { get; private set; } = 1;
    public static int DataPack { get; private set; }
    public static int StarterDamageLevel { get; private set; }
    public static int StarterFireRateLevel { get; private set; }
    public static int ServerArmorLevel { get; private set; }
    public static int ExtraBulletsLevel { get; private set; }
    public static int PowerUpCpuLevel { get; private set; }
    public static int PowerUpRamLevel { get; private set; }
    public static int PowerUpFirewallLevel { get; private set; }
    public static int PowerUpDoubleShotLevel { get; private set; }
    public static int PowerUpRicochetLevel { get; private set; }
    public static int PowerUpPiercingBeamLevel { get; private set; }
    public static bool HasSavedRun => GetSaveData().HasSave;

    public static int StarterDamageBonus => StarterDamageLevel * DamageBonusPerLevel;
    public static float StarterFireRateReduction => StarterFireRateLevel * FireRateReductionPerLevel;
    public static int ServerHpBonus => ServerArmorLevel * ServerHpBonusPerLevel;
    public static int ExtraBulletsBonus => ExtraBulletsLevel * ExtraBulletsPerLevel;

    public static int NextStage => Stage + 1;
    public static float EnemyHealthMultiplier => GetEnemyHealthMultiplier(Stage);
    public static float EnemyDamageMultiplier => GetEnemyDamageMultiplier(Stage);
    public static float EnemySpawnRateMultiplier => GetEnemySpawnRateMultiplier(Stage);
    public static int EnemyGroupBonus => GetEnemyGroupBonus(Stage);

    public static float GetEnemyHealthMultiplier(int stage)
    {
        return Mathf.Pow(2f, Mathf.Max(0, stage - 1));
    }

    public static float GetEnemyDamageMultiplier(int stage)
    {
        return Mathf.Pow(2f, Mathf.Max(0, stage - 1));
    }

    public static float GetEnemySpawnRateMultiplier(int stage)
    {
        return 1f + Mathf.Max(0, stage - 1) * EnemySpawnGrowthPerStage;
    }

    public static int GetEnemyGroupBonus(int stage)
    {
        return Mathf.FloorToInt(Mathf.Max(0, stage - 1) * 0.75f);
    }

    public static string GetStageDifficultySummary(int stage)
    {
        return $"Enemy HP x{GetEnemyHealthMultiplier(stage):0.00} | Damage x{GetEnemyDamageMultiplier(stage):0.00} | Spawn x{GetEnemySpawnRateMultiplier(stage):0.00}";
    }

    public static void SetDataPack(int amount)
    {
        DataPack = Mathf.Max(0, amount);
        SaveRun();
        GameEvents.RaiseDataPackChanged(DataPack);
    }

    public static void AddDataPack(int amount)
    {
        SetDataPack(DataPack + Mathf.Max(0, amount));
    }

    public static bool SpendDataPack(int amount)
    {
        int cost = Mathf.Max(0, amount);
        if (DataPack < cost)
        {
            return false;
        }

        SetDataPack(DataPack - cost);
        return true;
    }

    public static int GetBoosterLevel(ShopBoosterType type) => type switch
    {
        ShopBoosterType.StarterDamage => StarterDamageLevel,
        ShopBoosterType.StarterFireRate => StarterFireRateLevel,
        ShopBoosterType.ServerArmor => ServerArmorLevel,
        ShopBoosterType.ExtraBullets => ExtraBulletsLevel,
        _ => 0,
    };

    public static void AddBoosterLevel(ShopBoosterType type)
    {
        switch (type)
        {
            case ShopBoosterType.StarterDamage:
                StarterDamageLevel++;
                break;
            case ShopBoosterType.StarterFireRate:
                StarterFireRateLevel++;
                break;
            case ShopBoosterType.ServerArmor:
                ServerArmorLevel++;
                break;
            case ShopBoosterType.ExtraBullets:
                ExtraBulletsLevel++;
                break;
        }

        SaveRun();
    }

    public static int GetPowerUpLevel(UpgradeType type) => type switch
    {
        UpgradeType.OverclockCPU => PowerUpCpuLevel,
        UpgradeType.UpgradeRAM => PowerUpRamLevel,
        UpgradeType.Firewall => PowerUpFirewallLevel,
        UpgradeType.DoubleShot => PowerUpDoubleShotLevel,
        UpgradeType.Ricochet => PowerUpRicochetLevel,
        UpgradeType.PiercingBeam => PowerUpPiercingBeamLevel,
        _ => 0,
    };

    public static void AddPowerUpLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.OverclockCPU:
                PowerUpCpuLevel++;
                break;
            case UpgradeType.UpgradeRAM:
                PowerUpRamLevel++;
                break;
            case UpgradeType.Firewall:
                PowerUpFirewallLevel++;
                break;
            case UpgradeType.DoubleShot:
                PowerUpDoubleShotLevel++;
                break;
            case UpgradeType.Ricochet:
                PowerUpRicochetLevel++;
                break;
            case UpgradeType.PiercingBeam:
                PowerUpPiercingBeamLevel++;
                break;
        }
    }

    public static void AdvanceStage()
    {
        Stage++;
        SaveRun();
    }

    public static void ResetRun()
    {
        Stage = 1;
        DataPack = 0;
        StarterDamageLevel = 0;
        StarterFireRateLevel = 0;
        ServerArmorLevel = 0;
        ExtraBulletsLevel = 0;
        ClearPowerUps();
        SaveRun();
        GameEvents.RaiseDataPackChanged(DataPack);
    }

    public static bool LoadSavedRun()
    {
        RunSaveData saveData = GetSaveData();
        if (!saveData.HasSave)
        {
            return false;
        }

        Stage = Mathf.Max(1, saveData.Stage);
        DataPack = Mathf.Max(0, saveData.DataPack);
        StarterDamageLevel = Mathf.Max(0, saveData.StarterDamageLevel);
        StarterFireRateLevel = Mathf.Max(0, saveData.StarterFireRateLevel);
        ServerArmorLevel = Mathf.Max(0, saveData.ServerArmorLevel);
        ExtraBulletsLevel = Mathf.Max(0, saveData.ExtraBulletsLevel);
        ClearPowerUps();
        GameEvents.RaiseDataPackChanged(DataPack);
        return true;
    }

    public static void ClearSavedRun()
    {
        GetSaveData().Clear();
        Stage = 1;
        DataPack = 0;
        StarterDamageLevel = 0;
        StarterFireRateLevel = 0;
        ServerArmorLevel = 0;
        ExtraBulletsLevel = 0;
        ClearPowerUps();
        GameEvents.RaiseDataPackChanged(DataPack);
    }

    private static void SaveRun()
    {
        GetSaveData().Save(Stage, DataPack, StarterDamageLevel, StarterFireRateLevel, ServerArmorLevel, ExtraBulletsLevel);
    }

    private static RunSaveData GetSaveData()
    {
        if (saveDataCache != null)
        {
            return saveDataCache;
        }

        RunSaveData saveData = Resources.Load<RunSaveData>(SaveResourcePath);
        if (saveData != null)
        {
            saveDataCache = saveData;
            return saveDataCache;
        }

        saveDataCache = ScriptableObject.CreateInstance<RunSaveData>();
        saveDataCache.name = "RuntimeRunSaveData";
        return saveDataCache;
    }

    private static void ClearPowerUps()
    {
        PowerUpCpuLevel = 0;
        PowerUpRamLevel = 0;
        PowerUpFirewallLevel = 0;
        PowerUpDoubleShotLevel = 0;
        PowerUpRicochetLevel = 0;
        PowerUpPiercingBeamLevel = 0;
    }
}
