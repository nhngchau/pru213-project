using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject bulletPrefab; // Nơi nhét Prefab đạn vào
    [SerializeField] private Transform firePoint;     // Vị trí nòng súng (nơi đạn bay ra)

    void Update()
    {
        // Kiểm tra người chơi bấm Chuột Trái (Fire1)
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // 1. Lấy vị trí chuột trên màn hình, quy đổi ra tọa độ thế giới game
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Tính toán hướng bay: Điểm đến (Chuột) trừ Điểm đi (Nhân vật)
        Vector2 lookDirection = mousePos - transform.position;

        // Dùng lượng giác Atan2 để tính ra góc xoay (độ) cho viên đạn
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

        // 2. Ép viên đạn sinh ra và xoay đúng góc đó
        Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, angle));
    }
}