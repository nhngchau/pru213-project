using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject syntaxErrorPrefab; // Kéo Prefab con quái vào đây
    [SerializeField] private float spawnInterval = 2f;     // Cứ 2 giây sinh 1 con
    [SerializeField] private float spawnRadius = 10f;      // Sinh ra cách nhân vật 10 mét

    private Transform player;

    void Start()
    {
        // Tìm vị trí nhân vật
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Bắt đầu tiến trình sinh quái chạy ngầm
        StartCoroutine(SpawnEnemyRoutine());
    }

    // Coroutine: Vòng lặp thời gian trong Unity
    IEnumerator SpawnEnemyRoutine()
    {
        while (true) // Lặp vô hạn cho đến khi game over
        {
            yield return new WaitForSeconds(spawnInterval); // Tạm dừng 2 giây
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (player != null)
        {
            // 1. Random.insideUnitCircle lấy 1 điểm ngẫu nhiên trên 1 vòng tròn
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            // 2. Tính toán tọa độ sinh quái xung quanh người chơi
            Vector2 spawnPos = (Vector2)player.position + (randomDirection * spawnRadius);

            // 3. Đưa quái vật vào bản đồ
            Instantiate(syntaxErrorPrefab, spawnPos, Quaternion.identity);
        }
    }
}