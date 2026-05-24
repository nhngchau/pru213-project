using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float moveSpeed = 3.5f; // Tốc độ chạy

    private Transform player;

    void Start()
    {
        // 1. Tự động tìm Senior Developer trên bản đồ thông qua Tag "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {
        if (player != null)
        {
            // 2. Tính toán hướng đi từ Quái đến Player
            Vector2 direction = (player.position - transform.position).normalized;

            // 3. Di chuyển quái vật đuổi theo
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
}