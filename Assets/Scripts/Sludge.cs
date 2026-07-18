using System.Collections;
using UnityEngine;

/// <summary>
/// Memory Leak's 'Sludge' pool. While the Player stands in it: it slows them by 50% (GDD v3.0) and
/// also deals contact damage over time (HP/s). Lives for a few seconds, then despawns. The damage is
/// gated by a 1s timer (clean "HP per second") and only affects the Player (the ISlowable) - enemies
/// passing through are unharmed. The slow is safely reverted on exit AND on expiry.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Sludge : MonoBehaviour
{
    [Header("Sludge (GDD v3.0)")]
    [SerializeField] private float lifetime = 3f;
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.5f;

    [Header("Damage (HP/s)")]
    [SerializeField] private int damagePerSecond = 10;
    [SerializeField] private float damageTickInterval = 1f;

    private ISlowable affected;
    private float nextDamageTime;

    void Start()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out ISlowable slowable))
        {
            affected = slowable;
            slowable.AddSpeedModifier(this, slowMultiplier);
        }
    }

    // Damage tick (HP/s). Only the Player (an ISlowable) is hurt; enemies passing through are not.
    // TakeDamage respects the Player's invulnerability / downtime, so it is safe to call every tick.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextDamageTime)
        {
            return;
        }

        if (other.TryGetComponent(out ISlowable _) && other.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damagePerSecond);
            nextDamageTime = Time.time + damageTickInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out ISlowable slowable) && ReferenceEquals(slowable, affected))
        {
            slowable.RemoveSpeedModifier(this);
            affected = null;
        }
    }

    private void OnDestroy()
    {
        if (affected != null)
        {
            affected.RemoveSpeedModifier(this);
            affected = null;
        }
    }
}
