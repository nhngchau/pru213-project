using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    private const int DamageBonusPerLevel = 8;
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
    public static int PowerUpPiercingBeamLevel { get; private set; }
    public static bool HasSavedRun => GetSaveData().HasSave;
    
    public static List<string> UnlockedWeapons { get; private set; } = new List<string>() { "default_gun" };
    public static string EquippedWeaponID { get; private set; } = "default_gun";

    public static int StarterDamageBonus => StarterDamageLevel * DamageBonusPerLevel;
    public static float StarterFireRateReduction => StarterFireRateLevel * FireRateReductionPerLevel;
    public static int ServerHpBonus => ServerArmorLevel * ServerHpBonusPerLevel;
    public static int ExtraBulletsBonus => ExtraBulletsLevel * ExtraBulletsPerLevel;

    /// <summary>
    /// Tổng số power-up đã lấy trong run này. KHÔNG reset theo stage (khác với
    /// PlayerProgression.Level vốn chỉ đếm trong một stage), nên đây mới là con số trả lời đúng
    /// câu hỏi "tôi đã mạnh lên bao nhiêu" và là thứ nên hiện trên HUD.
    /// </summary>
    public static int TotalPowerUpLevels =>
        PowerUpCpuLevel + PowerUpRamLevel + PowerUpFirewallLevel
        + PowerUpDoubleShotLevel + PowerUpPiercingBeamLevel;

    public static int NextStage => Stage + 1;
    public static float EnemyHealthMultiplier => GetEnemyHealthMultiplier(Stage);
    public static float EnemyDamageMultiplier => GetEnemyDamageMultiplier(Stage);
    public static float EnemySpawnRateMultiplier => GetEnemySpawnRateMultiplier(Stage);
    public static int EnemyGroupBonus => GetEnemyGroupBonus(Stage);

    public static float GetEnemyHealthMultiplier(int stage)
    {
        // 1.28^(stage-1): stage 1=×1, stage 5=×2.7, stage 10=×9.3
        // Damage của người chơi tăng tuyến tính (+5 mỗi level CPU/booster) nên hệ số mũ phải nhỏ,
        // nếu không stage 7 trở đi là bức tường không thể phá.
        return Mathf.Pow(1.28f, Mathf.Max(0, stage - 1));
    }

    public static float GetEnemyDamageMultiplier(int stage)
    {
        // 1.12^(stage-1): stage 1=×1, stage 5=×1.6, stage 10=×2.8
        // TÁCH khỏi công thức HP một cách có chủ đích: máu Server chỉ tăng tuyến tính (+50%/stage,
        // xem ServerCore.Start), nên damage mà tăng cùng nhịp với HP quái thì stage cuối chỉ cần
        // hơn chục con lọt lưới là Server chết ngay.
        return Mathf.Pow(1.12f, Mathf.Max(0, stage - 1));
    }

    public static float GetEnemySpawnRateMultiplier(int stage)
    {
        // +15% mỗi stage: stage 1=×1.00, stage 10=×2.35
        return 1f + Mathf.Max(0, stage - 1) * EnemySpawnGrowthPerStage;
    }

    public static int GetEnemyGroupBonus(int stage)
    {
        // +1 mỗi stage nhưng chặn ở +4: stage 1=+0, stage 5=+4, stage 10=+4.
        // Số quái mỗi nhóm đã nhân với tốc độ spawn (×2.35 ở stage 10) và với máu quái, nên để nó
        // tăng không giới hạn là dồn ba hệ số nhân lên nhau.
        return Mathf.Min(4, Mathf.Max(0, stage - 1));
    }

    public static string GetStageDifficultySummary(int stage)
    {
        return $"Enemy HP x{GetEnemyHealthMultiplier(stage):0.00} | Damage x{GetEnemyDamageMultiplier(stage):0.00} | Spawn x{GetEnemySpawnRateMultiplier(stage):0.00}";
    }

    public static void SetDataPack(int amount)
    {
        DataPack = Mathf.Max(0, amount);

        // Không flush xuống đĩa ở đây: hàm này chạy mỗi lần hạ một con quái (~104 lần mỗi stage).
        // Các mốc thật sự quan trọng — qua stage, mua booster, chọn power-up, thua — đều flush đầy đủ.
        SaveRun(flushToDisk: false);

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
        PowerUpPiercingBeamLevel = Mathf.Max(0, saveData.PowerUpPiercingBeamLevel);

        UnlockedWeapons = saveData.UnlockedWeapons.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        if (UnlockedWeapons.Count == 0) UnlockedWeapons.Add("default_gun");
        
        EquippedWeaponID = string.IsNullOrEmpty(saveData.EquippedWeaponID) ? "default_gun" : saveData.EquippedWeaponID;

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

    private static void SaveRun(bool flushToDisk = true)
    {
        string unlockedStr = string.Join(",", UnlockedWeapons);
        GetSaveData().Save(
            Stage, DataPack,
            StarterDamageLevel, StarterFireRateLevel, ServerArmorLevel, ExtraBulletsLevel,
            PowerUpCpuLevel, PowerUpRamLevel, PowerUpFirewallLevel,
            PowerUpDoubleShotLevel, PowerUpPiercingBeamLevel,
            CheckpointStage, unlockedStr, EquippedWeaponID, flushToDisk);
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

    public static void UnlockWeapon(string weaponID)
    {
        if (!UnlockedWeapons.Contains(weaponID))
        {
            UnlockedWeapons.Add(weaponID);
            SaveRun();
        }
    }

    public static void EquipWeapon(string weaponID)
    {
        if (UnlockedWeapons.Contains(weaponID))
        {
            EquippedWeaponID = weaponID;
            SaveRun();
        }
    }
}
