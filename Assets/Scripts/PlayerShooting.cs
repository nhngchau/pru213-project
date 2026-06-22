using UnityEngine;
using UnityEngine.Pool;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Fire Rate (GDD v3.0)")]
    [SerializeField] private float fireRate = 0.5f;

    [Header("Bullet Pool")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 100;

    private float nextFireTime;
    private IObjectPool<Bullet> bulletPool;

    void Awake()
    {
        // Built-in Unity pool: createFunc builds a bullet, get/release toggle active state,
        // destroy trims the surplus past maxPoolSize. collectionCheck catches double-release.
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
        bullet.SetPool(bulletPool); // bulletPool is already assigned by the time the first Get runs
        return bullet;
    }

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
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDirection = mousePos - transform.position;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        // Pull from the pool instead of Instantiate, place it, then launch (resets velocity + lifetime).
        Bullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, 0f, angle));
        bullet.Launch();
    }
}
