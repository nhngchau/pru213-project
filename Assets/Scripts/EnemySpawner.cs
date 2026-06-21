using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject syntaxErrorPrefab;
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
        if (syntaxErrorPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: Syntax Error Prefab is missing.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No spawn points assigned.");
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
        Instantiate(syntaxErrorPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
