using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    // GDD v3.0 - Left click is HOLD-to-fire continuously. Base Rate 0.5s/shot (Upgrade RAM floor 0.1s).
    [Header("Fire Rate (GDD v3.0)")]
    [SerializeField] private float fireRate = 0.5f;

    private float nextFireTime;

    void Update()
    {
        // Hold-to-fire (GetButton), gated by the fire-rate cooldown.
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

        // TODO (next task - Object Pooling): GDD mandates pooling. Replace Instantiate with a
        // pool.Get(spawnPosition, rotation) call once the BulletPool is implemented.
        Instantiate(bulletPrefab, spawnPosition, Quaternion.Euler(0, 0, angle));
    }
}
