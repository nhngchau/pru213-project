using System.Collections;
using UnityEngine;

/// <summary>
/// "Refactor" Ultimate (GDD v3.0 - Section III.3): right-click releases an expanding AoE shockwave
/// centred on the Player that damages Bugs around it. Cooldown is driven by a Coroutine. Damage scales
/// with the current bullet damage (Overclock CPU upgrades), so it grows stronger as the player upgrades.
/// Add this component to the Player (next to PlayerShooting / PlayerHealth).
/// </summary>
public class PlayerUltimate : MonoBehaviour
{
    [Header("Refactor Ultimate (GDD III.3)")]
    [SerializeField] private float cooldown = 8f;
    [Tooltip("Ultimate damage = current bullet damage x this (so it scales with Overclock CPU upgrades).")]
    [SerializeField] private float damageMultiplier = 3f;

    [Header("Shockwave")]
    [SerializeField] private float expandSpeed = 18f;  // how fast the ring grows (units/sec)
    [SerializeField] private float maxRadius = 30f;    // grows past this (bigger than the map) -> destroyed
    [SerializeField] private Color waveColor = new Color(0.35f, 0.9f, 1f);
    [SerializeField] private float waveWidth = 0.25f;

    private PlayerShooting playerShooting;
    private PlayerHealth playerHealth;
    private bool onCooldown;

    public bool IsOnCooldown => onCooldown; // handy for a future cooldown UI indicator

    void Awake()
    {
        playerShooting = GetComponent<PlayerShooting>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(1) || onCooldown)
        {
            return;
        }

        // Don't fire while paused (upgrade panel), after the game ends, or while in Downtime.
        if (Time.timeScale == 0f)
        {
            return;
        }
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }
        if (playerHealth != null && playerHealth.IsDown)
        {
            return;
        }

        ActivateUltimate();
    }

    private void ActivateUltimate()
    {
        // GDD: damage scales with the damage upgrade (Overclock CPU raises bullet damage).
        int currentDamage = playerShooting != null ? playerShooting.BulletDamage : 10;
        int waveDamage = Mathf.RoundToInt(currentDamage * damageMultiplier);

        GameObject waveObject = new GameObject("RefactorWave");
        waveObject.transform.position = transform.position; // centred on the Player at cast time
        RefactorWave wave = waveObject.AddComponent<RefactorWave>();
        wave.Init(waveDamage, expandSpeed, maxRadius, waveColor, waveWidth);

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
}
