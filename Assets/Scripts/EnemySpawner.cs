using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    // GDD v3.0 - one entry per Bug type. spawnWeight is RELATIVE: higher => spawns more often.
    [System.Serializable]
    public struct EnemySpawnConfig
    {
        public GameObject enemyPrefab;
        [Min(0f)] public float spawnWeight;
    }

    [Header("Spawn Pool (GDD v3.0 - Weighted Random)")]
    [Tooltip("Drag each Bug prefab here and set its spawnWeight. Relative ratio: SyntaxError (high) > LogicBug (mid) > MemoryLeak (low).")]
    [SerializeField] private EnemySpawnConfig[] enemySpawnConfigs;

    [Header("Spawner Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Pool Settings")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 200;

    // One ObjectPool per distinct Bug prefab (SyntaxError / LogicBug / MemoryLeak).
    private readonly Dictionary<GameObject, IObjectPool<EnemyBehavior>> pools =
        new Dictionary<GameObject, IObjectPool<EnemyBehavior>>();

    void Awake()
    {
        BuildPools();
    }

    private void BuildPools()
    {
        foreach (EnemySpawnConfig config in enemySpawnConfigs)
        {
            GameObject prefab = config.enemyPrefab; // local copy -> captured correctly per pool
            if (prefab == null || pools.ContainsKey(prefab))
            {
                continue;
            }

            IObjectPool<EnemyBehavior> pool = null;
            pool = new ObjectPool<EnemyBehavior>(
                createFunc: () =>
                {
                    EnemyBehavior enemy = Instantiate(prefab).GetComponent<EnemyBehavior>();
                    enemy.SetPool(pool); // hand the Bug a reference to its own pool
                    return enemy;
                },
                actionOnGet: enemy => enemy.gameObject.SetActive(true),
                actionOnRelease: enemy => enemy.gameObject.SetActive(false),
                actionOnDestroy: enemy => Destroy(enemy.gameObject),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize);

            pools.Add(prefab, pool);
        }
    }

    void Start()
    {
        StartCoroutine(SpawnEnemyRoutine());
    }

    IEnumerator SpawnEnemyRoutine()
    {
        while (true)
        {
            // Stop producing Bugs once the game has ended (win/lose), per GDD win cleanup intent.
            if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No spawn points assigned.");
            return;
        }

        GameObject prefab = PickWeightedEnemy();
        if (prefab == null)
        {
            Debug.LogWarning("EnemySpawner: No valid enemy to spawn - check enemySpawnConfigs (prefab assigned? weight > 0?).");
            return;
        }

        if (!pools.TryGetValue(prefab, out IObjectPool<EnemyBehavior> pool))
        {
            Debug.LogWarning($"EnemySpawner: No pool found for prefab '{prefab.name}'.");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (spawnPoint == null)
        {
            Debug.LogWarning("EnemySpawner: Selected spawn point is missing.");
            return;
        }

        // Pull from the matching pool, place it, then reset its stats (OnGet state reset).
        EnemyBehavior enemy = pool.Get();
        enemy.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        enemy.ResetState();
    }

    // Roulette-wheel selection (GDD v3.0). O(n) over the configs, driven purely by the total
    // weight - never builds a list of duplicated prefabs. Unchanged by the pooling refactor.
    private GameObject PickWeightedEnemy()
    {
        if (enemySpawnConfigs == null || enemySpawnConfigs.Length == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        foreach (EnemySpawnConfig config in enemySpawnConfigs)
        {
            if (config.enemyPrefab != null && config.spawnWeight > 0f)
            {
                totalWeight += config.spawnWeight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.value * totalWeight;
        foreach (EnemySpawnConfig config in enemySpawnConfigs)
        {
            if (config.enemyPrefab == null || config.spawnWeight <= 0f)
            {
                continue;
            }

            if (roll < config.spawnWeight)
            {
                return config.enemyPrefab;
            }

            roll -= config.spawnWeight;
        }

        for (int i = enemySpawnConfigs.Length - 1; i >= 0; i--)
        {
            if (enemySpawnConfigs[i].enemyPrefab != null && enemySpawnConfigs[i].spawnWeight > 0f)
            {
                return enemySpawnConfigs[i].enemyPrefab;
            }
        }

        return null;
    }
}
