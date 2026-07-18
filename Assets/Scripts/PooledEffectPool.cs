using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public static class PooledEffectPool
{
    private const int DefaultCapacity = 12;
    private const int MaxPoolSize = 80;

    private static readonly Dictionary<GameObject, IObjectPool<PooledAutoRelease>> Pools = new Dictionary<GameObject, IObjectPool<PooledAutoRelease>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Pools.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneCleanup()
    {
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return;
        }

        PooledAutoRelease effect = GetPool(prefab).Get();
        effect.transform.SetPositionAndRotation(position, rotation);
        effect.Play();
    }

    private static IObjectPool<PooledAutoRelease> GetPool(GameObject prefab)
    {
        if (Pools.TryGetValue(prefab, out IObjectPool<PooledAutoRelease> pool))
        {
            return pool;
        }

        IObjectPool<PooledAutoRelease> createdPool = null;
        createdPool = new ObjectPool<PooledAutoRelease>(
            createFunc: () => CreateEffect(prefab, createdPool),
            actionOnGet: effect =>
            {
                if (effect != null)
                {
                    effect.gameObject.SetActive(true);
                }
            },
            actionOnRelease: effect =>
            {
                if (effect != null)
                {
                    effect.gameObject.SetActive(false);
                }
            },
            actionOnDestroy: effect =>
            {
                if (effect != null)
                {
                    Object.Destroy(effect.gameObject);
                }
            },
            collectionCheck: true,
            defaultCapacity: DefaultCapacity,
            maxSize: MaxPoolSize);

        Pools[prefab] = createdPool;
        return createdPool;
    }

    private static PooledAutoRelease CreateEffect(GameObject prefab, IObjectPool<PooledAutoRelease> pool)
    {
        GameObject instance = Object.Instantiate(prefab);
        if (!instance.TryGetComponent(out PooledAutoRelease autoRelease))
        {
            autoRelease = instance.AddComponent<PooledAutoRelease>();
        }

        autoRelease.SetPool(pool);
        instance.SetActive(false);
        return autoRelease;
    }

    private static void HandleSceneUnloaded(Scene _)
    {
        foreach (IObjectPool<PooledAutoRelease> pool in Pools.Values)
        {
            pool.Clear();
        }

        Pools.Clear();
    }
}
