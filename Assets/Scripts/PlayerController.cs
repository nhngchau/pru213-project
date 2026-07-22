using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, ISlowable
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.Space;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.14f;
    [SerializeField] private float dashCooldown = 1.2f;
    [Tooltip("Số giây bất tử tính từ lúc bắt đầu dash — cho phép lách qua đòn đánh.")]
    [SerializeField] private float dashInvulnerability = 0.25f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem moveDustParticle;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;
    private PlayerHealth playerHealth;

    private float baseMoveSpeed;
    private readonly Dictionary<object, float> speedModifiers = new Dictionary<object, float>();

    private Vector2 lastMoveDirection = Vector2.right;
    private Vector2 dashDirection;
    private float dashEndTime;
    private float nextDashTime;

    public bool IsDashing => Time.time < dashEndTime;
    public bool IsDashReady => Time.time >= nextDashTime;

    /// <summary>0 ngay sau khi dash -> 1 khi đã hồi xong. Dùng cho UI cooldown nếu cần.</summary>
    public float DashCooldownProgress =>
        dashCooldown <= 0f ? 1f : Mathf.Clamp01(1f - (nextDashTime - Time.time) / dashCooldown);

    void Awake()
    {
        baseMoveSpeed = moveSpeed;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        anim.SetFloat("Speed", movement.sqrMagnitude);

        // --- Dust particle control ---
        bool isMoving = movement != Vector2.zero;
        if (moveDustParticle != null)
        {
            if (isMoving && !moveDustParticle.isPlaying)
            {
                moveDustParticle.Play();
            }
            else if (!isMoving && moveDustParticle.isPlaying)
            {
                moveDustParticle.Stop();
            }
        }

        // Only update facing direction while moving (preserves last-facing direction when idle)
        if (isMoving)
        {
            anim.SetFloat("Horizontal", movement.x);
            anim.SetFloat("Vertical", movement.y);
            lastMoveDirection = movement;
        }

        if (Input.GetKeyDown(dashKey))
        {
            TryDash();
        }
    }

    void FixedUpdate()
    {
        // Dash dùng dashSpeed cố định nên không bị speedModifiers làm chậm — lướt ra khỏi
        // vũng Sludge được, đó chính là cách xử lý mà dash tồn tại để phục vụ.
        Vector2 velocity = IsDashing ? dashDirection * dashSpeed : movement * moveSpeed;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    private void TryDash()
    {
        if (IsDashing || !IsDashReady)
        {
            return;
        }

        dashDirection = movement != Vector2.zero ? movement : lastMoveDirection;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        playerHealth?.GrantInvulnerability(dashInvulnerability);
    }

    public void AddSpeedModifier(object source, float multiplier)
    {
        if (source == null)
        {
            return;
        }

        speedModifiers[source] = Mathf.Clamp01(multiplier);
        RecalculateSpeed();
    }

    public void RemoveSpeedModifier(object source)
    {
        if (source != null && speedModifiers.Remove(source))
        {
            RecalculateSpeed();
        }
    }

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
