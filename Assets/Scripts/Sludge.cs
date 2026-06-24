using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Sludge : MonoBehaviour
{
    [Header("Sludge (GDD v3.0)")]
    [SerializeField] private float lifetime = 3f;
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.5f;

    private ISlowable affected;

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
