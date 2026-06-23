using UnityEngine;

/// <summary>
/// Centralizes audio used by GameScene. Attach this component to the GameManager object.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip enemyDefeatedSound;
    [SerializeField] private AudioClip serverHitSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip gameOverSound;
    [Range(0f, 1f)] [SerializeField] private float soundVolume = 0.8f;

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

    public void PlayShoot() => PlaySound(shootSound);
    public void PlayEnemyDefeated() => PlaySound(enemyDefeatedSound);
    public void PlayServerHit() => PlaySound(serverHitSound);

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
}
