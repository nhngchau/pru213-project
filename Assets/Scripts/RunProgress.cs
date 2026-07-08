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
    private const int DamageBonusPerLevel = 5;
    private const float FireRateReductionPerLevel = 0.02f;
    private const int ServerHpBonusPerLevel = 150;
    private const int ExtraBulletsPerLevel = 1;

    public static int Stage { get; private set; } = 1;
    public static int DataPack { get; private set; }
    public static int StarterDamageLevel { get; private set; }
    public static int StarterFireRateLevel { get; private set; }
    public static int ServerArmorLevel { get; private set; }
    public static int ExtraBulletsLevel { get; private set; }

    public static int StarterDamageBonus => StarterDamageLevel * DamageBonusPerLevel;
    public static float StarterFireRateReduction => StarterFireRateLevel * FireRateReductionPerLevel;
    public static int ServerHpBonus => ServerArmorLevel * ServerHpBonusPerLevel;
    public static int ExtraBulletsBonus => ExtraBulletsLevel * ExtraBulletsPerLevel;

    public static float EnemyHealthMultiplier => 1f + Mathf.Max(0, Stage - 1) * 0.25f;
    public static float EnemySpawnRateMultiplier => 1f + Mathf.Max(0, Stage - 1) * 0.15f;

    public static void SetDataPack(int amount)
    {
        DataPack = Mathf.Max(0, amount);
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
    }

    public static void AdvanceStage()
    {
        Stage++;
    }

    public static void ResetRun()
    {
        Stage = 1;
        DataPack = 0;
        StarterDamageLevel = 0;
        StarterFireRateLevel = 0;
        ServerArmorLevel = 0;
        ExtraBulletsLevel = 0;
    }
}
