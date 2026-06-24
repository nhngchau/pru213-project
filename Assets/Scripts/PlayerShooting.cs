using UnityEngine;
using UnityEngine.Pool;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Weapon Stats (GDD v3.0 - tuned by UpgradeManager)")]
    [SerializeField] private float fireRate = 0.5f;     // Upgrade RAM lowers this (floor 0.1)
    [SerializeField] private int bulletDamage = 10;     // Overclock CPU raises this (+5 / level)

    [Header("Bullet Pool")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 100;

    private float nextFireTime;
    private IObjectPool<Bullet> bulletPool;

    void Awake()
    {
        bulletPool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: bullet => bullet.gameObject.SetActive(true),
            actionOnRelease: bullet => bullet.gameObject.SetActive(false),
            actionOnDestroy: bullet => Destroy(bullet.gameObject),
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxPoolSize);
    }

    private Bullet CreateBullet()
    {
        Bullet bullet = Instantiate(bulletPrefab).GetComponent<Bullet>();
        bullet.SetPool(bulletPool);
        return bullet;
    }

    // --- Upgrade hooks (GDD v3.0 - Section VI) -------------------------------
    public void SetFireRate(float value) => fireRate = Mathf.Max(0.1f, value);
    public void SetBulletDamage(int value) => bulletDamage = value;
    public int BulletDamage => bulletDamage; // read by PlayerUltimate so its damage scales with upgrades

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        // Aim the shot at the mouse cursor (screen -> world).
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDirection = mousePos - transform.position;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        // Pull a Code Bullet from the pool, place + aim it, then launch (resets velocity + lifetime).
        Bullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, 0f, angle));
        bullet.SetDamage(bulletDamage); // apply the current (possibly upgraded) damage per shot
        bullet.Launch();

        // Audio: weapon fired.
        GameAudioManager.Instance?.PlayShoot();
    }
}
