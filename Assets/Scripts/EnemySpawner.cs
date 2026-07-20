using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Cấu hình enemy cho từng stage.
/// Nếu stage hiện tại không có trong danh sách → dùng stageEnemySets cuối cùng.
/// </summary>
[System.Serializable]
public class StageEnemySet
{
    [Tooltip("Stage này áp dụng từ stage nào trở đi (ví dụ: 1, 2, 3...)")]
    public int fromStage = 1;

    [Tooltip("Danh sách enemy prefab xuất hiện ở stage này")]
    public GameObject[] enemyPrefabs;
}

[System.Serializable]
public class StageBossSettings
{
    [Tooltip("Stage xuất hiện Boss (ví dụ: 6, 9, 10)")]
    public int stage;
    [Tooltip("Prefab của Boss tương ứng")]
    public GameObject bossPrefab;
    [Tooltip("Số lượng Boss xuất hiện")]
    public int bossCount = 1;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs theo Stage")]
    [Tooltip("Mỗi entry là 1 bộ enemy cho stage tương ứng. Stage không có entry → dùng entry cuối.")]
    [SerializeField] private StageEnemySet[] stageEnemySets;

    [Header("Boss Settings")]
    [Tooltip("Cấu hình Boss riêng biệt cho từng Stage")]
    [SerializeField] private List<StageBossSettings> bossSettings = new List<StageBossSettings>();
    [Tooltip("Thời gian chờ trước khi Boss xuất hiện (giây)")]
    [SerializeField] private float bossSpawnDelay = 10f;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2.4f;

    [Header("Group Spawn Settings")]
    [SerializeField] private int minGroupSize = 2;
    [SerializeField] private int maxGroupSize = 5;
    [SerializeField] private float groupSpreadRadius = 0.6f;

    [Header("Build Progress Pressure")]
    [Tooltip("Extra spawn speed applied as Build Progress approaches 100%. 2 means twice as fast near the end.")]
    [SerializeField] private float endBuildSpawnRateMultiplier = 1.6f;
    [Tooltip("Extra enemies added to each group when Build Progress reaches 100%.")]
    [SerializeField] private int endBuildGroupBonus = 2;
    [SerializeField] private float minRuntimeSpawnInterval = 0.45f;

    [Header("Enemy Pool")]
    [SerializeField] private int defaultCapacityPerPrefab = 30;
    [SerializeField] private int maxPoolSizePerPrefab = 160;

    private readonly Dictionary<GameObject, IObjectPool<EnemyBehavior>> enemyPools = new Dictionary<GameObject, IObjectPool<EnemyBehavior>>();
    private float runtimeSpawnInterval;
    private int runtimeMinGroupSize;
    private int runtimeMaxGroupSize;
    private float buildProgress01;

    // Cache bộ enemy đang dùng cho stage hiện tại
    private GameObject[] currentEnemyPrefabs;
    private StageBossSettings currentBossSetting;

    private void Awake()
    {
        currentEnemyPrefabs = GetEnemyPrefabsForStage(RunProgress.Stage);
        if (bossSettings != null)
        {
            currentBossSetting = bossSettings.Find(b => b.stage == RunProgress.Stage);
        }
        BuildPools();
    }

    /// <summary>Trả về bộ enemy phù hợp với stage hiện tại.</summary>
    private GameObject[] GetEnemyPrefabsForStage(int stage)
    {
        if (stageEnemySets == null || stageEnemySets.Length == 0)
        {
            return new GameObject[0];
        }

        // Tìm entry phù hợp nhất: entry có fromStage <= stage và lớn nhất
        StageEnemySet best = stageEnemySets[0];
        foreach (StageEnemySet set in stageEnemySets)
        {
            if (set.fromStage <= stage && set.fromStage >= best.fromStage)
            {
                best = set;
            }
        }

        return best.enemyPrefabs ?? new GameObject[0];
    }


    private void OnEnable()
    {
        GameEvents.OnBuildProgressChanged += HandleBuildProgressChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnBuildProgressChanged -= HandleBuildProgressChanged;
    }

