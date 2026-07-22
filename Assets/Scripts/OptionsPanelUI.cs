using System;
using UnityEngine;

/// <summary>
/// Panel chỉnh âm lượng (tổng / nhạc / SFX), dựng bằng code nên dùng được ở cả
/// MainMenuScene lẫn pop-up Pause trong GameScene. Ghi thẳng vào <see cref="VolumeSettings"/>.
/// </summary>
public class OptionsPanelUI : MonoBehaviour
{
    private const int SortingOrder = 30500; // nằm trên pop-up Pause

    private static OptionsPanelUI instance;

    private Action onClosed;

    public static bool IsOpen => instance != null;

    public static void Show(Action onClosed = null)
    {
        if (instance != null)
        {
            instance.onClosed = onClosed;
            return;
        }

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("OptionsCanvas", SortingOrder);
        instance = canvas.gameObject.AddComponent<OptionsPanelUI>();
        instance.onClosed = onClosed;
        instance.Build(canvas.transform);
    }

    public static void Close()
    {
        if (instance != null)
        {
            instance.CloseInternal();
        }
    }

    private void Build(Transform root)
    {
        RuntimeUI.CreateBackdrop(root, new Color(0f, 0f, 0f, 0.75f));

        // Chiều cao do ContentSizeFitter tự tính, số 0 chỉ là giá trị khởi tạo.
        RectTransform panel = RuntimeUI.CreateMenuPanel(root, new Vector2(820f, 0f), 18f);

        RuntimeUI.CreateText(panel, "OPTIONS", 40f, RuntimeUI.AccentColor, 58f);
        RuntimeUI.CreateText(panel, "Audio", 22f, new Color(0.7f, 0.76f, 0.82f, 1f), 32f);

        RuntimeUI.CreateSliderRow(panel, "Master Volume", VolumeSettings.Master, value => VolumeSettings.Master = value);
        RuntimeUI.CreateSliderRow(panel, "Music",         VolumeSettings.Music,  value => VolumeSettings.Music = value);
        RuntimeUI.CreateSliderRow(panel, "Sound Effects", VolumeSettings.Sfx,    value => VolumeSettings.Sfx = value);

        RuntimeUI.CreateText(panel, string.Empty, 16f, Color.clear, 12f); // khoảng đệm
        RuntimeUI.CreateButton(panel, "BACK", CloseInternal);
    }

    private void CloseInternal()
    {
        Action callback = onClosed;
        onClosed = null;
        instance = null;

        Destroy(gameObject);
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
