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
    private const float EnemyHealthGrowthPerStage = 0.25f;
    private const float EnemySpawnGrowthPerStage = 0.25f;

    public static int Stage { get; private set; } = 1;
    public static int DataPack { get; private set; }
    public static int StarterDamageLevel { get; private set; }
    public static int StarterFireRateLevel { get; private set; }
    public static int ServerArmorLevel { get; private set; }
    public static int ExtraBulletsLevel { get; private set; }
    public static bool HasSavedRun => GetSaveData().HasSave;

    public static int StarterDamageBonus => StarterDamageLevel * DamageBonusPerLevel;
    public static float StarterFireRateReduction => StarterFireRateLevel * FireRateReductionPerLevel;
    public static int ServerHpBonus => ServerArmorLevel * ServerHpBonusPerLevel;
    public static int ExtraBulletsBonus => ExtraBulletsLevel * ExtraBulletsPerLevel;

    public static int NextStage => Stage + 1;
    public static float EnemyHealthMultiplier => GetEnemyHealthMultiplier(Stage);
    public static float EnemySpawnRateMultiplier => GetEnemySpawnRateMultiplier(Stage);
    public static int EnemyGroupBonus => GetEnemyGroupBonus(Stage);

    public static float GetEnemyHealthMultiplier(int stage)
    {
        return 1f + Mathf.Max(0, stage - 1) * EnemyHealthGrowthPerStage;
    }

    public static float GetEnemySpawnRateMultiplier(int stage)
    {
        return 1f + Mathf.Max(0, stage - 1) * EnemySpawnGrowthPerStage;
    }

    public static int GetEnemyGroupBonus(int stage)
    {
        return Mathf.FloorToInt(Mathf.Max(0, stage - 1) * 1.5f);
    }

    public static string GetStageDifficultySummary(int stage)
    {
        return $"Enemy HP x{GetEnemyHealthMultiplier(stage):0.00} | Spawn x{GetEnemySpawnRateMultiplier(stage):0.00} | Group +{GetEnemyGroupBonus(stage)}";
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
}
