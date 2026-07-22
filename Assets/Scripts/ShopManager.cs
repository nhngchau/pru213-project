using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }
    // Giá booster tăng tuyến tính còn thu nhập DataPack tăng theo số quái mỗi stage (nhanh hơn nhiều),
    // nên không có trần thì stage cuối người chơi mua được gần như không giới hạn.
    private const int StarterDamageMaxLevel = 15;
    // Nhịp bắn gốc của vũ khí là 0.3s và PlayerShooting kẹp sàn ở 0.1s, mỗi cấp giảm 0.02s -> đúng
    // 10 cấp là chạm sàn. Bán quá số này là bán cấp không có tác dụng gì.
    private const int StarterFireRateMaxLevel = 10;
    private const int ServerArmorMaxLevel = 10;
    // Cộng thẳng vào cùng biến bulletsPerShot với power-up Double Shot nên phải chặt tay hơn hẳn.
    // Trần 1 ở đây + trần 3 của Double Shot + 1 tia gốc = tối đa 5 tia mỗi phát.
    private const int ExtraBulletsMaxLevel = 1;

    private static readonly ShopBoosterType[] BoosterOrder =
    {
        ShopBoosterType.StarterDamage,
        ShopBoosterType.StarterFireRate,
        ShopBoosterType.ServerArmor,
        ShopBoosterType.ExtraBullets,
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public ShopBoosterType[] GetBoosters() => BoosterOrder;

    public string GetTitle(ShopBoosterType type) => type switch
    {
        ShopBoosterType.StarterDamage => "Starter Damage",
        ShopBoosterType.StarterFireRate => "Quick Compiler",
        ShopBoosterType.ServerArmor => "Server Armor",
        ShopBoosterType.ExtraBullets => "Extra Barrel",
        _ => string.Empty,
    };

    public string GetDescription(ShopBoosterType type) => type switch
    {
        ShopBoosterType.StarterDamage => $"+8 starting bullet damage. {LevelLabel(type)}",
        ShopBoosterType.StarterFireRate => IsMaxed(type)
            ? "Fire cooldown is already capped."
            : $"-0.02s starting fire cooldown. {LevelLabel(type)}",
        ShopBoosterType.ServerArmor => $"+150 starting server HP. {LevelLabel(type)}",
        ShopBoosterType.ExtraBullets => $"+1 bullet per shot at stage start. {LevelLabel(type)}",
        _ => string.Empty,
    };

    private string LevelLabel(ShopBoosterType type)
    {
        return $"Lv {RunProgress.GetBoosterLevel(type)} / {GetMaxLevel(type)}";
    }

    public int GetCost(ShopBoosterType type)
    {
        int level = RunProgress.GetBoosterLevel(type);
        return type switch
        {
            ShopBoosterType.StarterDamage => 100 + level * 75,
            ShopBoosterType.StarterFireRate => 120 + level * 90,
            ShopBoosterType.ServerArmor => 90 + level * 70,
            ShopBoosterType.ExtraBullets => 160 + level * 120,
            _ => 0,
        };
    }

    public int GetMaxLevel(ShopBoosterType type) => type switch
    {
        ShopBoosterType.StarterDamage => StarterDamageMaxLevel,
        ShopBoosterType.StarterFireRate => StarterFireRateMaxLevel,
        ShopBoosterType.ServerArmor => ServerArmorMaxLevel,
        ShopBoosterType.ExtraBullets => ExtraBulletsMaxLevel,
        _ => int.MaxValue,
    };

    public bool IsMaxed(ShopBoosterType type)
    {
        return RunProgress.GetBoosterLevel(type) >= GetMaxLevel(type);
    }

    public bool CanBuy(ShopBoosterType type) => !IsMaxed(type) && RunProgress.DataPack >= GetCost(type);

    public bool TryBuy(ShopBoosterType type)
    {
        if (IsMaxed(type))
        {
            return false;
        }

        int cost = GetCost(type);
        if (!RunProgress.SpendDataPack(cost))
        {
            return false;
        }

        RunProgress.AddBoosterLevel(type);
        GameEvents.RaiseUpgradePurchased();
        return true;
    }
}
