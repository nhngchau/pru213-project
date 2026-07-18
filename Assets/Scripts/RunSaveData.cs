using UnityEngine;

[CreateAssetMenu(fileName = "RunSaveData", menuName = "The Senior Defender/Run Save Data")]
public class RunSaveData : ScriptableObject
{
    [SerializeField] private bool hasSave;
    [SerializeField] private int stage = 1;
    [SerializeField] private int bestStage = 1;
    [SerializeField] private int dataPack;
    [SerializeField] private int starterDamageLevel;
    [SerializeField] private int starterFireRateLevel;
    [SerializeField] private int serverArmorLevel;
    [SerializeField] private int extraBulletsLevel;

    public bool HasSave => hasSave;
    public int Stage => stage;
    public int BestStage => bestStage;
    public int DataPack => dataPack;
    public int StarterDamageLevel => starterDamageLevel;
    public int StarterFireRateLevel => starterFireRateLevel;
    public int ServerArmorLevel => serverArmorLevel;
    public int ExtraBulletsLevel => extraBulletsLevel;

    public void Save(int runStage, int runDataPack, int damageLevel, int fireRateLevel, int armorLevel, int bulletsLevel)
    {
        hasSave = true;
        stage = Mathf.Max(1, runStage);
        bestStage = Mathf.Max(bestStage, stage); // chỉ cập nhật nếu cao hơn record cũ
        dataPack = Mathf.Max(0, runDataPack);
        starterDamageLevel = Mathf.Max(0, damageLevel);
        starterFireRateLevel = Mathf.Max(0, fireRateLevel);
        serverArmorLevel = Mathf.Max(0, armorLevel);
        extraBulletsLevel = Mathf.Max(0, bulletsLevel);
    }

    public void Clear()
    {
        hasSave = false;
        stage = 1;
        // bestStage không bị xóa khi reset — đây là kỷ lục vĩnh viễn
        dataPack = 0;
        starterDamageLevel = 0;
        starterFireRateLevel = 0;
        serverArmorLevel = 0;
        extraBulletsLevel = 0;
    }
}
