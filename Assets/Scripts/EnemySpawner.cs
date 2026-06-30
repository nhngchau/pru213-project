using System.Collections;
using UnityEngine;

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

    private void Start()
    {
        StartCoroutine(SpawnEnemyRoutine());
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

            yield return new WaitForSeconds(spawnInterval);
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
        int smallestGroup = Mathf.Min(minGroupSize, maxGroupSize);
        int largestGroup  = Mathf.Max(minGroupSize, maxGroupSize);
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

            GameObject enemyObject = Instantiate(prefab, spawnPosition, spawnPoint.rotation);

            // Reset enemy state if the prefab uses EnemyBehavior (safe no-op otherwise).
            EnemyBehavior enemy = enemyObject.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.ResetState();
            }
        }
    }
}
