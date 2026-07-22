using UnityEngine;

/// <summary>
/// Centralizes audio used by GameScene. Attach this component to the GameManager object.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [Range(0f, 1f)] [SerializeField] private float defaultMusicVolume = 0.5f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip enemyDefeatedSound;
    [SerializeField] private AudioClip serverHitSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip gameOverSound;
    [Range(0f, 1f)] [SerializeField] private float defaultSoundVolume = 0.8f;

    private float musicVolume;
    private float soundVolume;

    private AudioSource musicSource;
    private AudioSource soundSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Load volume from PlayerPrefs or use default
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        soundVolume = PlayerPrefs.GetFloat("SFXVolume", defaultSoundVolume);

        musicSource = CreateAudioSource("Game Music", true, musicVolume);
        soundSource = CreateAudioSource("Game SFX", false, soundVolume);
    }

    private void Start()
    {
        if (backgroundMusic == null)
        {
            return;
        }

        musicSource.clip = backgroundMusic;
        musicSource.Play();
    }

    public void PlayShoot()         => PlaySound(shootSound);
    public void PlayEnemyDefeated() => PlaySound(enemyDefeatedSound);
    public void PlayServerHit()     => PlaySound(serverHitSound);
    public void PlayPlayerHit()     => PlaySound(playerHitSound);
    public void PlayLevelUp()       => PlaySound(levelUpSound);

    public void PlayWin()
    {
        StopMusic();
        PlaySound(winSound);
    }

    public void PlayGameOver()
    {
        StopMusic();
        PlaySound(gameOverSound);
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
        source.ignoreListenerPause = true;
        return source;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && soundSource != null)
        {
            soundSource.PlayOneShot(clip, soundVolume);
        }
    }

    private void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // --- Dynamic Volume Settings ---
    // Cố ý KHÔNG gọi PlayerPrefs.Save() ở đây: hàm này được nối vào Slider.onValueChanged nên bắn
    // mỗi frame trong lúc người chơi kéo, mà Save() thì ghi thẳng xuống đĩa. SetFloat chỉ ghi vào
    // bộ nhớ; SettingsPanelUI flush một lần lúc đóng bảng, và Unity cũng tự flush khi thoát game.
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
}
