using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "The Senior Defender/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    [Header("Weapon")]
    [Min(0.05f)] public float fireRate = 0.15f;
    [Min(1)] public int bulletDamage = 10;
    [Min(1)] public int bulletsPerShot = 1;
    [Min(0f)] public float spreadAngle = 15f;
    [Min(0)] public int bulletBounces = 0;
    [Min(0)] public int bulletPierces = 0;

    [Header("Recoil")]
    [Min(0f)] public float recoilDistance = 0.08f;
    [Min(0f)] public float recoilReturnSpeed = 12f;

    [Header("Pool")]
    [Min(1)] public int defaultCapacity = 40;
    [Min(1)] public int maxPoolSize = 200;
}
