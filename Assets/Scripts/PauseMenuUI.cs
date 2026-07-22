using UnityEngine;

/// <summary>
/// Pop-up tạm dừng, dựng bằng code lúc runtime: Resume / Restart / Option / Main Menu.
/// Được bật tắt bởi <see cref="GameUIManager"/> khi nhận event OnGamePaused.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private const int SortingOrder = 30000;

    private static PauseMenuUI instance;

    public static bool IsOpen => instance != null;

    public static void Show()
    {
        if (instance != null)
        {
            return;
        }

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("PauseMenuCanvas", SortingOrder);
        instance = canvas.gameObject.AddComponent<PauseMenuUI>();
        instance.Build(canvas.transform);
    }

    public static void Hide()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    private void Build(Transform root)
    {
        RuntimeUI.CreateBackdrop(root, new Color(0f, 0f, 0f, 0.72f));

        RectTransform panel = RuntimeUI.CreateMenuPanel(root, new Vector2(580f, 0f));

        RuntimeUI.CreateText(panel, "PAUSED", 44f, RuntimeUI.AccentColor, 60f);
        RuntimeUI.CreateText(
            panel,
            $"Stage {RunProgress.Stage}   -   Level {RunProgress.PlayerLevel}   -   Score {RunStats.Score}",
            20f,
            new Color(0.70f, 0.76f, 0.82f, 1f),
            32f);

        RuntimeUI.CreateButton(panel, "RESUME", OnResume);
        RuntimeUI.CreateButton(panel, "RESTART", OnRestart);
        RuntimeUI.CreateButton(panel, "OPTION", OnOption);
        RuntimeUI.CreateButton(panel, "MAIN MENU", OnMainMenu, RuntimeUI.DangerColor);
    }

    private void OnResume()
    {
        // GameManager bắn OnGamePaused(false) → GameUIManager gọi Hide().
        GameManager.Instance?.TogglePause();
    }

    private void OnRestart()
    {
        // Chơi lại stage hiện tại, giữ nguyên tiến trình run.
        Time.timeScale = 1f;
        RunStats.Reset();
        SceneTransition.LoadScene("GameScene");
    }

    private void OnOption()
    {
        // Panel Option có sortingOrder cao hơn nên nằm đè lên pop-up này.
        OptionsPanelUI.Show();
    }

    private void OnMainMenu()
    {
        Time.timeScale = 1f;
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
