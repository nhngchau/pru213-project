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
    private float nextDamageTime;
    private bool isAttackingServer = false;
    private bool isDead = false;
    private int currentHP;
    private IObjectPool<EnemyBehavior> pool;
    private Coroutine releaseRoutine;
    private bool loggedMissingConfig;

    void Awake()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        ValidateConfig();
    }

    void Start()
    {
        server = FindFirstObjectByType<ServerCore>();
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

        Vector2 direction = Vector2.zero;

        if (server != null && !isAttackingServer)
        {
            direction = (server.transform.position - transform.position).normalized;
            transform.Translate(direction * config.moveSpeed * Time.deltaTime);
        }

        // Send directional info to Animator
        if (anim != null)
        {
            anim.SetFloat("Speed", direction.sqrMagnitude);
            if (direction != Vector2.zero)
            {
                anim.SetFloat("Horizontal", direction.x);
                anim.SetFloat("Vertical", direction.y);
            }
        }
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

        if (anim != null)
        {
            anim.SetTrigger("Die");
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

        isAttackingServer = false;
        isDead = false;
        currentHP = Mathf.Max(1, Mathf.RoundToInt(config.maxHP * RunProgress.EnemyHealthMultiplier));
        nextDamageTime = 0f;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (anim != null)
        {
            anim.ResetTrigger("Die");
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

        // --- Damage the Server on contact ---
        ServerCore touchedServer = collision.GetComponent<ServerCore>();
        if (touchedServer != null)
        {
            isAttackingServer = true;

            if (Time.time >= nextDamageTime)
            {
                touchedServer.TakeDamage(config.damageToServer);
                Die(false); // Enemy plays death animation and disappears
            }
        }

        // --- Damage the Player on contact ---
        // Uses IDamageable so it works with PlayerHealth without a hard reference.
        if (collision.CompareTag("Player"))
        {
            if (Time.time >= nextDamageTime)
            {
                if (collision.TryGetComponent(out IDamageable player))
                {
                    player.TakeDamage(config.damageToPlayer);
                    nextDamageTime = Time.time + config.damageInterval;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ServerCore>() != null)
        {
            isAttackingServer = false;
        }
    }
}
