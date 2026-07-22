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
    private const float EnemySpawnGrowthPerStage = 0.15f; // tăng nhanh hơn: +15% mỗi stage

    public const int MaxStage = 10; // stage tối đa, stage 10 = win game

    public static int Stage { get; private set; } = 1;
    public static bool IsGameCompleted => Stage > MaxStage;
    public static int CheckpointStage { get; private set; } = 1; // stage mốc gần nhất đã vượt qua
    public static int DataPack { get; private set; }
    public static int BestStage => GetSaveData().BestStage;
    public static int StarterDamageLevel { get; private set; }
    public static int StarterFireRateLevel { get; private set; }
    public static int ServerArmorLevel { get; private set; }
    public static int ExtraBulletsLevel { get; private set; }
    public static int PowerUpCpuLevel { get; private set; }
    public static int PowerUpRamLevel { get; private set; }
    public static int PowerUpFirewallLevel { get; private set; }
    public static int PowerUpDoubleShotLevel { get; private set; }
    public static int PowerUpExplosiveLevel { get; private set; }
    public static int PowerUpPiercingBeamLevel { get; private set; }
    public static bool HasSavedRun => GetSaveData().HasSave;

    /// <summary>
    /// Level + EXP của player, giữ nguyên khi chuyển stage (không reset về 1/0 nữa)
    /// nên ngưỡng lên cấp tiếp tục tăng thay vì quay lại mức khởi điểm.
    /// </summary>
    public static int PlayerLevel { get; private set; } = 1;
    public static int PlayerExp { get; private set; }

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
        // 1.55^(stage-1): stage 1=×1, stage 5=×5.7, stage 10=×51
        return Mathf.Pow(1.55f, Mathf.Max(0, stage - 1));
    }

    public static float GetEnemyDamageMultiplier(int stage)
    {
        // Cùng công thức HP để cân đối
        return Mathf.Pow(1.55f, Mathf.Max(0, stage - 1));
    }

    public static float GetEnemySpawnRateMultiplier(int stage)
    {
        // +15% mỗi stage: stage 1=×1.00, stage 10=×2.35
        return 1f + Mathf.Max(0, stage - 1) * EnemySpawnGrowthPerStage;
    }

    public static int GetEnemyGroupBonus(int stage)
    {
        // +1 mỗi stage (tăng mạnh hơn 0.75 cũ): stage 1=+0, stage 10=+9
        return Mathf.Max(0, stage - 1);
    }

    public static string GetStageDifficultySummary(int stage)
    {
        return $"Enemy HP x{GetEnemyHealthMultiplier(stage):0.00} | Damage x{GetEnemyDamageMultiplier(stage):0.00} | Spawn x{GetEnemySpawnRateMultiplier(stage):0.00}";
    }

    /// <summary>
    /// Cập nhật level/exp. Chỉ ghi xuống đĩa khi <paramref name="persist"/> = true
    /// (mỗi lần lên cấp) — các thay đổi EXP lặt vặt chỉ giữ trong bộ nhớ static,
    /// vốn đã sống xuyên qua việc load lại scene giữa các stage.
    /// </summary>
    public static void SetPlayerProgress(int level, int exp, bool persist = false)
    {
        PlayerLevel = Mathf.Max(1, level);
        PlayerExp = Mathf.Max(0, exp);

        if (persist)
        {
            SaveRun();
        }
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
        UpgradeType.Explosive => PowerUpExplosiveLevel,
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
            case UpgradeType.Explosive:
                PowerUpExplosiveLevel++;
                break;
            case UpgradeType.PiercingBeam:
                PowerUpPiercingBeamLevel++;
                break;
        }

        SaveRun(); // lưu ngay sau khi chọn upgrade
    }

    public static void AdvanceStage()
    {
        // Kiểm tra milestone TRƯỚC khi tăng stage (stage 3, 6, 9)
        bool isMilestone = (Stage % 3 == 0 && Stage < MaxStage);
        int milestoneBonus = isMilestone ? Stage * 10 : 0;

        bool isFinalStage = (Stage >= MaxStage); // vừa clear stage 10

        Stage++;

        if (isMilestone)
        {
            // Checkpoint = chính stage milestone vừa vượt qua (3, 6, 9)
            // Thua 4,5 -> về 3 | Thua 7,8 -> về 6 | Thua 10 -> về 9
            CheckpointStage = Stage - 1; // Stage đã được tăng lên ở trên nên trừ 1 để lấy mốc
            DataPack += milestoneBonus;
            GameEvents.RaiseDataPackChanged(DataPack);
            GameEvents.RaiseMilestoneReached(Stage - 1, milestoneBonus);
        }

        if (isFinalStage)
        {
            // Clear stage 10 → thắng toàn bộ game!
            GameEvents.RaiseGameCompleted();
        }

        SaveRun();
    }

    public static void ResetRun()
    {
        Stage = 1;
        CheckpointStage = 1;
        DataPack = 0;
        StarterDamageLevel = 0;
        StarterFireRateLevel = 0;
        ServerArmorLevel = 0;
        ExtraBulletsLevel = 0;
        ClearPowerUps();
        SaveRun();
        GameEvents.RaiseDataPackChanged(DataPack);
    }

    /// <summary>
    /// Thua stage → quay về stage mốc gần nhất đã vượt qua (checkpoint).
    /// Power-up trong game bị xóa, booster shop và DataPack giữ lại.
    /// </summary>
    public static void RestartFromCheckpoint()
    {
        Stage = CheckpointStage; // quay về milestone (3, 6, 9 hoặc 1 nếu chưa qua stage nào)
        ClearPowerUps();         // mất power-up trong game (phạt nhẹ)
        // Giữ lại: DataPack, booster shop (StarterDamage/FireRate/ServerArmor/ExtraBullets)
        SaveRun();
        GameEvents.RaiseDataPackChanged(DataPack);
        Debug.Log($"[RunProgress] Restarting from checkpoint: Stage {Stage}");
    }

    public static bool LoadSavedRun()
    {
        RunSaveData saveData = GetSaveData();
        if (!saveData.HasSave)
        {
            return false;
        }

        Stage                = Mathf.Max(1, saveData.Stage);
        CheckpointStage      = Mathf.Max(1, saveData.CheckpointStage);
        DataPack             = Mathf.Max(0, saveData.DataPack);
        StarterDamageLevel   = Mathf.Max(0, saveData.StarterDamageLevel);
        StarterFireRateLevel = Mathf.Max(0, saveData.StarterFireRateLevel);
        ServerArmorLevel     = Mathf.Max(0, saveData.ServerArmorLevel);
        ExtraBulletsLevel    = Mathf.Max(0, saveData.ExtraBulletsLevel);

        // Load power-up levels (level-up trong game)
        PowerUpCpuLevel          = Mathf.Max(0, saveData.PowerUpCpuLevel);
        PowerUpRamLevel          = Mathf.Max(0, saveData.PowerUpRamLevel);
        PowerUpFirewallLevel     = Mathf.Max(0, saveData.PowerUpFirewallLevel);
        PowerUpDoubleShotLevel   = Mathf.Max(0, saveData.PowerUpDoubleShotLevel);
        PowerUpExplosiveLevel    = Mathf.Max(0, saveData.PowerUpExplosiveLevel);
        PowerUpPiercingBeamLevel = Mathf.Max(0, saveData.PowerUpPiercingBeamLevel);

        PlayerLevel = Mathf.Max(1, saveData.PlayerLevel);
        PlayerExp   = Mathf.Max(0, saveData.PlayerExp);

        GameEvents.RaiseDataPackChanged(DataPack);
        return true;
    }

    public static void ClearSavedRun()
    {
        GetSaveData().Clear();
        Stage = 1;
        CheckpointStage = 1;
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
        GetSaveData().Save(
            Stage, DataPack,
            StarterDamageLevel, StarterFireRateLevel, ServerArmorLevel, ExtraBulletsLevel,
            PowerUpCpuLevel, PowerUpRamLevel, PowerUpFirewallLevel,
            PowerUpDoubleShotLevel, PowerUpExplosiveLevel, PowerUpPiercingBeamLevel,
            CheckpointStage,
            PlayerLevel, PlayerExp);
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
        // Level/EXP mất cùng power-up: power-up sinh ra từ việc lên cấp, giữ level cao
        // mà mất hết power-up sẽ khiến ngưỡng EXP quá nặng so với sức mạnh còn lại.
        PlayerLevel = 1;
        PlayerExp = 0;

        PowerUpCpuLevel = 0;
        PowerUpRamLevel = 0;
        PowerUpFirewallLevel = 0;
        PowerUpDoubleShotLevel = 0;
        PowerUpExplosiveLevel = 0;
        PowerUpPiercingBeamLevel = 0;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Chỉ dùng cho Debug/Demo — nhảy thẳng tới stage bất kỳ.</summary>
    public static void DebugSetStage(int targetStage)
    {
        Stage = Mathf.Max(1, targetStage);
        SaveRun();
        Debug.Log($"[DebugPanel] Stage set to {Stage}");
    }
#endif
}