    private void Start()
    {
        ApplySpawnScaling();
        StartCoroutine(SpawnEnemyRoutine());

        // Nếu stage hiện tại có cấu hình Boss, bắt đầu đếm ngược để spawn Boss
        if (currentBossSetting != null && currentBossSetting.bossPrefab != null)
        {
            StartCoroutine(SpawnBossRoutine());
        }
    }

    private IEnumerator SpawnBossRoutine()
    {
        yield return new WaitForSeconds(bossSpawnDelay);

        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            yield break;
        }

        if (spawnPoints == null || spawnPoints.Length == 0) yield break;

        for (int i = 0; i < currentBossSetting.bossCount; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector2 spreadOffset = Random.insideUnitCircle * groupSpreadRadius;
            Vector3 spawnPosition = spawnPoint.position + new Vector3(spreadOffset.x, spreadOffset.y, 0f);

            EnemyBehavior boss = GetEnemy(currentBossSetting.bossPrefab);
            if (boss != null)
            {
                boss.transform.SetPositionAndRotation(spawnPosition, spawnPoint.rotation);
                boss.ResetState();
                
                // Tăng kích thước Boss cho nổi bật (tuỳ chọn)
                boss.transform.localScale = currentBossSetting.bossPrefab.transform.localScale * 1.5f;
            }
        }
    }

    private void HandleBuildProgressChanged(float percent)
    {
        buildProgress01 = Mathf.Clamp01(percent / 100f);
        ApplySpawnScaling();
    }

    private void ApplySpawnScaling()
    {
        float stageSpawnMultiplier = Mathf.Max(1f, RunProgress.EnemySpawnRateMultiplier);
        float buildSpawnMultiplier = Mathf.Lerp(1f, Mathf.Max(1f, endBuildSpawnRateMultiplier), buildProgress01);
        int buildGroupBonus = Mathf.RoundToInt(Mathf.Max(0, endBuildGroupBonus) * buildProgress01);

        runtimeSpawnInterval = Mathf.Max(minRuntimeSpawnInterval, spawnInterval / (stageSpawnMultiplier * buildSpawnMultiplier));
        runtimeMinGroupSize = minGroupSize + RunProgress.EnemyGroupBonus + buildGroupBonus;
        runtimeMaxGroupSize = maxGroupSize + RunProgress.EnemyGroupBonus + buildGroupBonus;
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
        if (currentEnemyPrefabs == null || currentEnemyPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: Không có enemy prefab cho stage " + RunProgress.Stage + ". Hãy gán Stage Enemy Sets trong Inspector.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No spawn points assigned.");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (spawnPoint == null) return;

        int smallestGroup = Mathf.Min(runtimeMinGroupSize, runtimeMaxGroupSize);
        int largestGroup  = Mathf.Max(runtimeMinGroupSize, runtimeMaxGroupSize);
        int groupSize     = Random.Range(smallestGroup, largestGroup + 1);

        for (int i = 0; i < groupSize; i++)
        {
            GameObject prefab = currentEnemyPrefabs[Random.Range(0, currentEnemyPrefabs.Length)];
            if (prefab == null) continue;

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

        // Khởi tạo Pool cho Boss nếu stage hiện tại cần Boss
        if (currentBossSetting != null && currentBossSetting.bossPrefab != null && !enemyPools.ContainsKey(currentBossSetting.bossPrefab))
        {
            GameObject bPrefab = currentBossSetting.bossPrefab;
            if (bPrefab.TryGetComponent(out EnemyBehavior _))
            {
                enemyPools[bPrefab] = new ObjectPool<EnemyBehavior>(
                    createFunc: () => CreateEnemy(bPrefab),
                    actionOnGet: enemy => enemy.gameObject.SetActive(true),
                    actionOnRelease: enemy => enemy.gameObject.SetActive(false),
                    actionOnDestroy: enemy => Destroy(enemy.gameObject),
                    collectionCheck: true,
                    defaultCapacity: 1,
                    maxSize: 5);
            }
        }

        if (currentEnemyPrefabs == null) return;

        foreach (GameObject prefab in currentEnemyPrefabs)
        {
            if (prefab == null || enemyPools.ContainsKey(prefab)) continue;

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
