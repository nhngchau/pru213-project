using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class PooledAutoRelease : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float fallbackLifetime = 2f;

    private IObjectPool<PooledAutoRelease> pool;
    private Coroutine releaseRoutine;

    public void SetPool(IObjectPool<PooledAutoRelease> objectPool)
    {
        pool = objectPool;
    }

    public void Play()
    {
        if (releaseRoutine != null)
        {
            StopCoroutine(releaseRoutine);
        }

        foreach (ParticleSystem particle in GetComponentsInChildren<ParticleSystem>())
        {
            particle.Clear(true);
            particle.Play(true);
        }

        releaseRoutine = StartCoroutine(ReleaseAfterLifetime());
    }

    private IEnumerator ReleaseAfterLifetime()
    {
        float longestLifetime = fallbackLifetime;
        foreach (ParticleSystem particle in GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = particle.main;
            longestLifetime = Mathf.Max(longestLifetime, main.duration + main.startLifetime.constantMax);
        }

        yield return new WaitForSeconds(longestLifetime);
        releaseRoutine = null;

        if (pool != null && gameObject != null)
        {
            pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
