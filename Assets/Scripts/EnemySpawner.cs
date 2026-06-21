using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
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

    void Start()
    {
        StartCoroutine(SpawnEnemyRoutine());
    }

    IEnumerator SpawnEnemyRoutine()
    {
        while (true)
        {
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

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (spawnPoint == null)
        {
            Debug.LogWarning("EnemySpawner: Selected spawn point is missing.");
            return;
        }

        // TODO (next task - Object Pooling): GDD mandates pooling for enemy spawning.
        // Replace Instantiate with pool.Get(spawnPoint.position, spawnPoint.rotation).
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }

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
