using UnityEngine;

/// <summary>
/// Lưu tiến trình run bằng PlayerPrefs — hoạt động cả trong Editor lẫn build .exe.
/// </summary>
[CreateAssetMenu(fileName = "RunSaveData", menuName = "The Senior Defender/Run Save Data")]
public class RunSaveData : ScriptableObject
{
    // --- PlayerPrefs keys ---
    private const string KeyHasSave              = "SD_HasSave";
    private const string KeyStage                = "SD_Stage";
    private const string KeyBestStage            = "SD_BestStage";
    private const string KeyDataPack             = "SD_DataPack";
    private const string KeyStarterDamage        = "SD_StarterDamage";
    private const string KeyStarterFireRate      = "SD_StarterFireRate";
    private const string KeyServerArmor          = "SD_ServerArmor";
    private const string KeyExtraBullets         = "SD_ExtraBullets";
    private const string KeyPowerUpCpu           = "SD_PU_Cpu";
    private const string KeyPowerUpRam           = "SD_PU_Ram";
    private const string KeyPowerUpFirewall      = "SD_PU_Firewall";
    private const string KeyPowerUpDoubleShot    = "SD_PU_DoubleShot";
    private const string KeyPowerUpPiercingBeam  = "SD_PU_PiercingBeam";
    private const string KeyCheckpointStage      = "SD_CheckpointStage"; // stage mốc gần nhất đã vượt qua
    private const string KeyUnlockedWeapons      = "SD_UnlockedWeapons";
    private const string KeyEquippedWeapon       = "SD_EquippedWeapon";

    // --- Properties đọc từ PlayerPrefs ---
    public bool HasSave              => PlayerPrefs.GetInt(KeyHasSave, 0) == 1;
    public int  Stage                => PlayerPrefs.GetInt(KeyStage, 1);
    public int  BestStage            => PlayerPrefs.GetInt(KeyBestStage, 1);
    public int  DataPack             => PlayerPrefs.GetInt(KeyDataPack, 0);
    public int  StarterDamageLevel   => PlayerPrefs.GetInt(KeyStarterDamage, 0);
    public int  StarterFireRateLevel => PlayerPrefs.GetInt(KeyStarterFireRate, 0);
    public int  ServerArmorLevel     => PlayerPrefs.GetInt(KeyServerArmor, 0);
    public int  ExtraBulletsLevel    => PlayerPrefs.GetInt(KeyExtraBullets, 0);

    // --- Power-up levels (level-up trong game) ---
    public int  PowerUpCpuLevel          => PlayerPrefs.GetInt(KeyPowerUpCpu, 0);
    public int  PowerUpRamLevel          => PlayerPrefs.GetInt(KeyPowerUpRam, 0);
    public int  PowerUpFirewallLevel     => PlayerPrefs.GetInt(KeyPowerUpFirewall, 0);
    public int  PowerUpDoubleShotLevel   => PlayerPrefs.GetInt(KeyPowerUpDoubleShot, 0);
    public int  PowerUpPiercingBeamLevel => PlayerPrefs.GetInt(KeyPowerUpPiercingBeam, 0);

    /// <summary>Stage mốc gần nhất đã vượt qua (1 nếu chưa vượt milestone nào).</summary>
    public int  CheckpointStage          => PlayerPrefs.GetInt(KeyCheckpointStage, 1);
    
    public string UnlockedWeapons        => PlayerPrefs.GetString(KeyUnlockedWeapons, "default_gun");
    public string EquippedWeaponID       => PlayerPrefs.GetString(KeyEquippedWeapon, "default_gun");

