using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bảng chỉnh âm lượng, dùng chung cho Main Menu lẫn trong trận.
///
/// Panel tự dựng layout khi các tham chiếu để trống (cùng kiểu với ShopPanelUI/WeaponShopUI), nên
/// gọi CreateRuntimePanel là có ngay bảng chạy được mà không cần thiết kế gì trong Editor. Nếu sau
/// này bạn tự dựng panel đẹp hơn, đặt tên object chứa "music"/"sfx"/"close" là nó tự bind và bỏ
/// qua phần tự dựng.
///
/// Âm lượng ghi vào PlayerPrefs bằng đúng hai khoá mà GameAudioManager và MainMenuController đọc
/// lúc Awake, nên chỉnh ở scene nào cũng có hiệu lực ở scene còn lại.
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    public const string MusicVolumeKey = "MusicVolume";
    public const string SfxVolumeKey = "SFXVolume";
    private const float DefaultMusicVolume = 0.5f;
    private const float DefaultSfxVolume = 0.8f;

    [Header("UI References — để trống thì tự dựng lúc chạy")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeButton;
    [Tooltip("Nút thoát về Main Menu. Chỉ có ý nghĩa khi bảng này đang làm màn pause.")]
    [SerializeField] private Button mainMenuButton;

    [Header("Nhãn % — tuỳ chọn, để trống thì không hiện số")]
    [SerializeField] private TMP_Text musicValueLabel;
    [SerializeField] private TMP_Text sfxValueLabel;

    // Panel này thường được dựng lúc chạy nên không có prefab nào để kéo sprite vào Inspector.
    // Vì vậy khi ô để trống, nó tự nạp theo đường dẫn Resources cố định bên dưới — bạn chỉ cần thả
    // file ảnh đúng chỗ đúng tên là icon hiện ra, không phải sửa code hay Inspector.
    [Header("Icon — để trống thì tự nạp từ Resources/UI/Icons/")]
    [Tooltip("Bỏ trống -> tìm Resources/UI/Icons/Music")]
    [SerializeField] private Sprite musicIcon;
    [Tooltip("Bỏ trống -> tìm Resources/UI/Icons/SFX")]
    [SerializeField] private Sprite sfxIcon;

    private const string MusicIconResourcePath = "UI/Icons/Music";
    private const string SfxIconResourcePath = "UI/Icons/SFX";

    // --- Bảng màu ---------------------------------------------------------
    // Lấy theo tông của PausePanel: nền tím xám, viền hồng. Đổi 4 hằng này là đổi toàn bộ giao diện
    // được sinh ra, không phải đi sửa từng chỗ AddComponent<Image>().
    private static readonly Color AccentColor = new Color(0.93f, 0.55f, 0.87f, 1f);   // hồng viền panel
    private static readonly Color TrackColor = new Color(0.22f, 0.18f, 0.30f, 0.85f); // rãnh slider
    private static readonly Color HandleColor = new Color(1f, 0.95f, 0.99f, 1f);      // núm kéo
    private static readonly Color ValueTextColor = new Color(0.95f, 0.74f, 0.92f, 1f);// số phần trăm
    private static readonly Color CardColor = new Color(0.30f, 0.25f, 0.40f, 0.98f);  // nền card tự dựng

    // Cache thay vì FindFirstObjectByType mỗi lần đổi giá trị: onValueChanged bắn mỗi frame trong
    // lúc kéo slider, quét cả scene từng frame là phí thấy rõ.
    private MainMenuController menuController;
    private Image musicIconImage;
    private Image sfxIconImage;
    private bool hasUnsavedVolume;

    /// <summary>Dựng sẵn một bảng settings phủ toàn màn hình dưới <paramref name="parent"/>.</summary>
    public static GameObject CreateRuntimePanel(Transform parent)
    {
        GameObject panel = new GameObject("RuntimeSettingsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image backdrop = panel.GetComponent<Image>();
        backdrop.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);

        panel.AddComponent<SettingsPanelUI>();
        return panel;
    }

    private void Awake()
    {
        AutoBindExistingReferences();
        EnsureRuntimeLayout();
        ApplyIcons();

        menuController = FindFirstObjectByType<MainMenuController>();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
            closeButton.onClick.AddListener(CloseSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }

    private void OnEnable()
    {
        BindSlider(musicSlider, MusicVolumeKey, DefaultMusicVolume, OnMusicVolumeChanged, musicValueLabel);
        BindSlider(sfxSlider, SfxVolumeKey, DefaultSfxVolume, OnSFXVolumeChanged, sfxValueLabel);
    }

    private void OnDisable()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }

        // Flush một lần lúc đóng bảng. GameAudioManager/MainMenuController chỉ SetFloat chứ không
        // Save() nữa, vì Save() ghi thẳng xuống đĩa mà onValueChanged thì bắn mỗi frame khi kéo.
        if (hasUnsavedVolume)
        {
            PlayerPrefs.Save();
            hasUnsavedVolume = false;
        }
    }

    private void BindSlider(Slider slider, string prefsKey, float defaultValue,
                            UnityEngine.Events.UnityAction<float> handler, TMP_Text valueLabel)
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.RemoveListener(handler);

        // Âm lượng của AudioSource luôn nằm trong 0..1. Slider bạn tự kéo trong Editor có thể để
        // thang khác (Unity mặc định 0..1 nhưng rất dễ sửa nhầm thành 0..100), và khi đó mọi giá
        // trị đọc/ghi đều lệch. Ép lại thang ngay đây để panel tự thiết kế không thể sai chỗ này.
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        // SetValueWithoutNotify: gán value lúc mở bảng không được tính là người chơi vừa chỉnh,
        // nếu không mỗi lần mở panel lại ghi PlayerPrefs một lần vô ích.
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(prefsKey, defaultValue));
        slider.onValueChanged.AddListener(handler);

        UpdateValueLabel(valueLabel, slider.value);
    }

    private void OnMusicVolumeChanged(float volume)
    {
        hasUnsavedVolume = true;

        if (menuController != null)
        {
            menuController.SetMusicVolume(volume);
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetMusicVolume(volume);
        }

        UpdateValueLabel(musicValueLabel, volume);
    }

    private void OnSFXVolumeChanged(float volume)
    {
        hasUnsavedVolume = true;

        if (menuController != null)
        {
            menuController.SetSFXVolume(volume);
        }

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetSFXVolume(volume);
        }

        UpdateValueLabel(sfxValueLabel, volume);
    }

    private static void UpdateValueLabel(TMP_Text label, float volume01)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(volume01 * 100f)}%";
        }
    }

    /// <summary>
    /// Đóng bảng. Gắn được thẳng vào OnClick của Button trong Inspector.
    ///
    /// Khi bảng này đang đóng vai màn pause thì chỉ ẩn đi là chưa đủ — Time.timeScale vẫn nằm ở 0
    /// và game đứng hình vĩnh viễn. Nhả pause qua GameManager để nó tự ẩn panel và raise event.
    /// Ở Main Menu không có GameManager nên nhánh này bỏ qua, chỉ ẩn panel như thường.
    /// </summary>
    public void CloseSettings()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            GameManager.Instance.TogglePause();
            return;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Thoát về Main Menu. Gắn được thẳng vào OnClick của Button trong Inspector.
    ///
    /// Tiến trình KHÔNG mất: stage, DataPack, power-up và booster đều đã được ghi vào PlayerPrefs từ
    /// trước (xem RunProgress.SaveRun). Thứ duy nhất bỏ dở là lượt chơi của stage hiện tại — bấm
    /// Continue ở menu sẽ vào lại đúng stage đó từ đầu.
    /// </summary>
    public void GoToMainMenu()
    {
        // timeScale đang là 0 vì game đang pause. Không trả về 1 thì scene Main Menu cũng đứng hình.
        Time.timeScale = 1f;
        SceneTransition.LoadScene("MainMenuScene");
    }

    // --- Tự bind / tự dựng -------------------------------------------------

    private void AutoBindExistingReferences()
    {
        foreach (Slider slider in GetComponentsInChildren<Slider>(true))
        {
            string objectName = slider.name.ToLowerInvariant();

            if (musicSlider == null && (objectName.Contains("music") || objectName.Contains("nhac")))
            {
                musicSlider = slider;
            }
            else if (sfxSlider == null && (objectName.Contains("sfx") || objectName.Contains("sound")))
            {
                sfxSlider = slider;
            }
        }

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            string objectName = button.name.ToLowerInvariant();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string labelText = label != null ? label.text.ToLowerInvariant() : string.Empty;

            // Xét cả chữ trên nút, không chỉ tên object: nút trong bảng pause hay được đặt tên trống
            // rỗng kiểu "Button" nhưng nhãn thì luôn ghi rõ RESUME / MAIN MENU.
            //
            // Kiểm "menu" TRƯỚC để nút MAIN MENU không bị nhánh dưới nhận nhầm.
            if (mainMenuButton == null && (ContainsMenuWord(objectName) || ContainsMenuWord(labelText)))
            {
                mainMenuButton = button;
                continue;
            }

            if (closeButton == null && (ContainsCloseWord(objectName) || ContainsCloseWord(labelText)))
            {
                closeButton = button;
            }
        }
    }

    private static bool ContainsMenuWord(string value)
    {
        return value.Contains("menu") || value.Contains("sanh");
    }

    private static bool ContainsCloseWord(string value)
    {
        return value.Contains("close")
            || value.Contains("resume")
            || value.Contains("back")
            || value.Contains("dong")
            || value.Contains("tiep");
    }

    private void EnsureRuntimeLayout()
    {
        if (musicSlider != null && sfxSlider != null)
        {
            if (closeButton == null)
            {
                closeButton = CreateCloseButton((RectTransform)transform);
            }

            return; // slider đã có sẵn -> không dựng gì thêm
        }

        // Panel đã được thiết kế sẵn (khung + nút thoát, ví dụ PausePanel với WindowBackground và
        // nút RESUME): chỉ chèn hai hàng slider vào ĐÚNG khung đang chứa nút đó. Dựng card riêng ở
        // đây sẽ vẽ một nền đục trùm lên toàn bộ khung và chữ có sẵn.
        RectTransform host = closeButton != null ? closeButton.transform.parent as RectTransform : null;
        if (host != null)
        {
            // Toạ độ tính theo khung PausePanel 728x339 (tâm là gốc): tiêu đề chiếm +65..+15, nút
            // thoát đã được dời xuống -120..-150, nên hai hàng nằm gọn trong khoảng giữa.
            musicSlider = CreateSliderRow(host, "MusicSlider", "MUSIC", 44f, -25f,
                out musicIconImage, out musicValueLabel);
            sfxSlider = CreateSliderRow(host, "SFXSlider", "SFX", 44f, -80f,
                out sfxIconImage, out sfxValueLabel);
            return;
        }

        RectTransform card = CreateRect("SettingsCard", transform);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(620f, 400f);

        Image cardBackground = card.gameObject.AddComponent<Image>();
        cardBackground.color = CardColor;

        CreateLabel("Title", card, "SETTINGS", 44f, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -96f), new Vector2(-24f, -24f));

        musicSlider = CreateSliderRow(card, "MusicSlider", "MUSIC", 56f, 50f,
            out musicIconImage, out musicValueLabel);
        sfxSlider = CreateSliderRow(card, "SFXSlider", "SFX", 56f, -30f,
            out sfxIconImage, out sfxValueLabel);

        if (closeButton == null)
        {
            closeButton = CreateCloseButton(card);
        }
    }

    /// <summary>
    /// Một hàng: [icon] [nhãn] [slider] [giá trị %].
    ///
    /// Neo theo TÂM của parent (<paramref name="centerY"/> tính từ tâm) chứ không theo mép trên, để
    /// dùng chung được cho cả card tự dựng lẫn khung có sẵn của người dùng — hai thứ có chiều cao
    /// khác nhau nên toạ độ tính từ mép trên sẽ không tái sử dụng được.
    /// </summary>
    private Slider CreateSliderRow(RectTransform parent, string sliderName, string label,
                                   float rowHeight, float centerY,
                                   out Image iconImage, out TMP_Text valueLabel)
    {
        RectTransform row = CreateRect(sliderName + "Row", parent);
        row.anchorMin = new Vector2(0f, 0.5f);
        row.anchorMax = new Vector2(1f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(-80f, rowHeight); // -80 = chừa lề 40px mỗi bên
        row.anchoredPosition = new Vector2(0f, centerY);

        RectTransform iconRect = CreateRect("Icon", row);
        SetRect(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(44f, 44f);
        iconRect.anchoredPosition = Vector2.zero;

        iconImage = iconRect.gameObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false; // bật lên trong ApplyIcons() nếu có sprite

        RectTransform labelRect = CreateRect("Label", row);
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(120f, 44f);
        labelRect.anchoredPosition = new Vector2(56f, 0f);

        TMP_Text labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 24f;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        RectTransform valueRect = CreateRect("Value", row);
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(80f, 44f);
        valueRect.anchoredPosition = Vector2.zero;

        valueLabel = valueRect.gameObject.AddComponent<TextMeshProUGUI>();
        valueLabel.fontSize = 24f;
        valueLabel.alignment = TextAlignmentOptions.Right;
        valueLabel.color = ValueTextColor;
        valueLabel.raycastTarget = false;

        return CreateSlider(row, sliderName);
    }

    /// <summary>
    /// Dựng một Slider hoàn chỉnh bằng code. Unity đòi đủ Background + Fill + Handle và phải gán
    /// fillRect/handleRect, thiếu một cái là slider trông như thanh trắng không kéo được.
    /// </summary>
    private static Slider CreateSlider(RectTransform parent, string sliderName)
    {
        RectTransform sliderRect = CreateRect(sliderName, parent);
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.offsetMin = new Vector2(190f, -10f);
        sliderRect.offsetMax = new Vector2(-100f, 10f);

        Slider slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform background = CreateRect("Background", sliderRect);
        SetRect(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = TrackColor;

        RectTransform fillArea = CreateRect("Fill Area", sliderRect);
        SetRect(fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-14f, 0f));

        RectTransform fill = CreateRect("Fill", fillArea);
        SetRect(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = AccentColor;

        RectTransform handleArea = CreateRect("Handle Slide Area", sliderRect);
        SetRect(handleArea, Vector2.zero, Vector2.one, new Vector2(7f, 0f), new Vector2(-7f, 0f));

        RectTransform handle = CreateRect("Handle", handleArea);
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(22f, 0f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = HandleColor;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private static Button CreateCloseButton(RectTransform parent)
    {
        RectTransform buttonRect = CreateRect("CloseButton", parent);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(220f, 58f);
        buttonRect.anchoredPosition = new Vector2(0f, 32f);

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = AccentColor;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform textRect = CreateRect("Label", buttonRect);
        SetRect(textRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = "CLOSE";
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }

    private void ApplyIcons()
    {
        SetIcon(musicIconImage, musicIcon != null ? musicIcon : Resources.Load<Sprite>(MusicIconResourcePath));
        SetIcon(sfxIconImage, sfxIcon != null ? sfxIcon : Resources.Load<Sprite>(SfxIconResourcePath));
    }

    private static void SetIcon(Image target, Sprite sprite)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private static TMP_Text CreateLabel(string name, RectTransform parent, string content, float fontSize,
                                        TextAlignmentOptions alignment,
                                        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = CreateRect(name, parent);
        SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);

        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
