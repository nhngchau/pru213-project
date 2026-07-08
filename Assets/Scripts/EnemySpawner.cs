using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("Add all enemy types here (e.g. SyntaxError, MemoryLeak, LogicBug). One is chosen at random per spawn.")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;

    [Header("Group Spawn Settings")]
    [SerializeField] private int minGroupSize = 3;
    [SerializeField] private int maxGroupSize = 6;
    [SerializeField] private float groupSpreadRadius = 0.6f;

    [Header("Enemy Pool")]
    [SerializeField] private int defaultCapacityPerPrefab = 20;
    [SerializeField] private int maxPoolSizePerPrefab = 120;

    private readonly Dictionary<GameObject, IObjectPool<EnemyBehavior>> enemyPools = new Dictionary<GameObject, IObjectPool<EnemyBehavior>>();
    private float runtimeSpawnInterval;
    private int runtimeMinGroupSize;
    private int runtimeMaxGroupSize;

    private void Awake()
    {
        BuildPools();
    }

    private void Start()
    {
        ApplyStageScaling();
        StartCoroutine(SpawnEnemyRoutine());
    }

    private void ApplyStageScaling()
    {
        float spawnMultiplier = Mathf.Max(1f, RunProgress.EnemySpawnRateMultiplier);
        runtimeSpawnInterval = Mathf.Max(0.25f, spawnInterval / spawnMultiplier);
        runtimeMinGroupSize = minGroupSize + Mathf.Max(0, RunProgress.Stage - 1);
        runtimeMaxGroupSize = maxGroupSize + Mathf.Max(0, RunProgress.Stage - 1);
    }

    private IEnumerator SpawnEnemyRoutine()
    {
        while (true)
        {
            // Stop spawning when the game has already ended.
            if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            yield return new WaitForSeconds(runtimeSpawnInterval);
            SpawnEnemyGroup();
        }
    }

    private void SpawnEnemyGroup()
    {
        // --- Guard checks ---------------------------------------------------
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefabs assigned. Add at least one prefab to the Enemy Prefabs array.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No spawn points assigned.");
            return;
        }

        // --- Pick a random spawn point --------------------------------------
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (spawnPoint == null)
        {
            Debug.LogWarning("EnemySpawner: Selected spawn point is null — check your Spawn Points array for missing references.");
            return;
        }

        // --- Determine group size -------------------------------------------
        // Random.Range (int) is exclusive on max, so add 1 to include maxGroupSize.
        int smallestGroup = Mathf.Min(runtimeMinGroupSize, runtimeMaxGroupSize);
        int largestGroup  = Mathf.Max(runtimeMinGroupSize, runtimeMaxGroupSize);
        int groupSize     = Random.Range(smallestGroup, largestGroup + 1);

        // --- Spawn each enemy in the group ----------------------------------
        for (int i = 0; i < groupSize; i++)
        {
            // Choose a random prefab from the array for every individual enemy.
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: Enemy prefab at index {i % enemyPrefabs.Length} is null — skipping.");
                continue;
            }

            // Spread enemies in a circle around the chosen spawn point.
            Vector2 spreadOffset = Random.insideUnitCircle * groupSpreadRadius;
            Vector3 spawnPosition = spawnPoint.position + new Vector3(spreadOffset.x, spreadOffset.y, 0f);

            EnemyBehavior enemy = GetEnemy(prefab);
            if (enemy != null)
            {
                enemy.transform.SetPositionAndRotation(spawnPosition, spawnPoint.rotation);
                enemy.ResetState();
            }
        }
    }

    private void BuildPools()
    {
        enemyPools.Clear();
        if (enemyPrefabs == null)
        {
            return;
        }

        foreach (GameObject prefab in enemyPrefabs)
        {
            if (prefab == null || enemyPools.ContainsKey(prefab))
            {
                continue;
            }

            if (!prefab.TryGetComponent(out EnemyBehavior _))
            {
                Debug.LogWarning($"EnemySpawner: '{prefab.name}' has no EnemyBehavior component and cannot be pooled.");
                continue;
            }

            enemyPools[prefab] = new ObjectPool<EnemyBehavior>(
                createFunc: () => CreateEnemy(prefab),
                actionOnGet: enemy => enemy.gameObject.SetActive(true),
                actionOnRelease: enemy => enemy.gameObject.SetActive(false),
                actionOnDestroy: enemy => Destroy(enemy.gameObject),
                collectionCheck: true,
                defaultCapacity: Mathf.Max(1, defaultCapacityPerPrefab),
                maxSize: Mathf.Max(defaultCapacityPerPrefab, maxPoolSizePerPrefab));
        }
    }

    private EnemyBehavior CreateEnemy(GameObject prefab)
    {
        GameObject enemyObject = Instantiate(prefab);
        EnemyBehavior enemy = enemyObject.GetComponent<EnemyBehavior>();
        enemy.SetPool(enemyPools[prefab]);
        enemyObject.SetActive(false);
        return enemy;
    }

    private EnemyBehavior GetEnemy(GameObject prefab)
    {
        if (!enemyPools.TryGetValue(prefab, out IObjectPool<EnemyBehavior> pool))
        {
            Debug.LogWarning($"EnemySpawner: No pool found for '{prefab.name}'. Check Enemy Prefabs.");
            return null;
        }

        return pool.Get();
    }
}
