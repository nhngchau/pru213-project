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
    private const string KeyPowerUpExplosive     = "SD_PU_Explosive";
    private const string KeyPowerUpPiercingBeam  = "SD_PU_PiercingBeam";
    private const string KeyCheckpointStage      = "SD_CheckpointStage"; // stage mốc gần nhất đã vượt qua
    private const string KeyPlayerLevel          = "SD_PlayerLevel";
    private const string KeyPlayerExp            = "SD_PlayerExp";

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
    public int  PowerUpExplosiveLevel    => PlayerPrefs.GetInt(KeyPowerUpExplosive, 0);
    public int  PowerUpPiercingBeamLevel => PlayerPrefs.GetInt(KeyPowerUpPiercingBeam, 0);

    /// <summary>Stage mốc gần nhất đã vượt qua (1 nếu chưa vượt milestone nào).</summary>
    public int  CheckpointStage          => PlayerPrefs.GetInt(KeyCheckpointStage, 1);

    /// <summary>Level + EXP tích luỹ của player — giữ nguyên khi sang stage mới.</summary>
    public int  PlayerLevel              => PlayerPrefs.GetInt(KeyPlayerLevel, 1);
    public int  PlayerExp                => PlayerPrefs.GetInt(KeyPlayerExp, 0);

    /// <summary>Lưu toàn bộ tiến trình xuống PlayerPrefs (tự động ghi đĩa).</summary>
    public void Save(
        int runStage, int runDataPack,
        int damageLevel, int fireRateLevel, int armorLevel, int bulletsLevel,
        int cpuLevel, int ramLevel, int firewallLevel,
        int doubleShotLevel, int explosiveLevel, int piercingBeamLevel,
        int checkpointStage,
        int playerLevel, int playerExp)
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
        PlayerPrefs.SetInt(KeyPowerUpExplosive,  Mathf.Max(0, explosiveLevel));
        PlayerPrefs.SetInt(KeyPowerUpPiercingBeam, Mathf.Max(0, piercingBeamLevel));
        PlayerPrefs.SetInt(KeyCheckpointStage,   Mathf.Max(1, checkpointStage));
        PlayerPrefs.SetInt(KeyPlayerLevel,       Mathf.Max(1, playerLevel));
        PlayerPrefs.SetInt(KeyPlayerExp,         Mathf.Max(0, playerExp));

        PlayerPrefs.Save(); // ghi xuống đĩa ngay lập tức
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
        PlayerPrefs.SetInt(KeyPowerUpExplosive,  0);
        PlayerPrefs.SetInt(KeyPowerUpPiercingBeam, 0);
        PlayerPrefs.SetInt(KeyCheckpointStage,   1); // reset checkpoint về đầu
        PlayerPrefs.SetInt(KeyPlayerLevel,       1);
        PlayerPrefs.SetInt(KeyPlayerExp,         0);
        PlayerPrefs.Save();
        // KeyBestStage không bị xóa — kỷ lục tồn tại vĩnh viễn
    }
}
