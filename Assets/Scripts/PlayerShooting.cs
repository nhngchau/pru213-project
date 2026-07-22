using UnityEngine;
using UnityEngine.Pool;

public class PlayerShooting : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponConfig weaponConfig;

    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Gun Visual")]
    [SerializeField] private Transform gunPivot;
    [SerializeField] private Transform playerVisual; // Object chứa hình ảnh nhân vật

    [Header("Weapon Stats (GDD v3.0 - tuned by UpgradeManager)")]
    [SerializeField] private float fireRate = 0.15f;        // Fire cooldown in seconds. Upgrade RAM lowers this (floor 0.1).
    [SerializeField] private int bulletDamage = 10;         // Damage per bullet. Overclock CPU raises this (+5 / level).
    [SerializeField] private int bulletsPerShot = 1;        // How many bullets fire at once. Upgrades can raise this to multi-shot.
    [SerializeField] private float spreadAngle = 15f;       // Half-angle of the bullet fan in degrees. Bullets are spread evenly across [-spreadAngle, +spreadAngle].
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
    private Vector2 lastAimDirection = Vector2.right;

    void Awake()
    {
        ApplyConfig();

        if (bulletPrefab == null || !bulletPrefab.TryGetComponent(out Bullet _))
        {
            Debug.LogError("PlayerShooting: Bullet Prefab must be assigned and contain a Bullet component.");
            enabled = false;
            return;
        }

        ResolveGunPivot();
        ResolvePlayerVisual();
        AimGunAtCursor();
        CacheFirePointRestPosition();

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
    public void SetBulletPierces(int value) => bulletPierces = Mathf.Max(0, value);
    public float FireRate => fireRate;
    public int BulletDamage => bulletDamage;
    public int BulletsPerShot => bulletsPerShot;
    public int BulletPierces => bulletPierces;

    void Update()
    {
        AimGunAtCursor();
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
        Vector2 lookDirection = GetAimDirection();
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
            bullet.SetModifiers(bulletPierces);
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

    private void ResolveGunPivot()
    {
        if (gunPivot == null)
        {
            gunPivot = FindGunChild(transform);
        }
    }

    private void ResolvePlayerVisual()
    {
        if (playerVisual == null)
        {
            // Tự động tìm object tên PlayerVisual theo hình bạn chụp
            Transform visual = transform.Find("PlayerVisual");
            if (visual != null)
            {
                playerVisual = visual;
            }
        }
    }

    private void AimGunAtCursor()
    {
        Vector2 aimDirection = GetAimDirection();
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        bool isAimingLeft = Mathf.Abs(angle) > 90f; // Nhắm sang trái khi góc > 90 hoặc < -90

        if (gunPivot != null)
        {
            gunPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            // Lật súng lại để không bị chổng ngược
            gunPivot.localScale = new Vector3(gunPivot.localScale.x, isAimingLeft ? -1f : 1f, gunPivot.localScale.z);
        }

        if (playerVisual != null)
        {
            // Lật mặt nhân vật sang trái/phải tương ứng
            playerVisual.localScale = new Vector3(isAimingLeft ? -1f : 1f, playerVisual.localScale.y, playerVisual.localScale.z);
        }
    }

    private static Transform FindGunChild(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            string childName = child.name.ToLowerInvariant();
            if (childName.Contains("gun") || childName.Contains("weapon"))
            {
                return child;
            }

            Transform nested = FindGunChild(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private Vector2 GetAimDirection()
    {
        if (Camera.main == null)
        {
            return lastAimDirection;
        }

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aimDirection = mousePosition - transform.position;
        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            lastAimDirection = aimDirection.normalized;
        }

        return lastAimDirection;
    }

    private void CacheFirePointRestPosition()
    {
        if (firePoint != null)
        {
            firePointStartLocalPosition = firePoint.localPosition;
        }
    }

    private void ApplyConfig()
    {
        if (WeaponManager.Instance != null)
        {
            var equipped = WeaponManager.Instance.GetEquippedWeapon();
            if (equipped != null) weaponConfig = equipped;
        }

        if (weaponConfig == null)
        {
            ApplyRunBoosters();
            return;
        }

        // Apply sprite to gunPivot if it exists
        if (weaponConfig.weaponSprite != null && gunPivot != null)
        {
            if (gunPivot.TryGetComponent(out SpriteRenderer sr))
            {
                sr.sprite = weaponConfig.weaponSprite;
            }
            else
            {
                // check children for sprite renderer
                var childSr = gunPivot.GetComponentInChildren<SpriteRenderer>();
                if (childSr != null) childSr.sprite = weaponConfig.weaponSprite;
            }
        }

        fireRate = weaponConfig.fireRate;
        bulletDamage = weaponConfig.bulletDamage;
        bulletsPerShot = weaponConfig.bulletsPerShot;
        spreadAngle = weaponConfig.spreadAngle;
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
