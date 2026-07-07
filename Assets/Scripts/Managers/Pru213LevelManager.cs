using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.TopDownEngine;
using MoreMountains.Tools;

namespace pru213
{
    [System.Serializable]
    public struct Wave
    {
        [Tooltip("Thời gian đếm ngược kích hoạt đợt này (Ví dụ: 45 nghĩa là đợt này xuất hiện ở giây thứ 45 của màn chơi)")]
        public float SpawnTime;
        [Tooltip("Danh sách quái vật trong đợt")]
        public GameObject[] MonsterPrefabs;
        [Tooltip("Số lượng quái vật sinh ra")]
        public int MonsterCount;
        [Tooltip("Khoảng cách spawn tối thiểu từ Center")]
        public float MinRadius;
        [Tooltip("Khoảng cách spawn tối đa từ Center")]
        public float MaxRadius;
        [Tooltip("Bán kính nhóm. Các quái vật trong đợt sẽ xuất hiện cạnh nhau trong bán kính này.")]
        public float GroupRadius;
        [HideInInspector] public bool hasSpawned;
    }

    public class Pru213LevelManager : MonoBehaviour, MMEventListener<TopDownEngineEvent>
    {
        public static Pru213LevelManager Instance { get; private set; }

        [Header("Character Spawn")]
        [Tooltip("Prefab của người chơi (Player)")]
        public Character PlayerPrefab;
        [Tooltip("Điểm xuất hiện ban đầu của người chơi")]
        public Transform InitialSpawnPoint;

        [Header("Level State & UI")]
        [Tooltip("Thời gian đếm ngược của màn chơi (giây)")]
        public float LevelDuration = 60f;
        [MMReadOnly]
        public float CurrentTimeLeft;

        [Header("Waves Setup")]
        [Tooltip("Cấu hình các đợt quái xuất hiện theo thời gian")]
        public List<Wave> Waves = new List<Wave>();

        [Header("Support Items")]
        [Tooltip("Thời gian giữa 2 lần spawn vật phẩm hỗ trợ (giây)")]
        public float ItemSpawnInterval = 15f;
        [Tooltip("Danh sách các prefab vật phẩm hỗ trợ (Pickable Items)")]
        public GameObject[] ItemPrefabs;
        [Tooltip("Khoảng cách spawn vật phẩm tối thiểu từ Center")]
        public float ItemMinRadius = 2f;
        [Tooltip("Khoảng cách spawn vật phẩm tối đa từ Center")]
        public float ItemMaxRadius = 15f;

        [Header("Level Bounds & Environment")]
        [Tooltip("Dùng để giới hạn vị trí spawn và set Confiner cho Camera (3D)")]
        public Collider BoundsCollider;
        [Tooltip("Dùng để giới hạn vị trí spawn và set Confiner cho Camera (2D)")]
        public Collider2D BoundsCollider2D;
        [Tooltip("Điểm trung tâm của màn chơi (nơi đặt ServerCore). Quái và item sẽ spawn quanh điểm này.")]
        public Transform CenterPoint;
        [Tooltip("Đánh dấu nếu game là 2D (spawn trên mặt phẳng XY thay vì XZ)")]
        public bool Is2D = false;

        private float nextItemSpawnTime;
        private List<Character> players = new List<Character>();
        
        [Header("Object Pooling")]
        [Tooltip("Optional: assigns a specific MMMultipleObjectPooler to use for spawning. If left empty, one will be created automatically.")]
        public MMMultipleObjectPooler ObjectPooler;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CurrentTimeLeft = LevelDuration;
            nextItemSpawnTime = LevelDuration - ItemSpawnInterval;

            InitializePools();
            SpawnPlayer();

            // Kích hoạt các event khởi tạo của TopDown Engine
            TopDownEngineEvent.Trigger(TopDownEngineEventTypes.LevelStart, null);
        }

