using UnityEngine;

/// <summary>
/// Thống kê của run hiện tại để tính điểm hiển thị ở màn thắng/thua.
/// Công thức: Stage × 1000 + Level × 250 + số quái tiêu diệt × 10 + DataPack.
/// </summary>
public static class RunStats
{
    public static int EnemiesKilled { get; private set; }

    /// <summary>Điểm tính theo trạng thái hiện tại của run.</summary>
    public static int Score => ComputeScore(RunProgress.Stage, RunProgress.PlayerLevel, EnemiesKilled, RunProgress.DataPack);

    public static int ComputeScore(int stage, int level, int kills, int dataPack)
    {
        return Mathf.Max(0, stage) * 1000
             + Mathf.Max(0, level) * 250
             + Mathf.Max(0, kills) * 10
             + Mathf.Max(0, dataPack);
    }

    public static void Reset()
    {
        EnemiesKilled = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Đăng ký một lần cho cả phiên chơi. Gỡ trước khi gắn để phòng trường hợp
        // Unity tắt domain reload khiến static còn giữ đăng ký cũ.
        GameEvents.OnEnemyDied -= HandleEnemyDied;
        GameEvents.OnEnemyDied += HandleEnemyDied;
    }

    private static void HandleEnemyDied(Vector3 _)
    {
        EnemiesKilled++;
    }
}
