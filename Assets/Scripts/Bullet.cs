using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats (GDD v3.0)")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifeTime = 2f; // auto-return to the pool after this long

    private Rigidbody2D rb;
    private IObjectPool<Bullet> pool;
    private bool isReleased;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Called once by the pool's createFunc so the bullet can return itself later.</summary>
    public void SetPool(IObjectPool<Bullet> objectPool)
    {
        pool = objectPool;
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

        ReturnToPool();
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