        private void InitializePools()
        {
            if (ObjectPooler == null)
            {
                GameObject poolGO = new GameObject("Level_ObjectPooler");
                poolGO.transform.SetParent(this.transform);
                ObjectPooler = poolGO.AddComponent<MMMultipleObjectPooler>();
                ObjectPooler.MutualizedPoolName = "LevelPooler";
                ObjectPooler.Pool = new List<MMMultipleObjectPoolerObject>();
            }

            // Tạo pool cho quái vật
            foreach (Wave w in Waves)
            {
                if (w.MonsterPrefabs == null) continue;
                foreach (GameObject prefab in w.MonsterPrefabs)
                {
                    AddPrefabToPool(prefab, w.MonsterCount);
                }
            }

            // Tạo pool cho items
            if (ItemPrefabs != null)
            {
                foreach (GameObject itemPrefab in ItemPrefabs)
                {
                    AddPrefabToPool(itemPrefab, 5); // Tạo sẵn 5 item dự trữ, PoolCanExpand = true
                }
            }
            
            ObjectPooler.FillObjectPool();
        }

        private void AddPrefabToPool(GameObject prefab, int size)
        {
            if (prefab == null) return;

            foreach (var p in ObjectPooler.Pool)
            {
                if (p.GameObjectToPool == prefab)
                {
                    if (size > p.PoolSize) p.PoolSize = size;
                    return;
                }
            }

            MMMultipleObjectPoolerObject poolObj = new MMMultipleObjectPoolerObject();
            poolObj.GameObjectToPool = prefab;
            poolObj.PoolSize = size;
            poolObj.PoolCanExpand = true;
            poolObj.Enabled = true;
            ObjectPooler.Pool.Add(poolObj);
        }

        private void SpawnPlayer()
        {
            if (PlayerPrefab == null) return;

            Vector3 spawnPos = InitialSpawnPoint != null ? InitialSpawnPoint.position : Vector3.zero;
            Character newPlayer = Instantiate(PlayerPrefab, spawnPos, Quaternion.identity);
            newPlayer.name = PlayerPrefab.name;
            players.Add(newPlayer);

            TopDownEngineEvent.Trigger(TopDownEngineEventTypes.SpawnComplete, newPlayer);
            
            // Thiết lập Camera theo dõi người chơi
            MMCameraEvent.Trigger(MMCameraEventTypes.SetTargetCharacter, newPlayer);
            MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
            MMGameEvent.Trigger("CameraBound");

            // Thiết lập giới hạn màn chơi cho Camera (Confiner)
            if (BoundsCollider != null || BoundsCollider2D != null)
            {
                MMCameraEvent.Trigger(MMCameraEventTypes.SetConfiner, null, BoundsCollider, BoundsCollider2D);
            }
        }

