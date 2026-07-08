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
    private int remainingBounces;
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

    public void SetModifiers(int bounces, int pierces)
    {
        remainingBounces = Mathf.Max(0, bounces);
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
        if (remainingBounces > 0 && IsBounceSurface(collision.gameObject))
        {
            Reflect(collision);
            return;
        }

        // Code Bullets only ever damage Bugs (GDD). Decoupled via IDamageable; the "Enemy" tag
        // gate keeps bullets from ever damaging the Player or the Server.
        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        if (collision.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damage);
        }

        if (remainingPierces > 0)
        {
            remainingPierces--;
            return;
        }

        ReturnToPool();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (remainingBounces <= 0 || !IsBounceSurface(collision.gameObject))
        {
            ReturnToPool();
            return;
        }

        Vector2 normal = collision.contactCount > 0 ? collision.GetContact(0).normal : -rb.linearVelocity.normalized;
        Reflect(normal);
    }

    private bool IsBounceSurface(GameObject other)
    {
        return other.CompareTag("Wall")
            || other.CompareTag("Obstacle")
            || other.layer == LayerMask.NameToLayer(bounceLayerName);
    }

    private void Reflect(Collider2D collision)
    {
        Vector2 closestPoint = collision.ClosestPoint(transform.position);
        Vector2 normal = ((Vector2)transform.position - closestPoint).normalized;

        if (normal.sqrMagnitude <= 0.001f)
        {
            normal = -rb.linearVelocity.normalized;
        }

        Reflect(normal);
    }

    private void Reflect(Vector2 normal)
    {
        remainingBounces--;
        Vector2 reflectedVelocity = Vector2.Reflect(rb.linearVelocity.normalized, normal) * speed;
        rb.linearVelocity = reflectedVelocity;
        transform.right = reflectedVelocity.normalized;
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
