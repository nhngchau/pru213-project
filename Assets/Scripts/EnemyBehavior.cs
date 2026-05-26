using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float moveSpeed = 3.5f; // Tốc độ chạy
    [SerializeField] private int damageToServer = 10;
    [SerializeField] private float damageInterval = 1f;

    private ServerCore server;
    private float nextDamageTime;

    void Start()
    {
        // Tự động tìm Central Server trong scene.
        server = FindFirstObjectByType<ServerCore>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        if (server != null)
        {
            // Tính hướng đi từ Enemy đến Server.
            Vector2 direction = (server.transform.position - transform.position).normalized;

            // Di chuyển enemy về phía Server.
            transform.Translate(direction * moveSpeed * Time.deltaTime);
        }
    }

    // 4. Xử lý va chạm: Bị trúng đạn thì chết
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Nếu chạm vào object có gắn tag "Bullet"
        if (collision.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject); // Phá hủy viên đạn
            Destroy(gameObject);           // Phá hủy chính con quái này (Bug Fixed!)
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        ServerCore touchedServer = collision.GetComponent<ServerCore>();

        if (touchedServer != null && Time.time >= nextDamageTime)
        {
            touchedServer.TakeDamage(damageToServer);
            nextDamageTime = Time.time + damageInterval;
        }
    }
}
