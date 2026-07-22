using System;
using UnityEngine;

/// <summary>
/// Kho âm lượng dùng chung cho cả MainMenuScene lẫn GameScene, lưu bằng PlayerPrefs.
/// Panel Option ghi vào đây; các AudioSource lắng nghe <see cref="OnChanged"/> để đổi ngay lập tức.
/// </summary>
public static class VolumeSettings
{
    private const string KeyMaster = "SD_VolumeMaster";
    private const string KeyMusic  = "SD_VolumeMusic";
    private const string KeySfx    = "SD_VolumeSfx";

    private static float master = -1f;
    private static float music  = -1f;
    private static float sfx    = -1f;

    /// <summary>Bắn ra mỗi khi một mức âm lượng thay đổi.</summary>
    public static event Action OnChanged;

    public static float Master
    {
        get { EnsureLoaded(); return master; }
        set { EnsureLoaded(); Apply(ref master, KeyMaster, value); }
    }

    public static float Music
    {
        get { EnsureLoaded(); return music; }
        set { EnsureLoaded(); Apply(ref music, KeyMusic, value); }
    }

    public static float Sfx
    {
        get { EnsureLoaded(); return sfx; }
        set { EnsureLoaded(); Apply(ref sfx, KeySfx, value); }
    }

    /// <summary>Âm lượng thực tế cần gán cho AudioSource nhạc nền.</summary>
    public static float MusicOutput => Master * Music;

    /// <summary>Âm lượng thực tế cần gán cho AudioSource hiệu ứng.</summary>
    public static float SfxOutput => Master * Sfx;

    private static void EnsureLoaded()
    {
        if (master >= 0f)
        {
            return;
        }

        master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        music  = PlayerPrefs.GetFloat(KeyMusic, 0.5f);
        sfx    = PlayerPrefs.GetFloat(KeySfx, 0.8f);
    }

    private static void Apply(ref float field, string key, float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(field, clamped))
        {
            return;
        }

        field = clamped;
        PlayerPrefs.SetFloat(key, clamped);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
