using System.Collections;
using UnityEngine;

/// <summary>
/// GDD v3.0 - Memory Leak special ability: a pool of 'Sludge' that lives for a few seconds
/// and slows anything ISlowable (the Player) standing in it. Fully self-contained and modular:
/// it manages its own lifetime and GUARANTEES the slow is reverted - both when the Player
/// leaves the area AND when the pool expires while the Player is still inside.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Sludge : MonoBehaviour
{
    [Header("Sludge (GDD v3.0)")]
    [SerializeField] private float lifetime = 3f;                        // GDD: exists for 3 seconds
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.5f; // GDD: reduces Move Speed by 50%

    // The single entity (the Player) we are currently slowing, so we can revert exactly it.
    private ISlowable affected;

    void Start()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject); // OnDestroy reverts the slow if the Player is still standing in it.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out ISlowable slowable))
        {
            affected = slowable;
            slowable.AddSpeedModifier(this, slowMultiplier);
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
        // Safety net: if the 3s elapse while the Player is still inside, OnTriggerExit2D may not
        // fire - so revert here to make sure the Player never gets stuck at reduced speed.
        if (affected != null)
        {
            affected.RemoveSpeedModifier(this);
            affected = null;
        }
    }
}
