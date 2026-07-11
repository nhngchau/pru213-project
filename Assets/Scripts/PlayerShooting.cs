using UnityEngine;
using UnityEngine.Pool;

public class PlayerShooting : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponConfig weaponConfig;

    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Weapon Stats (GDD v3.0 - tuned by UpgradeManager)")]
    [SerializeField] private float fireRate = 0.15f;        // Fire cooldown in seconds. Upgrade RAM lowers this (floor 0.1).
    [SerializeField] private int bulletDamage = 10;         // Damage per bullet. Overclock CPU raises this (+5 / level).
    [SerializeField] private int bulletsPerShot = 1;        // How many bullets fire at once. Upgrades can raise this to multi-shot.
    [SerializeField] private float spreadAngle = 15f;       // Half-angle of the bullet fan in degrees. Bullets are spread evenly across [-spreadAngle, +spreadAngle].
    [SerializeField] private int bulletBounces;
    [SerializeField] private int bulletPierces;

    [Header("Recoil")]
    [SerializeField] private float recoilDistance = 0.08f;
    [SerializeField] private float recoilReturnSpeed = 12f;

    [Header("Bullet Pool")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 100;

    private float nextFireTime;
    private IObjectPool<Bullet> bulletPool;
    private Vector3 firePointStartLocalPosition;

    void Awake()
    {
        ApplyConfig();

        if (bulletPrefab == null || !bulletPrefab.TryGetComponent(out Bullet _))
        {
            Debug.LogError("PlayerShooting: Bullet Prefab must be assigned and contain a Bullet component.");
            enabled = false;
            return;
        }

        if (firePoint != null)
        {
            firePointStartLocalPosition = firePoint.localPosition;
        }

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
    public void SetBulletsPerShot(int value) => bulletsPerShot = Mathf.Max(1, value);
    public void SetBulletBounces(int value) => bulletBounces = Mathf.Max(0, value);
    public void SetBulletPierces(int value) => bulletPierces = Mathf.Max(0, value);
    public float FireRate => fireRate;
    public int BulletDamage => bulletDamage; // read by PlayerUltimate so its damage scales with upgrades
    public int BulletsPerShot => bulletsPerShot;
    public int BulletBounces => bulletBounces;
    public int BulletPierces => bulletPierces;

    void Update()
    {
        ReturnRecoilToNormal();

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        if (Camera.main == null)
        {
            return;
        }

        // Aim all bullets toward the mouse cursor (screen -> world).
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDirection = mousePos - transform.position;
        float baseAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        // --- Multi-shot fan spread -----------------------------------------
        // With bulletsPerShot = 1, fires straight with no spread.
        // With bulletsPerShot = N, fires N bullets evenly across
        // [-spreadAngle, +spreadAngle] relative to base aim.
        // Examples (spreadAngle = 15):
        //   N=1 : 0 degrees
        //   N=3 : -15, 0, +15 degrees
        //   N=5 : -15, -7.5, 0, +7.5, +15 degrees
        int count = Mathf.Max(1, bulletsPerShot);
        for (int i = 0; i < count; i++)
        {
            float offset = count == 1
                ? 0f                                                    // single shot: dead-centre
                : Mathf.Lerp(-spreadAngle, spreadAngle, i / (float)(count - 1)); // even fan

            float finalAngle = baseAngle + offset;

            Bullet bullet = bulletPool.Get();
            bullet.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, 0f, finalAngle));
            bullet.SetDamage(bulletDamage);
            bullet.SetModifiers(bulletBounces, bulletPierces);
            bullet.Launch();
        }

        ApplyRecoil(lookDirection);

        // Audio: weapon fired (once per trigger pull, not once per bullet).
        GameAudioManager.Instance?.PlayShoot();
    }

    private void ApplyRecoil(Vector2 lookDirection)
    {
        if (firePoint == null || lookDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        // Move only the fire point backward for a visual kick. This does not move the player body.
        Vector3 worldRecoil = -(Vector3)lookDirection.normalized * recoilDistance;
        Vector3 localRecoil = firePoint.parent != null
            ? firePoint.parent.InverseTransformVector(worldRecoil)
            : worldRecoil;

        firePoint.localPosition = firePointStartLocalPosition + localRecoil;
    }

    private void ReturnRecoilToNormal()
    {
        if (firePoint == null)
        {
            return;
        }

        // Smoothly return the fire point to its original local position after each shot.
        firePoint.localPosition = Vector3.Lerp(
            firePoint.localPosition,
            firePointStartLocalPosition,
            recoilReturnSpeed * Time.deltaTime);
    }

    private void ApplyConfig()
    {
        if (weaponConfig == null)
        {
            ApplyRunBoosters();
            return;
        }

        fireRate = weaponConfig.fireRate;
        bulletDamage = weaponConfig.bulletDamage;
        bulletsPerShot = weaponConfig.bulletsPerShot;
        spreadAngle = weaponConfig.spreadAngle;
        bulletBounces = weaponConfig.bulletBounces;
        bulletPierces = weaponConfig.bulletPierces;
        recoilDistance = weaponConfig.recoilDistance;
        recoilReturnSpeed = weaponConfig.recoilReturnSpeed;
        defaultCapacity = weaponConfig.defaultCapacity;
        maxPoolSize = weaponConfig.maxPoolSize;

        ApplyRunBoosters();
    }

    private void ApplyRunBoosters()
    {
        bulletDamage += RunProgress.StarterDamageBonus;
        fireRate = Mathf.Max(0.1f, fireRate - RunProgress.StarterFireRateReduction);
        bulletsPerShot = Mathf.Max(1, bulletsPerShot + RunProgress.ExtraBulletsBonus);
    }
}
