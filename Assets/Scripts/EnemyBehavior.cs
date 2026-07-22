using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyBehavior : MonoBehaviour, IDamageable
{
    [Header("Config")]
    [SerializeField] private EnemyConfig config;

    [Header("Animation")]
    [SerializeField] private Animator anim;

    private ServerCore server;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private float nextAttackTime;
    private bool isDead = false;
    private int currentHP;
    private IObjectPool<EnemyBehavior> pool;
    private Coroutine releaseRoutine;
    private bool loggedMissingConfig;
    private bool loggedMissingProjectile;
    private bool isCountedAsBlocker;

    // Hash sẵn tên parameter: rẻ hơn bản SetFloat(string) vốn phải băm lại tên mỗi lần gọi.
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimHorizontal = Animator.StringToHash("Horizontal");
    private static readonly int AnimVertical = Animator.StringToHash("Vertical");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    private bool hasSpeedParam;
    private bool hasDirectionParams;
    private bool hasDieParam;

    /// <summary>
    /// Số bug đang chặn Build Progress còn sống. WaveManager đọc giá trị này để dừng tiến độ build.
    /// Dùng static counter thay vì quét scene mỗi frame vì Update của WaveManager chạy liên tục.
    /// </summary>
    public static int ActiveBlockerCount { get; private set; }

    // Static không tự về 0 khi vào lại Play Mode nếu Domain Reload bị tắt, và cũng không reset khi
    // load lại scene. Reset cả hai đường: lúc khởi động runtime và lúc WaveManager bắt đầu stage.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnLoad() => ActiveBlockerCount = 0;

    public static void ResetBlockerCount() => ActiveBlockerCount = 0;

    void Awake()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();
        ValidateConfig();
    }

    void Start()
    {
        server = FindFirstObjectByType<ServerCore>();
        CachePlayer();
        ResetState();
    }

    void Update()
    {
        if (!ValidateConfig())
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        if (isDead)
        {
            return; // Stop moving when dead
        }

        // Mỗi archetype tạo một kiểu áp lực khác nhau — xem enum EnemyArchetype.
        Vector2 direction = config.archetype switch
        {
            EnemyArchetype.Hunter => TickHunter(),
            EnemyArchetype.Ranged => TickRanged(),
            _ => TickChaser(),
        };

        // Send directional info to Animator
        if (anim != null)
        {
            if (hasSpeedParam)
            {
                anim.SetFloat(AnimSpeed, direction.sqrMagnitude);
            }

            if (hasDirectionParams && direction != Vector2.zero)
            {
                anim.SetFloat(AnimHorizontal, direction.x);
                anim.SetFloat(AnimVertical, direction.y);
            }
        }
    }

    /// <summary>
    /// Ghi nhớ Animator của prefab này thực sự có những parameter nào.
    ///
    /// Gọi SetFloat/SetTrigger với tên parameter không tồn tại KHÔNG làm hỏng gameplay, nhưng Unity
    /// ghi một warning kèm nguyên stack trace cho mỗi lần gọi. Ở đây là 3 lần mỗi frame mỗi con quái
    /// — với vài chục con trên sân là hàng nghìn dòng log mỗi giây, đủ để thổi Editor.log lên cỡ GB
    /// và làm Unity Editor ném OutOfMemoryException khi nó index file log đó.
    /// </summary>
    private void CacheAnimatorParameters()
    {
        hasSpeedParam = false;
        hasDirectionParams = false;
        hasDieParam = false;

        // Không có controller thì truy cập .parameters là vô nghĩa (trả về mảng rỗng).
        if (anim == null || anim.runtimeAnimatorController == null)
        {
            return;
        }

        bool hasHorizontal = false;
        bool hasVertical = false;

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.nameHash == AnimSpeed) hasSpeedParam = true;
            else if (parameter.nameHash == AnimHorizontal) hasHorizontal = true;
            else if (parameter.nameHash == AnimVertical) hasVertical = true;
            else if (parameter.nameHash == AnimDie) hasDieParam = true;
        }

        // Hai tham số hướng luôn được set cùng nhau nên gộp thành một cờ.
        hasDirectionParams = hasHorizontal && hasVertical;
    }

    // --- AI theo archetype ------------------------------------------------
    // Mỗi Tick* trả về hướng vừa di chuyển (Vector2.zero nếu đứng yên) để Animator dùng.

    /// <summary>Hành vi gốc: lao thẳng vào Server. Chạm tới nơi là tự huỷ (xem OnTriggerStay2D).</summary>
    private Vector2 TickChaser()
    {
        if (server == null)
        {
            return Vector2.zero;
        }

        return MoveToward(server.transform.position);
    }

    /// <summary>Đuổi theo Player. Khi Player đang downtime thì quay về đánh Server.</summary>
    private Vector2 TickHunter()
    {
        Transform target = GetLivePlayer();
        return target != null ? MoveToward(target.position) : TickChaser();
    }

    /// <summary>
    /// Giữ khoảng cách preferredRange với Player rồi bắn: quá xa thì tiến lại, quá gần thì lùi ra.
    /// Nhờ vậy áp sát là cách xử lý đúng, còn đứng yên bắn từ xa sẽ bị bắn trả.
    /// </summary>
    private Vector2 TickRanged()
    {
        Transform target = GetLivePlayer();
        if (target == null)
        {
            return TickChaser(); // không có ai để quấy rối -> quay về đánh Server
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (Time.time >= nextAttackTime && distance <= config.preferredRange * 1.25f)
        {
            FireAt(toTarget);
            nextAttackTime = Time.time + config.attackInterval;
        }

        if (distance > config.preferredRange)
        {
            return MoveToward(target.position);
        }

        if (distance < config.preferredRange * 0.6f)
        {
            return MoveInDirection(-toTarget); // bị áp sát -> lùi ra
        }

        return Vector2.zero; // đang ở đúng tầm -> đứng lại bắn
    }

    private Vector2 MoveToward(Vector3 targetPosition)
    {
        return MoveInDirection(targetPosition - transform.position);
    }

    private Vector2 MoveInDirection(Vector2 rawDirection)
    {
        if (rawDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        Vector2 direction = rawDirection.normalized;
        transform.Translate(direction * config.moveSpeed * Time.deltaTime, Space.World);
        return direction;
    }

    private void FireAt(Vector2 aimDirection)
    {
        EnemyProjectile projectile;

        if (config.projectilePrefab != null)
        {
            GameObject spawned = Instantiate(config.projectilePrefab, transform.position, Quaternion.identity);
            if (!spawned.TryGetComponent(out projectile))
            {
                if (!loggedMissingProjectile)
                {
                    Debug.LogError($"EnemyConfig '{config.name}': projectilePrefab thiếu component EnemyProjectile.");
                    loggedMissingProjectile = true;
                }

                Destroy(spawned);
                return;
            }
        }
        else
        {
            // Chưa có prefab riêng -> dựng viên đạn tối giản để tính năng vẫn chạy được ngay.
            projectile = EnemyProjectile.CreateDefault(transform.position, new Color(1f, 0.45f, 0.3f));
        }

        projectile.Launch(aimDirection, GetScaledDamage(config.projectileDamage), config.projectileSpeed);
    }

    /// <summary>Player nếu đang sống; null khi chưa tìm thấy hoặc đang trong downtime.</summary>
    private Transform GetLivePlayer()
    {
        if (playerTransform == null || (playerHealth != null && playerHealth.IsDown))
        {
            return null;
        }

        return playerTransform;
    }

    // --- Chặn Build Progress ----------------------------------------------
    // ResetState() bị gọi hai lần ở lần spawn đầu tiên (một lần từ EnemySpawner ngay sau pool.Get(),
    // một lần từ Start()), nên cả hai hàm dưới đây phải idempotent — nếu không bộ đếm sẽ lệch dần
    // và build bị khoá vĩnh viễn.

    private void RegisterBlocker()
    {
        if (isCountedAsBlocker || config == null || !config.blocksBuildProgress)
        {
            return;
        }

        isCountedAsBlocker = true;
        ActiveBlockerCount++;
        RaiseBlockedChanged();
    }

    private void UnregisterBlocker()
    {
        if (!isCountedAsBlocker)
        {
            return;
        }

        isCountedAsBlocker = false;
        ActiveBlockerCount = Mathf.Max(0, ActiveBlockerCount - 1);
        RaiseBlockedChanged();
    }

    private void RaiseBlockedChanged()
    {
        string bugName = config != null && !string.IsNullOrEmpty(config.displayName) ? config.displayName : "BUG";
        GameEvents.RaiseBuildBlockedChanged(ActiveBlockerCount, bugName);
    }

    private void OnDisable()
    {
        // Bắt cả đường trả về pool lẫn đường unload scene.
        UnregisterBlocker();
    }

    private void CachePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        playerTransform = playerObject.transform;
        playerObject.TryGetComponent(out playerHealth);
    }

    public void TakeDamage(int damage)
    {
        if (!ValidateConfig())
        {
            return;
        }

        if (isDead) return;

        currentHP -= Mathf.Max(0, damage);
        
        // --- ADDED: Raise damage taken event for EffectManager ---
        GameEvents.RaiseDamageTaken(transform.position, damage);

        if (currentHP <= 0)
        {
            GameAudioManager.Instance?.PlayEnemyDefeated();
            Die(true);
        }
    }

    private void Die(bool awardReward)
    {
        if (isDead) return;
        isDead = true;

        // Nhả khoá ngay khi chết, không đợi hết deathDelay — người chơi phải thấy build chạy lại
        // đúng khoảnh khắc viên đạn trúng đích thì phản hồi mới đã.
        UnregisterBlocker();

        if (anim != null && hasDieParam)
        {
            anim.SetTrigger(AnimDie);
        }

        // Disable collider so bullets and the player pass through the corpse
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (awardReward && config.dataPackValue > 0)
        {
            GameEvents.RaiseDataPackAwarded(config.dataPackValue);
        }

        if (awardReward && config.expReward > 0)
        {
            GameEvents.RaiseExpAwarded(config.expReward);
        }

        // --- ADDED: Raise enemy died event for EffectManager ---
        GameEvents.RaiseEnemyDied(transform.position);

        if (config.onDeathEffectPrefab != null)
        {
            SpawnDeathEffect();
        }

        // Vũng nguy hiểm (Sludge) chỉ để lại khi bị bắn chết, không phải khi enemy tự lao vào
        // Player/Server — nếu không thì người chơi vừa ăn damage vừa bị dính vũng ngay dưới chân.
        if (awardReward && config.onDeathHazardPrefab != null)
        {
            Instantiate(config.onDeathHazardPrefab, transform.position, Quaternion.identity);
        }

        releaseRoutine = StartCoroutine(ReleaseAfterDeath());
    }

    public void SetPool(IObjectPool<EnemyBehavior> objectPool)
    {
        pool = objectPool;
    }

    /// <summary>
    /// Resets transient runtime state. Call this right after Instantiate (or when returning
    /// an enemy to an object pool) so it starts with a clean slate.
    /// </summary>
    public void ResetState()
    {
        if (!ValidateConfig())
        {
            return;
        }

        if (releaseRoutine != null)
        {
            StopCoroutine(releaseRoutine);
            releaseRoutine = null;
        }

        isDead = false;
        currentHP = Mathf.Max(1, Mathf.RoundToInt(config.maxHP * RunProgress.EnemyHealthMultiplier));
        nextAttackTime = Time.time + config.attackInterval; // không bắn ngay lúc vừa spawn

        RegisterBlocker();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (anim != null && hasDieParam)
        {
            anim.ResetTrigger(AnimDie);
        }
    }

    private bool ValidateConfig()
    {
        if (config != null)
        {
            return true;
        }

        if (!loggedMissingConfig)
        {
            Debug.LogError($"EnemyBehavior on '{name}' is missing an EnemyConfig. Assign one on the prefab.");
            loggedMissingConfig = true;
        }

        return false;
    }

    private IEnumerator ReleaseAfterDeath()
    {
        yield return new WaitForSeconds(config.deathDelay);
        releaseRoutine = null;

        if (pool != null)
        {
            pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SpawnDeathEffect()
    {
        PooledEffectPool.Spawn(config.onDeathEffectPrefab, transform.position, Quaternion.identity);
    }



    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!ValidateConfig())
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        // Xác chết vẫn nằm trên sân cho tới hết deathDelay, mà OnTriggerStay2D chạy mỗi frame cho
        // từng collider đang chạm — thiếu guard này thì một con vừa chết vì đâm Server sẽ đâm thêm
        // cả Player ở frame sau.
        if (isDead)
        {
            return;
        }

        // Chạm là gây damage rồi tự huỷ ngay, không có cooldown: mọi enemy đều là một phát đánh đổi
        // mạng lấy máu. Vì thế enemy áp sát được Server luôn là mất máu thật, và người chơi không thể
        // đứng yên "tank" bằng cách để quái bám vào mình.
        ServerCore touchedServer = collision.GetComponent<ServerCore>();
        if (touchedServer != null)
        {
            touchedServer.TakeDamage(GetScaledDamage(config.damageToServer));
            Die(false);
            return;
        }

        // Dùng IDamageable để không phải tham chiếu cứng tới PlayerHealth.
        if (collision.CompareTag("Player") && collision.TryGetComponent(out IDamageable player))
        {
            player.TakeDamage(GetScaledDamage(config.damageToPlayer));
            Die(false);
        }
    }

    private static int GetScaledDamage(int baseDamage)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseDamage * RunProgress.EnemyDamageMultiplier));
    }
}
