using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats (GDD v3.0)")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifeTime = 2f; // auto-return to the pool after this long
    [SerializeField] private string bounceLayerName = "Wall";

    private Rigidbody2D rb;
    private IObjectPool<Bullet> pool;
    private bool isReleased;
    private float explosionRadius;
    private int remainingPierces;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Called once by the pool's createFunc so the bullet can return itself later.</summary>
    public void SetPool(IObjectPool<Bullet> objectPool)
    {
        pool = objectPool;
    }

    /// <summary>Set per shot by PlayerShooting so Overclock CPU upgrades affect pooled bullets.</summary>
    public void SetDamage(int value)
    {
        damage = value;
    }

    public void SetModifiers(float explosiveRad, int pierces)
    {
        explosionRadius = Mathf.Max(0f, explosiveRad);
        remainingPierces = Mathf.Max(0, pierces);
    }

    /// <summary>
    /// OnGet state reset (GDD): called by PlayerShooting right after positioning. Fires the bullet
    /// and (re)starts its lifetime. Safe to call on every reuse - Start() no longer drives this.
    /// </summary>
    public void Launch()
    {
        isReleased = false;
        rb.linearVelocity = transform.right * speed;

        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Hit enemy logic
        if (!collision.CompareTag("Enemy"))
        {
            // If it hits a wall/obstacle, it should just explode or disappear
            if (IsBounceSurface(collision.gameObject))
            {
                Explode();
                ReturnToPool();
            }
            return;
        }

        // Direct damage to the hit enemy
        if (collision.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damage);
        }

        // Explode if we have explosive radius
        if (explosionRadius > 0f)
        {
            Explode();
        }

        // Piercing logic
        if (remainingPierces > 0)
        {
            remainingPierces--;
            return;
        }

        ReturnToPool();
    }

    private void Explode()
    {
        if (explosionRadius <= 0f) return;

        // Deal AoE damage to all nearby enemies (excluding the one we just hit directly, though it's fine if they take double damage from explosion, let's just do full AoE)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in hitEnemies)
        {
            if (col.CompareTag("Enemy") && col.TryGetComponent(out IDamageable target))
            {
                // Deal explosion damage (could be full damage or partial, let's do full)
                target.TakeDamage(damage);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsBounceSurface(collision.gameObject))
        {
            Explode();
            ReturnToPool();
        }
    }

    private bool IsBounceSurface(GameObject other)
    {
        return other.CompareTag("Wall")
            || other.CompareTag("Obstacle")
            || other.layer == LayerMask.NameToLayer(bounceLayerName);
    }

    /// <summary>
    /// Single guarded return path. The isReleased flag prevents a double Release (e.g. a hit and
    /// the lifetime timeout in the same frame), which would otherwise trip ObjectPool collectionCheck.
    /// </summary>
    private void ReturnToPool()
    {
        if (isReleased)
        {
            return;
        }

        isReleased = true;
        CancelInvoke();
        rb.linearVelocity = Vector2.zero; // dormant bullets carry no leftover velocity

        if (pool != null)
        {
            pool.Release(this);
        }
        else
        {
            Destroy(gameObject); // fallback if a bullet was ever spawned without a pool
        }
    }
}
