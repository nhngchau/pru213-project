using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }
    
    private Dictionary<string, WeaponConfig> weaponDatabase = new Dictionary<string, WeaponConfig>();
    public List<WeaponConfig> AllWeapons { get; private set; } = new List<WeaponConfig>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadWeapons();
    }

    private void LoadWeapons()
    {
        WeaponConfig[] configs = Resources.LoadAll<WeaponConfig>("Weapons");
        AllWeapons = configs.OrderBy(w => w.price).ToList();

        weaponDatabase.Clear();
        foreach (var config in configs)
        {
            if (!weaponDatabase.ContainsKey(config.weaponID))
            {
                weaponDatabase.Add(config.weaponID, config);
            }
        }
        
        Debug.Log($"[WeaponManager] Loaded {weaponDatabase.Count} weapons.");
    }

    public WeaponConfig GetWeaponByID(string weaponID)
    {
        if (string.IsNullOrEmpty(weaponID)) return null;
        if (weaponDatabase.TryGetValue(weaponID, out var config))
        {
            return config;
        }
        
        // Fallback to default if somehow missing
        if (weaponDatabase.TryGetValue("default_gun", out var defaultCfg))
        {
            return defaultCfg;
        }
        
        // Ultimate fallback
        return AllWeapons.FirstOrDefault();
    }

    public WeaponConfig GetEquippedWeapon()
    {
        return GetWeaponByID(RunProgress.EquippedWeaponID);
    }
}
