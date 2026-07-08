using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Guide Panel")]
    [SerializeField] private GameObject guidePanel;

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
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float soundVolume = 0.8f;

    private Vector3 normalScale;
    private Vector3 targetScale;
    private AudioSource musicSource;
    private AudioSource soundSource;

    private void Awake()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;

        musicSource = CreateAudioSource("Menu Music", true, musicVolume);
        soundSource = CreateAudioSource("Menu SFX", false, soundVolume);
    }

    private void Start()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }

        PlayBackgroundMusic();
        RegisterButtonSounds();
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

    public void StartGame()
    {
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

    private void RegisterButtonSounds()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
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
}
