using UnityEngine;

public class Bullet : MonoBehaviour
{
    // GDD v3.0 - Projectile Speed 10.0 units/s | Base Damage 10 (Overclock CPU base).
    [Header("Bullet Stats (GDD v3.0)")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.right * speed;

        // TODO (next task - Object Pooling): replace timed Destroy with a return-to-pool timer.
        Destroy(gameObject, 2f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Code Bullets only ever damage Bugs (GDD v3.0). Decoupled: we hit any IDamageable
        // tagged "Enemy" without knowing its concrete Bug type. The tag gate keeps bullets
        // from ever damaging the Player or the Server.
        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        if (collision.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damage);
        }

        // TODO (next task - Object Pooling): return to pool instead of Destroy.
        Destroy(gameObject);
    }
}
