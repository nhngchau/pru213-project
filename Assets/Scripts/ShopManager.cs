using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }
    private const int StarterFireRateMaxLevel = 20;

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
        ShopBoosterType.StarterDamage => $"+5 starting bullet damage. Lv {RunProgress.GetBoosterLevel(type)}",
        ShopBoosterType.StarterFireRate => IsMaxed(type)
            ? "Fire cooldown is already capped."
            : $"-0.02s starting fire cooldown. Lv {RunProgress.GetBoosterLevel(type)} / {StarterFireRateMaxLevel}",
        ShopBoosterType.ServerArmor => $"+150 starting server HP. Lv {RunProgress.GetBoosterLevel(type)}",
        ShopBoosterType.ExtraBullets => $"+1 bullet per shot at stage start. Lv {RunProgress.GetBoosterLevel(type)}",
        _ => string.Empty,
    };

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

    public bool IsMaxed(ShopBoosterType type)
    {
        return type == ShopBoosterType.StarterFireRate
            && RunProgress.GetBoosterLevel(type) >= StarterFireRateMaxLevel;
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
