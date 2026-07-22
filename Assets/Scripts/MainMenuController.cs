using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Guide Panel")]
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private Button continueButton;

    [Header("Best Stage")]
    [Tooltip("Text hiển thị kỷ lục stage cao nhất. Tự tìm object tên BestStageText nếu để trống.")]
    [SerializeField] private TMP_Text bestStageText;

    [Header("Button Hover Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("Menu Sound")]
    [Tooltip("Nhạc nền lặp lại khi đang ở Main Menu.")]
    [SerializeField] private AudioClip backgroundMusic;
    [Tooltip("Âm thanh phát khi rê chuột vào một button.")]
    [SerializeField] private AudioClip buttonHoverSound;
    [Tooltip("Âm thanh phát khi nhấn một button.")]
    [SerializeField] private AudioClip buttonClickSound;
    [Range(0f, 1f)] [SerializeField] private float defaultMusicVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float defaultSoundVolume = 0.8f;

    private float musicVolume;
    private float soundVolume;

    private Vector3 normalScale;
    private Vector3 targetScale;
    private AudioSource musicSource;
    private AudioSource soundSource;

    private void Awake()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;

        // Load volume from PlayerPrefs or use default
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        soundVolume = PlayerPrefs.GetFloat("SFXVolume", defaultSoundVolume);

        musicSource = CreateAudioSource("Menu Music", true, musicVolume);
        soundSource = CreateAudioSource("Menu SFX", false, soundVolume);
    }

    private void Start()
    {
        AutoBindContinueButton();
        AutoBindBestStageText();

        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }

        PlayBackgroundMusic();
        RegisterButtonSounds();
        RefreshContinueState();
        RefreshBestStage();
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, animationSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = normalScale * hoverScale;
        PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }

    public void OpenGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }
    }

    public void CloseGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    public void OpenWeaponShop()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            Debug.LogError("MainMenuController: không tìm thấy Canvas nào trong scene để mở Weapon Shop.");
            return;
        }

        GameObject panel = WeaponShopUI.CreateRuntimePanel(canvas.transform);
        panel.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Canvas để gắn panel vào.
    ///
    /// KHÔNG chỉ dùng GetComponentInParent: MainMenuManager nằm ở gốc scene, không nằm dưới Canvas,
    /// nên hàm đó luôn trả null và mọi panel mở từ menu đều chết lặng.
    /// </summary>
    private Canvas ResolveCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            return canvas;
        }

        // Ưu tiên canvas vẽ đè lên toàn màn hình, tránh vớ phải canvas world-space của HP bar.
        foreach (Canvas candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (candidate.renderMode != RenderMode.WorldSpace)
            {
                return candidate.rootCanvas != null ? candidate.rootCanvas : candidate;
            }
        }

        return null;
    }

    public void StartGame()
    {
        NewGame();
    }

    public void NewGame()
    {
        RunProgress.ResetRun();
        SceneTransition.LoadScene("GameScene");
    }

    public void ContinueGame()
    {
        if (!RunProgress.LoadSavedRun())
        {
            RefreshContinueState();
            return;
        }

        SceneTransition.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private AudioSource CreateAudioSource(string sourceName, bool loop, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);
        sourceObject.transform.localPosition = Vector3.zero;

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        return source;
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusic == null || musicSource == null)
        {
            return;
        }

        musicSource.clip = backgroundMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    /// <summary>
    /// Quét toàn scene chứ không chỉ con của object này.
    ///
    /// MainMenuManager nằm ở gốc scene và KHÔNG có con nào — nó không phải cha của đám nút trong
    /// Canvas. Dùng GetComponentsInChildren ở đây sẽ trả về mảng rỗng và không nút nào có tiếng.
    /// </summary>
    private void RegisterButtonSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            button.onClick.AddListener(PlayClickSound);

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            if (trigger.triggers == null)
            {
                trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            }

            EventTrigger.Entry hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener(_ => PlayHoverSound());
            trigger.triggers.Add(hoverEntry);
        }
    }

    private void RefreshContinueState()
    {
        if (continueButton != null)
        {
            continueButton.interactable = RunProgress.HasSavedRun;
        }
    }

    private void RefreshBestStage()
    {
        if (bestStageText == null) return;

        int best = RunProgress.BestStage;
        bestStageText.text = best > 1
            ? $"🏆 Best Stage: {best}"
            : "Best Stage: --";
    }

    private void AutoBindBestStageText()
    {
        if (bestStageText != null) return;

        Transform found = FindChildRecursive(transform.root, "BestStageText");
        if (found != null)
        {
            bestStageText = found.GetComponent<TMP_Text>();
        }
    }

    private void AutoBindContinueButton()
    {
        if (continueButton != null)
        {
            return;
        }

        // Quét theo tên trên toàn scene. transform.root ở đây chính là MainMenuManager (nó ở gốc và
        // không có con), nên tìm theo cây con sẽ không bao giờ thấy nút nằm trong Canvas.
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string objectName = button.name.ToLowerInvariant();
            if (objectName.Contains("continue"))
            {
                continueButton = button;
                return;
            }
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void PlayHoverSound()
    {
        PlaySound(buttonHoverSound);
    }

    private void PlayClickSound()
    {
        PlaySound(buttonClickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && soundSource != null)
        {
            soundSource.PlayOneShot(clip, soundVolume);
        }
    }

    // --- Dynamic Volume Settings ---
    // Không Save() ở đây — xem chú thích cùng chỗ trong GameAudioManager.
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        soundVolume = volume;
        if (soundSource != null)
        {
            soundSource.volume = soundVolume;
        }
        PlayerPrefs.SetFloat("SFXVolume", soundVolume);
    }

    /// <summary>Mở bảng âm lượng ở Main Menu. Nối hàm này vào OnClick của nút Settings.</summary>
    public void OpenSettings()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            Debug.LogError("MainMenuController: không tìm thấy Canvas nào trong scene để mở Settings.");
            return;
        }

        GameObject panel = SettingsPanelUI.CreateRuntimePanel(canvas.transform);
        panel.transform.SetAsLastSibling();
    }
}
