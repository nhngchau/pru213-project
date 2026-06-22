using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Shared behaviour + stat block for every Bug (GDD v3.0 - Section IV). One data-driven
/// component: each prefab sets its own stats in the Inspector.
///
///   Bug          HP    Speed  Damage   DataPack  Special
///   SyntaxError  20    4.0    10 HP/s  5         -
///   LogicBug     40    3.0    15 HP/s  10        zigzag (separate upcoming task)
///   MemoryLeak   150   1.5    30 HP/s  25        drops a Sludge pool on death
///
/// Implements IDamageable so Code Bullets can damage it without knowing its concrete type.
/// Pooled via EnemySpawner: it never destroys itself - on death it returns to its ObjectPool.
/// </summary>
public class EnemyBehavior : MonoBehaviour, IDamageable
{
    [Header("Stats (GDD v3.0 - set per Bug type in the prefab)")]
    [SerializeField] private int maxHP = 20;
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private int dataPackValue = 5;

    [Header("Contact Damage (HP/s)")]
    [Tooltip("Damage per tick. With Tick Interval = 1s this is exactly HP/s (Syntax 10, Logic 15, Memory Leak 30).")]
    [SerializeField] private int damagePerSecond = 10;
    [SerializeField] private float damageTickInterval = 1f;

    [Header("On Death (GDD v3.0 - Memory Leak only)")]
    [Tooltip("Effect spawned when this Bug dies. Memory Leak assigns the Sludge prefab; others leave it empty.")]
    [SerializeField] private GameObject onDeathEffectPrefab;

    private int currentHP;
    private ServerCore server;
    private Transform target;
    private float nextDamageTime;
    private bool isAttackingServer;

    private Rigidbody2D rb;
    private IObjectPool<EnemyBehavior> pool;
    private bool isReleased;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        server = FindFirstObjectByType<ServerCore>();
        // GDD AI: Bugs converge on the Server by default.
        target = server != null ? server.transform : null;
    }

    /// <summary>Called once by the pool's createFunc so Die() can return this Bug to the right pool.</summary>
    public void SetPool(IObjectPool<EnemyBehavior> objectPool)
    {
        pool = objectPool;
    }

    /// <summary>
    /// OnGet state reset (GDD - CRITICAL): called by EnemySpawner after positioning. Restores HP,
    /// clears velocity and AI flags so a recycled Bug behaves exactly like a freshly spawned one.
    /// </summary>
    public void ResetState()
    {
        isReleased = false;
        currentHP = maxHP;
        isAttackingServer = false;
        nextDamageTime = 0f;
        target = server != null ? server.transform : null;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Start()
    {
        currentHP = maxHP; // first-life init; every reuse goes through ResetState()
    }

    void OnEnable()
    {
        // Subscribe on activate / unsubscribe on deactivate. Pool Get() reactivates and Release()
        // deactivates, so these stay exactly balanced -> the Bug can never double-subscribe
        // (this is what prevents the OnPlayerDied event "memory leak" across pooling).
        PlayerEvents.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        PlayerEvents.OnPlayerDied -= HandlePlayerDied;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        if (target != null && !isAttackingServer)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            transform.Translate(direction * moveSpeed * Time.deltaTime);
        }
    }

    // GDD Section V: when the player goes down, every Bug commits 100% to the Server.
    private void HandlePlayerDied()
    {
        if (server != null)
        {
            target = server.transform;
        }
    }

    // --- IDamageable: taking damage from Code Bullets -------------------------

    // Base bullet damage is 10 (GDD), so a 20 HP Syntax Error needs 2 hits and a 150 HP
    // Memory Leak needs 15 - exactly as the stat table specifies.
    public void TakeDamage(int amount)
    {
        currentHP -= amount;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // GDD v3.0 - Memory Leak special: leave a Sludge pool behind. Data-driven, so this
        // class never needs to know it is "a Memory Leak" - it just drops whatever is assigned.
        if (onDeathEffectPrefab != null)
        {
            Instantiate(onDeathEffectPrefab, transform.position, Quaternion.identity);
        }

        // GDD v3.0 - DataPack economy: reward the kill (5 / 10 / 25), decoupled via the event hub.
        GameEvents.RaiseDataPackAwarded(dataPackValue);

        ReturnToPool();
    }

    // Guarded return path: isReleased blocks a double Release (e.g. two bullets landing in the
    // same frame), which would otherwise trip ObjectPool collectionCheck.
    private void ReturnToPool()
    {
        if (isReleased)
        {
            return;
        }

        isReleased = true;

        if (pool != null)
        {
            pool.Release(this);
        }
        else
        {
            Destroy(gameObject); // fallback if a Bug was ever spawned without a pool
        }
    }

    // --- Dealing contact damage (HP/s) to Player or Server --------------------

    private void OnTriggerStay2D(Collider2D other)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        // Decoupled + no friendly fire: hit any IDamageable that is not another Bug.
        if (!other.TryGetComponent(out IDamageable damageable) || damageable is EnemyBehavior)
        {
            return;
        }

        // Stop advancing once we are in contact with our goal, the Server.
        if (damageable is ServerCore)
        {
            isAttackingServer = true;
        }

        // Timer-gated tick = clean "HP per second" without applying damage every physics frame.
        if (Time.time >= nextDamageTime)
        {
            damageable.TakeDamage(damagePerSecond);
            nextDamageTime = Time.time + damageTickInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<ServerCore>() != null)
        {
            isAttackingServer = false;
        }
    }
}
