using UnityEngine;

/// <summary>
/// Centralizes audio used by GameScene. Attach this component to the GameManager object.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip enemyDefeatedSound;
    [SerializeField] private AudioClip serverHitSound;
    [SerializeField] private AudioClip playerHitSound;   // âm thanh khi player bị đánh
    [SerializeField] private AudioClip levelUpSound;     // âm thanh khi lên cấp
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip gameOverSound;

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
        musicSource = CreateAudioSource("Game Music", true, VolumeSettings.MusicOutput);
        soundSource = CreateAudioSource("Game SFX", false, VolumeSettings.SfxOutput);
    }

    private void OnEnable()
    {
        VolumeSettings.OnChanged += ApplyVolumes;
    }

    private void OnDisable()
    {
        VolumeSettings.OnChanged -= ApplyVolumes;
    }

    /// <summary>Áp mức âm lượng mới ngay khi người chơi kéo slider trong panel Option.</summary>
    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = VolumeSettings.MusicOutput;
        }

        if (soundSource != null)
        {
            soundSource.volume = VolumeSettings.SfxOutput;
        }
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
            soundSource.PlayOneShot(clip, VolumeSettings.SfxOutput);
        }
    }

    private void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
}
