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

    public int CurrentHP { get; private set; }
    public bool IsDown { get; private set; }
    public bool IsInvulnerable { get; private set; }

    private Rigidbody2D rb;
    private ServerCore server;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerShooting == null) playerShooting = GetComponent<PlayerShooting>();
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

        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        CurrentHP -= amount;
        CurrentHP = Mathf.Clamp(CurrentHP, 0, maxHP);

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
        IsInvulnerable = true;

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

        IsInvulnerable = false;
    }

    private void SetActiveState(bool active)
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.enabled = active;
        if (bodyCollider != null) bodyCollider.enabled = active;
        if (playerController != null) playerController.enabled = active;
        if (playerShooting != null) playerShooting.enabled = active;
    }

    [ContextMenu("DEBUG: Kill Player")]
    private void DebugKill() => TakeDamage(CurrentHP);
}