    /// <summary>
    /// Lưu toàn bộ tiến trình xuống PlayerPrefs.
    ///
    /// <paramref name="flushToDisk"/> = false thì chỉ ghi vào bộ nhớ, không gọi PlayerPrefs.Save().
    /// Dùng cho những thứ thay đổi liên tục như DataPack (cộng mỗi lần giết một con quái) — Save()
    /// ghi thẳng xuống Registry nên gọi trăm lần mỗi màn là đủ gây khựng hình.
    /// Dữ liệu vẫn an toàn: Unity tự flush khi thoát game, và mọi mốc quan trọng đều flush thật.
    /// </summary>
    public void Save(
        int runStage, int runDataPack,
        int damageLevel, int fireRateLevel, int armorLevel, int bulletsLevel,
        int cpuLevel, int ramLevel, int firewallLevel,
        int doubleShotLevel, int piercingBeamLevel,
        int checkpointStage,
        string unlockedWeapons = null,
        string equippedWeapon = null,
        bool flushToDisk = true)
    {
        int bestStage = Mathf.Max(PlayerPrefs.GetInt(KeyBestStage, 1), runStage);

        PlayerPrefs.SetInt(KeyHasSave,           1);
        PlayerPrefs.SetInt(KeyStage,             Mathf.Max(1, runStage));
        PlayerPrefs.SetInt(KeyBestStage,         bestStage);
        PlayerPrefs.SetInt(KeyDataPack,          Mathf.Max(0, runDataPack));
        PlayerPrefs.SetInt(KeyStarterDamage,     Mathf.Max(0, damageLevel));
        PlayerPrefs.SetInt(KeyStarterFireRate,   Mathf.Max(0, fireRateLevel));
        PlayerPrefs.SetInt(KeyServerArmor,       Mathf.Max(0, armorLevel));
        PlayerPrefs.SetInt(KeyExtraBullets,      Mathf.Max(0, bulletsLevel));
        PlayerPrefs.SetInt(KeyPowerUpCpu,        Mathf.Max(0, cpuLevel));
        PlayerPrefs.SetInt(KeyPowerUpRam,        Mathf.Max(0, ramLevel));
        PlayerPrefs.SetInt(KeyPowerUpFirewall,   Mathf.Max(0, firewallLevel));
        PlayerPrefs.SetInt(KeyPowerUpDoubleShot, Mathf.Max(0, doubleShotLevel));
        PlayerPrefs.SetInt(KeyPowerUpPiercingBeam, Mathf.Max(0, piercingBeamLevel));
        PlayerPrefs.SetInt(KeyCheckpointStage,   Mathf.Max(1, checkpointStage));

        if (unlockedWeapons != null) PlayerPrefs.SetString(KeyUnlockedWeapons, unlockedWeapons);
        if (equippedWeapon != null) PlayerPrefs.SetString(KeyEquippedWeapon, equippedWeapon);

        if (flushToDisk)
        {
            PlayerPrefs.Save();
        }
    }

    /// <summary>Xóa save hiện tại — BestStage được giữ lại vĩnh viễn.</summary>
    public void Clear()
    {
        PlayerPrefs.SetInt(KeyHasSave,           0);
        PlayerPrefs.SetInt(KeyStage,             1);
        PlayerPrefs.SetInt(KeyDataPack,          0);
        PlayerPrefs.SetInt(KeyStarterDamage,     0);
        PlayerPrefs.SetInt(KeyStarterFireRate,   0);
        PlayerPrefs.SetInt(KeyServerArmor,       0);
        PlayerPrefs.SetInt(KeyExtraBullets,      0);
        PlayerPrefs.SetInt(KeyPowerUpCpu,        0);
        PlayerPrefs.SetInt(KeyPowerUpRam,        0);
        PlayerPrefs.SetInt(KeyPowerUpFirewall,   0);
        PlayerPrefs.SetInt(KeyPowerUpDoubleShot, 0);
        PlayerPrefs.SetInt(KeyPowerUpPiercingBeam, 0);
        PlayerPrefs.SetInt(KeyCheckpointStage,   1); // reset checkpoint về đầu
        
        // Weapon shop progress is typically kept across resets, but if Clear() means full wipe, we wipe them.
        PlayerPrefs.SetString(KeyUnlockedWeapons, "default_gun");
        PlayerPrefs.SetString(KeyEquippedWeapon, "default_gun");
        PlayerPrefs.Save();
        // KeyBestStage không bị xóa — kỷ lục tồn tại vĩnh viễn
    }
}
