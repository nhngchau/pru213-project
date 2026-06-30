using UnityEngine;

public class EnemyBehavior : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private int damageToServer = 10;
    [SerializeField] private int damageToPlayer = 10;   // damage dealt to the player on contact
    [SerializeField] private float damageInterval = 1f;

    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private float deathDelay = 0.6f;   // Time to wait for death animation before Destroy

    private ServerCore server;
    private float nextDamageTime;
    private bool isAttackingServer = false;
    private bool isDead = false;

    void Start()
    {
        server = FindFirstObjectByType<ServerCore>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
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
            transform.Translate(direction * moveSpeed * Time.deltaTime);
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

    /// <summary>
    /// Deal damage to this enemy. Currently one hit is lethal (matches bullet behaviour).
    /// Called by RefactorWave and any other source that needs to hurt the enemy directly.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        // One-hit kill for now; replace with an HP system here if needed.
        GameAudioManager.Instance?.PlayEnemyDefeated();
        Die();
    }

    private void Die()
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

        Destroy(gameObject, deathDelay);
    }

    /// <summary>
    /// Resets transient runtime state. Call this right after Instantiate (or when returning
    /// an enemy to an object pool) so it starts with a clean slate.
    /// </summary>
    public void ResetState()
    {
        isAttackingServer = false;
        isDead = false;
        nextDamageTime = 0f;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }



    private void OnTriggerStay2D(Collider2D collision)
    {
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
                touchedServer.TakeDamage(damageToServer);
                Die(); // Enemy plays death animation and disappears
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
                    player.TakeDamage(damageToPlayer);
                    nextDamageTime = Time.time + damageInterval;
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
