using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Đầu vào cho chức năng tạm dừng: phím Esc và nút trên màn hình.
/// Tự sinh ra trong GameScene nên không cần kéo thả vào scene bằng tay.
/// </summary>
public class PauseController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Nút tạm dừng")]
    [Tooltip("Nút 3 gạch có sẵn trên Canvas. Nối bằng: GDD Tools > Wire Selected Button As Pause Button.")]
    [SerializeField] private Button pauseButton;
    [Tooltip("Bật thì tự tạo một nút ☰ ở góc trên phải khi chưa nối nút nào. " +
             "Mặc định tắt vì nút tự tạo không khớp vị trí với UI đã thiết kế sẵn.")]
    [SerializeField] private bool createButtonIfMissing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // RuntimeInitializeOnLoadMethod chỉ chạy MỘT lần lúc mở app, không chạy lại mỗi
        // lần load scene. Phải bám vào sceneLoaded thì vào GameScene từ Main Menu mới có
        // PauseController.
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureExistsForCurrentScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureExistsForCurrentScene();
    }

    public static void EnsureExistsForCurrentScene()
    {
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            return;
        }

        if (FindFirstObjectByType<PauseController>() != null)
        {
            return;
        }

        new GameObject("PauseController").AddComponent<PauseController>();
    }

    private void OnEnable()
    {
        GameEvents.OnGamePaused += HandleGamePaused;
    }

    private void OnDisable()
    {
        GameEvents.OnGamePaused -= HandleGamePaused;
    }

    /// <summary>
    /// Tự bật/tắt pop-up để chức năng pause không phụ thuộc vào việc GameUIManager
    /// có mặt hay không. Show/Hide đều idempotent nên trùng với GameUIManager cũng vô hại.
    /// </summary>
    private void HandleGamePaused(bool isPaused)
    {
        if (isPaused)
        {
            PauseMenuUI.Show();
            return;
        }

        PauseMenuUI.Hide();
        OptionsPanelUI.Close();
    }

    private void Start()
    {
        if (pauseButton == null && createButtonIfMissing)
        {
            pauseButton = CreatePauseButton();
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
            pauseButton.onClick.AddListener(TogglePause);
            return;
        }

        Debug.Log($"[PauseController] Chưa nối nút tạm dừng nào — hiện chỉ dừng được bằng phím {pauseKey}. " +
                  "Chọn nút 3 gạch trong Hierarchy rồi chạy: GDD Tools > Wire Selected Button As Pause Button.");
    }

    private void Update()
    {
        if (!Input.GetKeyDown(pauseKey))
        {
            return;
        }

        // Esc khi đang mở Option thì chỉ đóng Option, chưa thoát khỏi trạng thái pause.
        if (OptionsPanelUI.IsOpen)
        {
            OptionsPanelUI.Close();
            return;
        }

        TogglePause();
    }

    /// <summary>Gắn được trực tiếp vào onClick của một Button trong Inspector.</summary>
    public void TogglePause()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameEnded)
        {
            return;
        }

        // Đang mở modal chọn power-up thì bỏ qua, tránh chồng hai màn hình cùng dừng game.
        if (PlayerProgression.Instance != null && PlayerProgression.Instance.WaitingForPowerUpChoice)
        {
            return;
        }

        GameManager.Instance.TogglePause();
    }

    private Button CreatePauseButton()
    {
        Canvas canvas = RuntimeUI.CreateOverlayCanvas("PauseButtonCanvas", 500);
        canvas.transform.SetParent(transform, false);

        RectTransform rect = RuntimeUI.CreateRect("PauseButton", canvas.transform);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(64f, 64f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.10f, 0.16f, 0.22f, 0.85f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform labelRect = RuntimeUI.CreateRect("Label", rect);
        RuntimeUI.Stretch(labelRect);

        TMP_Text label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = "☰"; // ☰
        label.fontSize = 34f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return button;
    }
}
