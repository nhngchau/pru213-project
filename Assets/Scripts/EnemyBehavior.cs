using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private int damageToServer = 10;
    [SerializeField] private float damageInterval = 1f;

    private ServerCore server;
    private float nextDamageTime;
    private bool isAttackingServer = false;

    void Start()
    {
        server = FindFirstObjectByType<ServerCore>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        if (server != null && !isAttackingServer)
        {
            Vector2 direction = (server.transform.position - transform.position).normalized;
            transform.Translate(direction * moveSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
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