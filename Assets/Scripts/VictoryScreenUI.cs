using UnityEngine;

/// <summary>
/// Màn hình chiến thắng sau khi clear stage cuối. Dựng bằng code vì
/// WinModal.prefab không có component Modal nên UnityScreenNavigator không push được.
/// </summary>
public class VictoryScreenUI : MonoBehaviour
{
    private const int SortingOrder = 30000;

    private static VictoryScreenUI instance;

    public static bool IsOpen => instance != null;

    /// <summary>
    /// Hiện màn thắng. Các chỉ số phải được chụp lại TRƯỚC khi xoá save,
    /// vì <c>RunProgress.ClearSavedRun()</c> đưa Stage về 1.
    /// </summary>
    public static void Show(int stage, int level, int kills, int score)
    {
        if (instance != null)
        {
            return;
        }

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("VictoryCanvas", SortingOrder);
        instance = canvas.gameObject.AddComponent<VictoryScreenUI>();
        instance.Build(canvas.transform, stage, level, kills, score);
    }

    public static void Hide()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    private void Build(Transform root, int stage, int level, int kills, int score)
    {
        RuntimeUI.CreateBackdrop(root, new Color(0f, 0.03f, 0.06f, 0.88f));

        RectTransform panel = RuntimeUI.CreateMenuPanel(root, new Vector2(740f, 0f), 14f);

        RuntimeUI.CreateText(panel, "CONGRATULATIONS!", 46f, RuntimeUI.AccentColor, 64f);
        RuntimeUI.CreateText(panel, $"You defended the Server through all {RunProgress.MaxStage} stages!",
            21f, Color.white, 56f);

        Color statColor = new Color(0.70f, 0.76f, 0.82f, 1f);
        RuntimeUI.CreateText(panel, $"Stage Reached: {stage}", 20f, statColor, 30f);
        RuntimeUI.CreateText(panel, $"Level: {level}", 20f, statColor, 30f);
        RuntimeUI.CreateText(panel, $"Bugs Eliminated: {kills}", 20f, statColor, 30f);

        RuntimeUI.CreateText(panel, $"SCORE: {score}", 36f, new Color(1f, 0.85f, 0.30f, 1f), 56f);

        RuntimeUI.CreateText(panel, string.Empty, 16f, Color.clear, 10f); // khoảng đệm
        RuntimeUI.CreateButton(panel, "RESTART", OnRestart);
        RuntimeUI.CreateButton(panel, "MAIN MENU", OnMainMenu, RuntimeUI.DangerColor);
    }

    private void OnRestart()
    {
        Time.timeScale = 1f;
        RunProgress.ResetRun();
        RunStats.Reset();
        SceneTransition.LoadScene("GameScene");
    }

    private void OnMainMenu()
    {
        Time.timeScale = 1f;
        RunProgress.ClearSavedRun();
        RunStats.Reset();
        SceneTransition.LoadScene("MainMenuScene");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
