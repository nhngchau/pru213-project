using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the player's HP and the full Downtime -> Penalty -> Respawn -> Invulnerability flow
/// (GDD v3.0 - Section V). Movement/aiming stay in PlayerController/PlayerShooting; this
/// component only toggles them. All timing runs through Coroutines (never Update).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health (GDD v3.0)")]
    [SerializeField] private int maxHP = 100;

    [Header("Penalty / Respawn")]
    [SerializeField] private float penaltyDuration = 5f;      // GDD: 5s downtime timer
    [SerializeField] private float invulnerableDuration = 2f; // GDD: 2s protection after respawn
    [SerializeField] private float flashInterval = 0.15f;     // sprite blink cadence while invulnerable
    [SerializeField] private Vector2 respawnOffset = new Vector2(0f, -2f); // placed next to the Server

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
    }

    /// <summary>Public entry point for any future damage source. Respects invulnerability/downtime.</summary>
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

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            EnterDowntime();
        }
    }

    // --- Downtime ---------------------------------------------------------

    private void EnterDowntime()
    {
        IsDown = true;

        // GDD: do NOT destroy the player. Hide sprite, disable collision, freeze control.
        SetActiveState(false);

        // Notify listeners (Bugs rally to the Server, UI starts the countdown) - fully decoupled.
        PlayerEvents.RaisePlayerDied();

        StartCoroutine(PenaltyRoutine());
    }

    private IEnumerator PenaltyRoutine()
    {
        // 5..1 countdown, one tick per second, driven by the Coroutine (not Update).
        for (int secondsLeft = Mathf.CeilToInt(penaltyDuration); secondsLeft >= 1; secondsLeft--)
        {
            PlayerEvents.RaisePenaltyCountdown(secondsLeft);
            yield return new WaitForSeconds(1f);
        }

        Respawn();
    }

    // --- Respawn ----------------------------------------------------------

    private void Respawn()
    {
        if (server != null)
        {
            transform.position = (Vector2)server.transform.position + respawnOffset;
        }

        CurrentHP = maxHP;
        IsDown = false;

        SetActiveState(true);

        PlayerEvents.RaisePenaltyCountdown(0); // hide the countdown UI
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
                spriteRenderer.enabled = !spriteRenderer.enabled; // flash
            }

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true; // always end visible
        }

        IsInvulnerable = false;
    }

    // --- Helpers ----------------------------------------------------------

    private void SetActiveState(bool active)
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.enabled = active;
        if (bodyCollider != null) bodyCollider.enabled = active;
        if (playerController != null) playerController.enabled = active;
        if (playerShooting != null) playerShooting.enabled = active;
    }

    // Lets us trigger the flow from the Inspector for testing until a player-damage source exists.
    [ContextMenu("DEBUG: Kill Player")]
    private void DebugKill() => TakeDamage(CurrentHP);
}
