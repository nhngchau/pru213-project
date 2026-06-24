using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Refactor" Ultimate shockwave (GDD v3.0 - Section III.3). A ring drawn with a LineRenderer that
/// expands from the cast point, damaging each Bug once as the wavefront sweeps past it, then destroys
/// itself once it grows past maxRadius (bigger than the map). Built entirely from primitives - no sprite.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RefactorWave : MonoBehaviour
{
    private const int Segments = 64;

    private int damage;
    private float expandSpeed;
    private float maxRadius;

    private LineRenderer line;
    private readonly HashSet<EnemyBehavior> alreadyHit = new HashSet<EnemyBehavior>();

    /// <summary>Configure + launch the wave. Called right after the object is created.</summary>
    public void Init(int damage, float expandSpeed, float maxRadius, Color color, float width)
    {
        this.damage = damage;
        this.expandSpeed = expandSpeed;
        this.maxRadius = maxRadius;

        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;   // local points -> the ring stays centred on this object
        line.loop = true;
        line.positionCount = Segments;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = 100;      // draw on top of the gameplay sprites

        StartCoroutine(ExpandRoutine());
    }

    private IEnumerator ExpandRoutine()
    {
        float radius = 0f;
        while (radius < maxRadius)
        {
            radius += expandSpeed * Time.deltaTime;
            DrawRing(radius);
            DamageBugsWithin(radius);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void DrawRing(float radius)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = (i / (float)Segments) * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    // Damage every Bug inside the current radius exactly once (HashSet), so the wavefront "sweeps"
    // outward hitting each Bug as it reaches them. Killing a Bug runs its normal death (DataPack,
    // Memory Leak Sludge, return to pool).
    private void DamageBugsWithin(float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D col in hits)
        {
            if (col.TryGetComponent(out EnemyBehavior bug) && alreadyHit.Add(bug))
            {
                bug.TakeDamage(damage);
            }
        }
    }
}
