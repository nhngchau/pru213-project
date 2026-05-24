using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim; // 1. Khai báo Animator

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // 2. Lấy component Animator
    }

    void Update()
    {
        // Nhận input di chuyển
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        // 3. Truyền tốc độ vào Animator (dùng sqrMagnitude để tính toán nhanh hơn)
        anim.SetFloat("Speed", movement.sqrMagnitude);

        // 4. LOGIC QUAN TRỌNG: LƯU HƯỚNG NHÌN
        // Chỉ cập nhật hướng (Horizontal/Vertical) khi có phím bấm.
        // Khi buông phím (movement == Vector2.zero), code sẽ KHÔNG cập nhật giá trị về 0.
        // Nhờ đó, Animator vẫn nhớ và giữ nguyên hướng cuối cùng (ví dụ: đang xoay trái thì vẫn lưu -1).
        if (movement != Vector2.zero)
        {
            anim.SetFloat("Horizontal", movement.x);
            anim.SetFloat("Vertical", movement.y);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}