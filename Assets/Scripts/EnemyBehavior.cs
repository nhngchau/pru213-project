using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Shared behaviour + stat block for every Bug (GDD v3.0 - Section IV).
///
///   Bug          HP    Speed  Damage   DataPack
///   SyntaxError  20    4.0    10 HP/s  5
///   LogicBug     40    3.0    15 HP/s  10
///   MemoryLeak   150   1.5    30 HP/s  25   (drops a Sludge pool on death)
///
/// AI priority (GDD): chase + attack the Player while it is alive and within detectionRadius (forces
/// the player to keep moving); otherwise converge on the Server. Falls back to the Server while the
/// Player is down and re-acquires it on respawn.
/// Navigation: follows an A* path from PathfindingGrid so Bugs route AROUND furniture/walls to the
/// target (no more ramming into obstacles), with a collide-and-slide sweep as a close-range safety
/// net. Implements IDamageable (bullets) and is pooled.
/// </summary>
public class EnemyBehavior : MonoBehaviour, IDamageable
{
    [Header("Stats (GDD v3.0 - set per Bug type in the prefab)")]
    [SerializeField] private int maxHP = 20;
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private int dataPackValue = 5;

    [Header("Targeting")]
    [Tooltip("If the Player is alive and within this range, the Bug prioritises the Player over the Server.")]
    [SerializeField] private float detectionRadius = 4.5f;

    [Header("Pathfinding")]
    [SerializeField] private float repathInterval = 0.5f;        // how often to recompute the A* path
    [SerializeField] private float waypointReachDistance = 0.35f; // advance to next waypoint within this

    [Header("Obstacle Avoidance (close-range safety)")]
    [Tooltip("Layer(s) of the environment colliders (furniture / walls).")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float bodyRadius = 0.3f;
    [SerializeField] private float skinWidth = 0.02f;

    [Header("Contact Damage (HP/s)")]
    [Tooltip("Damage per tick. With Tick Interval = 1s this is exactly HP/s (Syntax 10, Logic 15, Memory Leak 30).")]
    [SerializeField] private int damagePerSecond = 10;
    [SerializeField] private float damageTickInterval = 1f;

    [Header("On Death (GDD v3.0 - Memory Leak only)")]
    [Tooltip("Effect spawned when this Bug dies. Memory Leak assigns the Sludge prefab; others leave it empty.")]
    [SerializeField] private GameObject onDeathEffectPrefab;

    private int currentHP;
    private float nextDamageTime;
    private bool isTouchingServer;

    private ServerCore server;
    private Transform serverTransform;
    private Collider2D serverCollider;
    private float serverAttackRadius = 1f;
    private Vector2 serverAttackOffset;
    private PlayerHealth player;
    private Transform playerTransform;
    private Transform target;

    private readonly List<Vector2> path = new List<Vector2>();
    private int pathIndex;
    private float nextRepathTime;
    private Transform lastPathTarget;

    private Rigidbody2D rb;
    private IObjectPool<EnemyBehavior> pool;
    private bool isReleased;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        server = FindFirstObjectByType<ServerCore>();
        serverTransform = server != null ? server.transform : null;
        serverCollider = server != null ? server.GetComponent<Collider2D>() : null;
        if (serverCollider != null)
        {
            // Radius of the ring around the Server that Bugs attack from (just inside its edge).
            float extent = Mathf.Max(serverCollider.bounds.extents.x, serverCollider.bounds.extents.y);
            serverAttackRadius = Mathf.Max(0.5f, extent * 0.85f);
        }

        player = FindFirstObjectByType<PlayerHealth>();
        playerTransform = player != null ? player.transform : null;

        target = serverTransform;
    }

    /// <summary>Called once by the pool's createFunc so Die() can return this Bug to the right pool.</summary>
    public void SetPool(IObjectPool<EnemyBehavior> objectPool) => pool = objectPool;

