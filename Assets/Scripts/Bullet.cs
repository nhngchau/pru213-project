using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 15f;

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        // Bắn thẳng về phía trước mặt của viên đạn
        rb.linearVelocity = transform.right * speed;

        // Code chống "Memory Leak": Hủy viên đạn sau 2 giây nếu không trúng ai
        // Giúp server (RAM của máy bạn) không bị quá tải bởi hàng ngàn viên đạn rác
        Destroy(gameObject, 2f);
    }

    // Phần này để dành cho Tuần 4 khi quái vật xuất hiện
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Gây sát thương cho Enemy
            Destroy(gameObject); // Chạm trúng thì viên đạn cũng biến mất
        }
    }
}