        private void Update()
        {
            // Nếu Server chết (hoặc game đã kết thúc), ngừng spawn và đếm ngược
            if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
            {
                return;
            }

            if (CurrentTimeLeft > 0)
            {
                CurrentTimeLeft -= Time.deltaTime;

                CheckWaves();
                CheckItemSpawn();

                if (CurrentTimeLeft <= 0)
                {
                    CurrentTimeLeft = 0;
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TriggerWin();
                    }
                }
            }
        }

        private void CheckWaves()
        {
            for (int i = 0; i < Waves.Count; i++)
            {
                Wave w = Waves[i];
                if (!w.hasSpawned && CurrentTimeLeft <= w.SpawnTime)
                {
                    SpawnWave(w);
                    w.hasSpawned = true;
                    Waves[i] = w; // Cập nhật lại struct trong List
                }
            }
        }

        private void SpawnWave(Wave wave)
        {
            if (wave.MonsterPrefabs == null || wave.MonsterPrefabs.Length == 0) return;

            // Xác định vị trí trung tâm của nhóm quái vật
            Vector3 groupCenterPos = GetRandomPosition(wave.MinRadius, wave.MaxRadius);

            for (int i = 0; i < wave.MonsterCount; i++)
            {
                GameObject prefab = wave.MonsterPrefabs[Random.Range(0, wave.MonsterPrefabs.Length)];
                
                // Random vị trí xung quanh groupCenterPos
                Vector2 randomOffset = Random.insideUnitCircle * wave.GroupRadius;
                Vector3 spawnPos;
                if (Is2D)
                {
                    spawnPos = groupCenterPos + new Vector3(randomOffset.x, randomOffset.y, 0);
                }
                else
                {
                    spawnPos = groupCenterPos + new Vector3(randomOffset.x, 0, randomOffset.y);
                }

                spawnPos = ClampToBounds(spawnPos);
                
                GameObject spawnedObj = SpawnFromPool(prefab, spawnPos);
            }
        }

        private GameObject SpawnFromPool(GameObject prefab, Vector3 spawnPos)
        {
            if (prefab == null) return null;
            
            GameObject pooledObj = ObjectPooler != null ? ObjectPooler.GetPooledGameObjectOfType(prefab.name) : null;
            
            if (pooledObj != null)
            {
                pooledObj.transform.position = spawnPos;
                pooledObj.transform.rotation = Quaternion.identity;
                pooledObj.SetActive(true);
                
                // Kích hoạt MM poolable nếu có
                MMPoolableObject poolable = pooledObj.GetComponent<MMPoolableObject>();
                if (poolable != null)
                {
                    poolable.TriggerOnSpawnComplete();
                }
                return pooledObj;
            }
            
            // Fallback nếu không có pool
            return Instantiate(prefab, spawnPos, Quaternion.identity);
        }

        private void CheckItemSpawn()
        {
            if (ItemPrefabs == null || ItemPrefabs.Length == 0) return;

            if (CurrentTimeLeft <= nextItemSpawnTime)
            {
                GameObject prefab = ItemPrefabs[Random.Range(0, ItemPrefabs.Length)];
                Vector3 spawnPos = GetRandomPosition(ItemMinRadius, ItemMaxRadius);
                SpawnFromPool(prefab, spawnPos);

                nextItemSpawnTime -= ItemSpawnInterval;
            }
        }

        private Vector3 GetRandomPosition(float minRadius, float maxRadius)
        {
            Vector3 center = CenterPoint != null ? CenterPoint.position : Vector3.zero;
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(minRadius, maxRadius);
            
            Vector3 targetPos;
            if (Is2D)
            {
                targetPos = center + new Vector3(randomDir.x, randomDir.y, 0) * randomDist;
            }
            else
            {
                targetPos = center + new Vector3(randomDir.x, 0, randomDir.y) * randomDist;
            }

            return ClampToBounds(targetPos);
        }
        
        private Vector3 ClampToBounds(Vector3 pos)
        {
            if (BoundsCollider != null)
            {
                return BoundsCollider.ClosestPoint(pos);
            }
            if (BoundsCollider2D != null)
            {
                return BoundsCollider2D.ClosestPoint(pos);
            }
            return pos;
        }

        [Header("Respawn Loop")]
        [Tooltip("Thời gian chờ trước khi hiện màn hình Game Over (giống LevelManager gốc)")]
        public float DelayBeforeDeathScreen = 1f;

        /// <summary>
        /// Gọi hàm này nếu người chơi chết
        /// </summary>
        public void PlayerDead()
        {
            StartCoroutine(PlayerDeadCo());
        }

        private IEnumerator PlayerDeadCo()
        {
            // Chờ một khoảng thời gian để play animation chết
            yield return new WaitForSeconds(DelayBeforeDeathScreen);

            // Bật Death Screen của TopDown Engine (nếu có UI prefab chuẩn của TDE)
            if (GUIManager.Instance != null)
            {
                GUIManager.Instance.SetDeathScreen(true);
            }

            // Gọi custom GameManager để xử lý cờ trạng thái (nếu có)
            if (GameManager.Instance != null && !GameManager.Instance.IsGameEnded)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }

        private void OnEnable()
        {
            this.MMEventStartListening<TopDownEngineEvent>();
        }

        private void OnDisable()
        {
            this.MMEventStopListening<TopDownEngineEvent>();
        }

        public void OnMMEvent(TopDownEngineEvent engineEvent)
        {
            switch (engineEvent.EventType)
            {
                case TopDownEngineEventTypes.PlayerDeath:
                    PlayerDead();
                    break;
            }
        }
    }
}