    /// <summary>OnGet state reset (pooling): a recycled Bug behaves exactly like a fresh spawn.</summary>
    public void ResetState()
    {
        isReleased = false;
        currentHP = maxHP;
        isTouchingServer = false;
        nextDamageTime = 0f;
        target = serverTransform;

        path.Clear();
        pathIndex = 0;
        lastPathTarget = null;
        nextRepathTime = 0f; // force a fresh path on the first Update after spawn

        // Each Bug attacks the Server from its OWN random point on a ring around it, so they spread
        // around the Server instead of all funnelling onto the single nearest cell of its centre.
        float attackAngle = Random.value * Mathf.PI * 2f;
        serverAttackOffset = new Vector2(Mathf.Cos(attackAngle), Mathf.Sin(attackAngle)) * serverAttackRadius;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Start()
    {
        currentHP = maxHP; // first-life init; every reuse goes through ResetState()
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        target = ChooseTarget();
        if (target == null)
        {
            return;
        }

        // Freeze only while drilling the stationary Server; when chasing the Player we keep following.
        if (target == serverTransform && isTouchingServer)
        {
            return;
        }

        // Recompute the route when the target kind changes or the refresh timer elapses. The Server is
        // static so its path stays valid between refreshes; the Player path refreshes as it moves.
        if (target != lastPathTarget || Time.time >= nextRepathTime)
        {
            RecomputePath();
        }

        Vector2 waypoint = CurrentWaypoint();
        Vector2 toWaypoint = waypoint - (Vector2)transform.position;
        if (toWaypoint.sqrMagnitude > 0.000001f)
        {
            Vector2 step = toWaypoint.normalized * (moveSpeed * Time.deltaTime);
            transform.Translate(CollideAndSlide(step), Space.World);
        }

        // Advance along the path once the current waypoint is reached.
        if (pathIndex < path.Count &&
            Vector2.Distance(transform.position, path[pathIndex]) <= waypointReachDistance)
        {
            pathIndex++;
        }
    }

    // GDD: prioritise the Player when alive and nearby; otherwise the Server. Polled every frame so
    // it works for pooled Bugs and reacts to the Player dying / respawning without extra plumbing.
    private Transform ChooseTarget()
    {
        bool playerInRange = playerTransform != null
            && player != null && !player.IsDown
            && Vector2.Distance(transform.position, playerTransform.position) <= detectionRadius;

        return playerInRange ? playerTransform : serverTransform;
    }

    private void RecomputePath()
    {
        lastPathTarget = target;
        nextRepathTime = Time.time + repathInterval;
        path.Clear();
        pathIndex = 0;

        PathfindingGrid grid = PathfindingGrid.GetOrCreate();
        if (grid != null && grid.IsReady)
        {
            grid.FindPath(transform.position, GoalPosition(), path);
        }
        // If no grid / no path, 'path' stays empty -> CurrentWaypoint() heads straight at the target.
    }

    private Vector2 CurrentWaypoint()
    {
        if (pathIndex < path.Count)
        {
            return path[pathIndex];
        }
        return GoalPosition(); // final approach, or fallback when there is no path
    }

    // Where this Bug is actually heading: its own offset point on the ring around the Server (so Bugs
    // surround it instead of stacking on one spot), or the Player's position when chasing the Player.
    private Vector2 GoalPosition()
    {
        if (target == serverTransform && serverTransform != null)
        {
            return (Vector2)serverTransform.position + serverAttackOffset;
        }
        return target != null ? (Vector2)target.position : (Vector2)transform.position;
    }

    // Kinematic "collide & slide" close-range safety: stops at a surface and slides along it, so the
    // Bug never clips through even between coarse A* waypoints. CircleCast is a continuous sweep.
    private Vector2 CollideAndSlide(Vector2 move)
    {
        if (obstacleMask.value == 0)
        {
            return move;
        }

        float distance = move.magnitude;
        if (distance < 0.0001f)
        {
            return Vector2.zero;
        }

        Vector2 dir = move / distance;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, bodyRadius, dir, distance + skinWidth, obstacleMask);
        if (hit.collider == null)
        {
            return move;
        }

        // The Server is a solid obstacle now (the Player can't pass through it, and Bugs route around it
        // when chasing the Player). But when a Bug is actually TARGETING the Server, let it push straight
        // in so it can reach + attack it (its trigger then fires OnTriggerStay2D -> damage, then freeze).
        if (target == serverTransform && hit.collider == serverCollider)
        {
            return move;
        }

        Vector2 toContact = dir * Mathf.Max(0f, hit.distance - skinWidth);
        Vector2 remaining = move - toContact;
        Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
        Vector2 slide = tangent * Vector2.Dot(remaining, tangent);

        if (slide.sqrMagnitude > 0.0001f)
        {
            float slideDist = slide.magnitude;
            Vector2 slideDir = slide / slideDist;
            Vector2 origin = (Vector2)transform.position + toContact;
            RaycastHit2D slideHit = Physics2D.CircleCast(origin, bodyRadius, slideDir, slideDist + skinWidth, obstacleMask);
            if (slideHit.collider != null)
            {
                slide = slideDir * Mathf.Max(0f, slideHit.distance - skinWidth);
            }
        }

        return toContact + slide;
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
        // GDD v3.0 - Memory Leak special: drop whatever onDeath effect is assigned (Sludge).
        if (onDeathEffectPrefab != null)
        {
            Instantiate(onDeathEffectPrefab, transform.position, Quaternion.identity);
        }

        // GDD v3.0 - DataPack economy: reward the kill (5 / 10 / 25), decoupled via the event hub.
        GameEvents.RaiseDataPackAwarded(dataPackValue);

        // Audio: a Bug was defeated (plays on every death, before it returns to the pool).
        GameAudioManager.Instance?.PlayEnemyDefeated();

        ReturnToPool();
    }

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

        // Touching the Server -> stop advancing and drill it (handled in Update).
        if (damageable is ServerCore)
        {
            isTouchingServer = true;
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
            isTouchingServer = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}