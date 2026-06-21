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
        if (collision.CompareTag("Enemy"))
        {
            EnemyBehavior enemy = collision.GetComponent<EnemyBehavior>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // TODO (next task - Object Pooling): return to pool instead of Destroy.
            Destroy(gameObject);
        }
    }
}
