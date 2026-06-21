using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    // GDD v3.0 - Bug Type: Syntax Error (the only Bug currently in the project).
    // HP 20 | Speed 4.0 | Damage 10 HP/s | Data Pack 5.
    // Logic Bug / Memory Leak variants and behaviors are a separate upcoming task.
    [Header("Enemy Stats (GDD v3.0 - Syntax Error)")]
    [SerializeField] private int maxHP = 20;
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private int damageToServer = 10;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private int dataPackValue = 5;

    private int currentHP;
    private ServerCore server;
    private Transform target;
    private float nextDamageTime;
    private bool isAttackingServer;

    void Awake()
    {
        server = FindFirstObjectByType<ServerCore>();
        // GDD AI: Bugs converge on the Server by default.
        target = server != null ? server.transform : null;
    }

    void Start()
    {
        currentHP = maxHP;
    }

    void OnEnable()
    {
        // Decoupled: react to the player's death via the event hub, never via PlayerController.
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

    // Damage is now applied by the Bullet (GDD base damage 10), not a one-shot kill.
    // A 20 HP Syntax Error therefore requires 2 base hits, as specified by the GDD stats.
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
        // TODO (next task - Data Pack economy): award dataPackValue (5) on death.
        // TODO (next task - Object Pooling): return this Bug to the pool instead of Destroy.
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        ServerCore touchedServer = collision.GetComponent<ServerCore>();

        if (touchedServer != null)
        {
            isAttackingServer = true;

            if (Time.time >= nextDamageTime)
            {
                touchedServer.TakeDamage(damageToServer);
                nextDamageTime = Time.time + damageInterval;
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
