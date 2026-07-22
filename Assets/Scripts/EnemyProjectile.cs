using UnityEngine;

/// <summary>
/// Đạn của enemy Ranged. Bay thẳng theo hướng bắn, gây damage cho Player hoặc Server rồi tự huỷ.
/// Không dùng ObjectPool vì số lượng nhỏ hơn đạn người chơi rất nhiều; lifeTime đảm bảo không rò rỉ.
/// Nếu EnemyConfig không gán projectilePrefab, CreateDefault() dựng một viên đạn tối giản bằng
/// primitive để tính năng chạy được ngay mà không cần setup Inspector.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;

    private static Sprite defaultSprite;

    private float speed;
    private int damage;
    private Vector2 direction;
    private bool launched;

    void Awake()
    {
        // Trigger 2D chỉ phát sự kiện khi ít nhất một bên có Rigidbody2D. Ép kinematic + gravity 0
        // ngay tại đây để prefab do người dùng tự tạo cũng hoạt động mà không cần chỉnh Inspector
        // (Rigidbody2D mặc định có gravityScale = 1 sẽ làm đạn rơi xuống).
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>Bắn viên đạn đi. Gọi ngay sau khi Instantiate.</summary>
    public void Launch(Vector2 aimDirection, int projectileDamage, float projectileSpeed)
    {
        direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
        damage = projectileDamage;
        speed = projectileSpeed;
        launched = true;

        transform.right = direction; // xoay theo hướng bay cho sprite có hướng
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!launched)
        {
            return;
        }

        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            return; // đạn enemy không bắn trúng nhau
        }

        // Chỉ Player và Server mới ăn damage — tránh trúng nhầm đạn của người chơi.
        bool isValidTarget = other.CompareTag("Player") || other.GetComponent<ServerCore>() != null;
        if (isValidTarget && other.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Dựng một viên đạn tối giản (ô vuông phát sáng) khi EnemyConfig chưa có projectilePrefab.
    /// Dùng Texture2D.whiteTexture nên không phụ thuộc asset nào trong project.
    /// </summary>
    public static EnemyProjectile CreateDefault(Vector3 position, Color color)
    {
        GameObject projectileObject = new GameObject("EnemyProjectile");
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetDefaultSprite();
        renderer.color = color;
        renderer.sortingOrder = 50;

        CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        // AddComponent sau collider để RequireComponent không tự chèn collider thừa.
        return projectileObject.AddComponent<EnemyProjectile>();
    }

    /// <summary>Sprite 1x1 dùng chung cho mọi viên đạn mặc định — tạo một lần rồi tái sử dụng.</summary>
    private static Sprite GetDefaultSprite()
    {
        if (defaultSprite == null)
        {
            defaultSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        return defaultSprite;
    }
}
