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

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs theo Stage")]
    [Tooltip("Mỗi entry là 1 bộ enemy cho stage tương ứng. Stage không có entry → dùng entry cuối.")]
    [SerializeField] private StageEnemySet[] stageEnemySets;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2.4f;

    [Header("Wave Pacing")]
    [Tooltip("Số giây ngừng spawn sau mỗi wave — tạo nhịp căng/nghỉ thay vì spawn đều suốt stage.")]
    [SerializeField] private float waveBreakDuration = 3f;

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
    private int unlockedPrefabCount = 1;
    private float waveBreakUntil;

    // Cache bộ enemy đang dùng cho stage hiện tại
    private GameObject[] currentEnemyPrefabs;

    private void Awake()
    {
        currentEnemyPrefabs = GetEnemyPrefabsForStage(RunProgress.Stage);
        BuildPools();
    }

    /// <summary>Trả về bộ enemy phù hợp với stage hiện tại.</summary>
    private GameObject[] GetEnemyPrefabsForStage(int stage)
    {
        if (stageEnemySets == null || stageEnemySets.Length == 0)
        {
            return new GameObject[0];
        }

        // Entry phù hợp = entry có fromStage lớn nhất mà vẫn <= stage hiện tại.
        // KHÔNG giả định mảng đã được sắp xếp trong Inspector, và không giả định luôn tồn tại một
        // entry fromStage <= stage — người chỉnh Inspector có thể xoá mất entry fromStage 1.
        StageEnemySet best = null;
        foreach (StageEnemySet set in stageEnemySets)
        {
            if (set == null || set.fromStage > stage)
            {
                continue;
            }

            if (best == null || set.fromStage > best.fromStage)
            {
                best = set;
            }
        }

        // Mọi entry đều dành cho stage cao hơn -> lấy tạm entry có fromStage thấp nhất, còn hơn
        // là spawn rỗng rồi để người chơi đứng nhìn một stage không có gì.
        if (best == null)
        {
            foreach (StageEnemySet set in stageEnemySets)
            {
                if (set != null && (best == null || set.fromStage < best.fromStage))
                {
                    best = set;
                }
            }
        }

        return best?.enemyPrefabs ?? new GameObject[0];
    }


    private void OnEnable()
    {
        GameEvents.OnBuildProgressChanged += HandleBuildProgressChanged;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnWaveEnded += HandleWaveEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnBuildProgressChanged -= HandleBuildProgressChanged;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnWaveEnded -= HandleWaveEnded;
    }

    /// <summary>
    /// Wave càng cao thì càng mở khoá thêm loại enemy trong bộ của stage: wave 1 chỉ dùng prefab đầu
    /// tiên, wave 2 thêm prefab thứ hai... Nhờ vậy độ đa dạng tăng dần theo stage mà không phải
    /// cấu hình thêm gì trong Inspector — chỉ cần xếp stageEnemySets theo thứ tự dễ đến khó.
    /// </summary>
    private void HandleWaveStarted(int wave)
    {
        int available = currentEnemyPrefabs != null ? currentEnemyPrefabs.Length : 0;
        unlockedPrefabCount = Mathf.Clamp(wave, 1, Mathf.Max(1, available));
    }

    // Lưu ý: WaveManager raise WaveEnded rồi WaveStarted ngay trong cùng một frame, nên khoảng nghỉ
    // phải được đặt ở đây và KHÔNG được reset trong HandleWaveStarted.
    private void HandleWaveEnded(int wave)
    {
        waveBreakUntil = Time.time + Mathf.Max(0f, waveBreakDuration);
    }

    private void Start()
    {
        ApplySpawnScaling();
        StartCoroutine(SpawnEnemyRoutine());
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

            // Đang trong khoảng nghỉ giữa hai wave -> chờ hết rồi mới spawn tiếp.
            while (Time.time < waveBreakUntil)
            {
                yield return null;
            }

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

        // Chỉ bốc trong số loại enemy đã được wave hiện tại mở khoá.
        int unlocked = Mathf.Clamp(unlockedPrefabCount, 1, currentEnemyPrefabs.Length);

        for (int i = 0; i < groupSize; i++)
        {
            GameObject prefab = currentEnemyPrefabs[Random.Range(0, unlocked)];
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
