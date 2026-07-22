using UnityEngine;

/// <summary>
/// Cách một enemy chọn mục tiêu và di chuyển. Quyết định nhánh AI chạy trong EnemyBehavior.
/// Mỗi archetype tạo một kiểu áp lực khác nhau lên người chơi.
/// </summary>
public enum EnemyArchetype
{
    Chaser, // lao thẳng vào Server — áp lực công thành, buộc người chơi phải dọn đường
    Hunter, // đuổi theo Player — buộc người chơi phải di chuyển, không đứng yên bắn được
    Ranged  // giữ khoảng cách với Player và bắn — buộc người chơi phải áp sát hoặc né
}

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "The Senior Defender/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Tên hiển thị trên HUD khi loại bug này đang chặn build.")]
    public string displayName = "BUG";

    [Header("Behaviour")]
    [Tooltip("Chaser = lao vào Server (mặc định) | Hunter = đuổi Player | Ranged = giữ khoảng cách và bắn Player")]
    public EnemyArchetype archetype = EnemyArchetype.Chaser;

    [Tooltip("Còn con này sống thì Build Progress đứng yên — code không compile được thì build không chạy. " +
             "Buộc người chơi bỏ vị trí an toàn để đi xử lý nó.")]
    public bool blocksBuildProgress;

    [Header("Stats")]
    [Min(1)] public int maxHP = 20;
    [Min(0f)] public float moveSpeed = 3f;
    [Tooltip("Damage gây ra khi chạm Server. Enemy tự huỷ ngay sau đó nên đây là sát thương một lần.")]
    [Min(0)] public int damageToServer = 10;
    [Tooltip("Damage gây ra khi chạm Player. Cũng là sát thương một lần rồi tự huỷ.")]
    [Min(0)] public int damageToPlayer = 10;
    [Min(0)] public int dataPackValue = 2;
    [Min(0)] public int expReward = 10;

    [Header("Ranged (chỉ dùng khi archetype = Ranged)")]
    [Tooltip("Khoảng cách enemy muốn giữ với mục tiêu. Xa hơn thì tiến lại, gần hơn thì lùi ra.")]
    [Min(0.5f)] public float preferredRange = 6f;
    [Tooltip("Số giây giữa hai phát bắn.")]
    [Min(0.1f)] public float attackInterval = 2f;
    [Min(0)] public int projectileDamage = 8;
    [Min(0.5f)] public float projectileSpeed = 7f;
    [Tooltip("Để trống thì đạn sẽ được dựng bằng primitive lúc chạy — vẫn hoạt động, chỉ là không có sprite riêng.")]
    public GameObject projectilePrefab;

    [Header("Death")]
    [Min(0f)] public float deathDelay = 0.6f;
    public GameObject onDeathEffectPrefab;
    [Tooltip("Vũng nguy hiểm để lại khi bị bắn chết — ví dụ Sludge.prefab. Để trống nếu enemy này không để lại gì.")]
    public GameObject onDeathHazardPrefab;
}
