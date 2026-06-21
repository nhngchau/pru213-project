using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, ISlowable
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim; // 1. Khai báo Animator

    // GDD v3.0 - Sludge slow: keep the original speed and a source-keyed set of multipliers
    // so each slow effect reverts independently and safely (no permanent slow).
    private float baseMoveSpeed;
    private readonly Dictionary<object, float> speedModifiers = new Dictionary<object, float>();

    void Awake()
    {
        baseMoveSpeed = moveSpeed;
    }

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

    // --- ISlowable (GDD v3.0 - Memory Leak 'Sludge') --------------------------

    /// <summary>Register or refresh a speed multiplier from a source (0.5 = 50% speed).</summary>
    public void AddSpeedModifier(object source, float multiplier)
    {
        if (source == null)
        {
            return;
        }

        speedModifiers[source] = Mathf.Clamp01(multiplier);
        RecalculateSpeed();
    }

    /// <summary>Remove a source's multiplier and recompute. Safe if the source is absent.</summary>
    public void RemoveSpeedModifier(object source)
    {
        if (source != null && speedModifiers.Remove(source))
        {
            RecalculateSpeed();
        }
    }

    // Strongest slow wins (min), so overlapping Sludge pools don't stack toward zero.
    private void RecalculateSpeed()
    {
        float multiplier = 1f;
        foreach (float m in speedModifiers.Values)
        {
            multiplier = Mathf.Min(multiplier, m);
        }

        moveSpeed = baseMoveSpeed * multiplier;
    }
}
