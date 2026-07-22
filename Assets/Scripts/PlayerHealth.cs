using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health (GDD v3.0)")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private Slider hpSlider;

    [Header("Penalty / Respawn")]
    [SerializeField] private float penaltyDuration = 5f;
    [SerializeField] private float invulnerableDuration = 2f;
    [SerializeField] private float flashInterval = 0.15f;
    [SerializeField] private Vector2 respawnOffset = new Vector2(0f, -2f);

    [Header("Component References (auto-found if left empty)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerShooting playerShooting;

    [Header("Objects to Hide on Death")]
    [Tooltip("Kéo Gun GameObject vào đây — sẽ ẩn khi player chết.")]
    [SerializeField] private GameObject gunObject;
    [Tooltip("Kéo PlayerHPSlider vào đây — sẽ ẩn khi player chết.")]
    [SerializeField] private GameObject hpBarObject;

    public int CurrentHP { get; private set; }
    public bool IsDown { get; private set; }
    public bool IsInvulnerable { get; private set; }

    private Rigidbody2D rb;
    private ServerCore server;

    // Đếm số nguồn đang cấp bất tử (hồi sinh, dash...) để nguồn kết thúc trước
    // không tắt nhầm bất tử mà nguồn khác vẫn đang giữ.
    private int invulnerabilityCount;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bodyCollider == null)   bodyCollider   = GetComponent<Collider2D>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerShooting == null)   playerShooting   = GetComponent<PlayerShooting>();

        // Tự động tìm Gun nếu chưa gán
        if (gunObject == null)
        {
            Transform gunTransform = FindChildRecursive(transform, "Gun");
            if (gunTransform != null) gunObject = gunTransform.gameObject;
        }

        // Tự động dùng hpSlider GameObject nếu chưa gán
        if (hpBarObject == null && hpSlider != null)
        {
            hpBarObject = hpSlider.gameObject;
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    void Start()
    {
        CurrentHP = maxHP;
        server = FindFirstObjectByType<ServerCore>();

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = CurrentHP;
        }

        PlayerEvents.RaisePlayerHealthChanged(CurrentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (IsDown || IsInvulnerable)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (godMode) return; // God Mode: bỏ qua mọi damage
#endif

        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        CurrentHP -= amount;
        CurrentHP = Mathf.Clamp(CurrentHP, 0, maxHP);

        GameAudioManager.Instance?.PlayPlayerHit(); // sound khi bị đánh

        if (hpSlider != null)
        {
            hpSlider.value = CurrentHP;
        }

        PlayerEvents.RaisePlayerHealthChanged(CurrentHP, maxHP);

        if (CurrentHP <= 0)
        {
            EnterDowntime();
        }
    }

    private void EnterDowntime()
    {
        IsDown = true;

        SetActiveState(false);

        PlayerEvents.RaisePlayerDied();

        StartCoroutine(PenaltyRoutine());
    }

    private IEnumerator PenaltyRoutine()
    {
        for (int secondsLeft = Mathf.CeilToInt(penaltyDuration); secondsLeft >= 1; secondsLeft--)
        {
            PlayerEvents.RaisePenaltyCountdown(secondsLeft);
            yield return new WaitForSeconds(1f);
        }

        Respawn();
    }

    private void Respawn()
    {
        if (server != null)
        {
            transform.position = (Vector2)server.transform.position + respawnOffset;
        }

        CurrentHP = maxHP;
        IsDown = false;

        if (hpSlider != null)
        {
            hpSlider.value = CurrentHP;
        }

        SetActiveState(true);

        PlayerEvents.RaisePlayerHealthChanged(CurrentHP, maxHP);
        PlayerEvents.RaisePenaltyCountdown(0);
        PlayerEvents.RaisePlayerRespawned();

        StartCoroutine(InvulnerabilityRoutine());
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        PushInvulnerability();

        float elapsed = 0f;
        while (elapsed < invulnerableDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        PopInvulnerability();
    }

    /// <summary>
    /// Cấp bất tử trong duration giây — dùng cho i-frame của dash. Không nhấp nháy sprite
    /// để phân biệt với bất tử sau hồi sinh.
    /// </summary>
    public void GrantInvulnerability(float duration)
    {
        if (IsDown || duration <= 0f)
        {
            return;
        }

        StartCoroutine(TemporaryInvulnerabilityRoutine(duration));
    }

    private IEnumerator TemporaryInvulnerabilityRoutine(float duration)
    {
        PushInvulnerability();
        yield return new WaitForSeconds(duration);
        PopInvulnerability();
    }

    private void PushInvulnerability()
    {
        invulnerabilityCount++;
        IsInvulnerable = true;
    }

    private void PopInvulnerability()
    {
        invulnerabilityCount = Mathf.Max(0, invulnerabilityCount - 1);
        IsInvulnerable = invulnerabilityCount > 0;
    }

    private void SetActiveState(bool active)
    {
        if (rb != null)               rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null)   spriteRenderer.enabled = active;
        if (bodyCollider != null)     bodyCollider.enabled = active;
        if (playerController != null) playerController.enabled = active;
        if (playerShooting != null)   playerShooting.enabled = active;

        // Ẩn/hiện gun và thanh máu cùng với player
        if (gunObject != null)   gunObject.SetActive(active);
        if (hpBarObject != null) hpBarObject.SetActive(active);
    }

    [ContextMenu("DEBUG: Kill Player")]
    private void DebugKill() => TakeDamage(CurrentHP);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool godMode = false;

    /// <summary>Bật/tắt God Mode — player không chết, máu liên tục full.</summary>
    public void DebugSetGodMode(bool enabled)
    {
        godMode = enabled;
        if (enabled) DebugHealToFull();
    }

    /// <summary>Hồi máu player về max ngay lập tức.</summary>
    public void DebugHealToFull()
    {
        if (IsDown) return; // đang trong penalty, không can thiệp

        CurrentHP = maxHP;
        if (hpSlider != null) hpSlider.value = CurrentHP;
        PlayerEvents.RaisePlayerHealthChanged(CurrentHP, maxHP);
    }

    // Override TakeDamage khi God Mode bật
    // (Unity không hỗ trợ override nên ta patch qua property)
    public bool IsGodMode => godMode;
#endif
}
