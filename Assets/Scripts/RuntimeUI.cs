using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Dựng UI bằng code lúc runtime. Dùng chung cho các màn hình được tạo động
/// (Pause, Options, Victory) nên không cần chỉnh prefab hay scene bằng tay.
/// </summary>
public static class RuntimeUI
{
    public static readonly Color PanelColor  = new Color(0.06f, 0.09f, 0.13f, 0.97f);
    public static readonly Color AccentColor = new Color(0.35f, 0.90f, 1.00f, 1f);
    public static readonly Color ButtonColor = new Color(0.13f, 0.45f, 0.62f, 1f);
    public static readonly Color DangerColor = new Color(0.70f, 0.18f, 0.20f, 1f);

    public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return canvas;
    }

    /// <summary>Không có EventSystem thì mọi button đều không bấm được.</summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    /// <summary>Nền mờ phủ toàn màn hình, đồng thời chặn click xuyên xuống gameplay.</summary>
    public static Image CreateBackdrop(Transform parent, Color color)
    {
        RectTransform rect = CreateRect("Backdrop", parent);
        Stretch(rect);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    /// <summary>Khung menu giữa màn hình, các phần tử con xếp dọc.</summary>
    public static RectTransform CreateMenuPanel(Transform parent, Vector2 size, float spacing = 16f)
    {
        RectTransform rect = CreateRect("Panel", parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image background = rect.gameObject.AddComponent<Image>();
        background.color = PanelColor;

        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 36, 36);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        // Phải bật childControlHeight, nếu không layout bỏ qua LayoutElement.preferredHeight
        // và mọi phần tử con giữ nguyên 100x100 mặc định -> tràn ra ngoài khung.
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Chiều cao khung tự co giãn theo nội dung nên không bao giờ bị tràn.
        ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    public static TMP_Text CreateText(Transform parent, string content, float fontSize, Color color, float height = 46f)
    {
        RectTransform rect = CreateRect("Text", parent);

        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        SetLayoutHeight(rect, height);
        return text;
    }

    public static Button CreateButton(Transform parent, string label, UnityAction onClick, Color? color = null, float height = 60f)
    {
        RectTransform rect = CreateRect(label.Replace(" ", string.Empty) + "Button", parent);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color ?? ButtonColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        RectTransform labelRect = CreateRect("Label", rect);
        Stretch(labelRect);

        TMP_Text text = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 26f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        SetLayoutHeight(rect, height);
        return button;
    }

    /// <summary>Một dòng "Nhãn — thanh trượt — %" dùng cho panel Option.</summary>
    public static Slider CreateSliderRow(Transform parent, string label, float value, UnityAction<float> onChanged)
    {
        RectTransform row = CreateRect(label.Replace(" ", string.Empty) + "Row", parent);
        SetLayoutHeight(row, 54f);

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform labelRect = CreateRect("Label", row);
        TMP_Text labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 22f;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.raycastTarget = false;
        SetLayoutWidth(labelRect, 220f);
        SetLayoutHeight(labelRect, 40f);

        RectTransform sliderRect = CreateRect("Slider", row);
        LayoutElement sliderLayout = sliderRect.gameObject.AddComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.minWidth = 200f;
        sliderLayout.preferredHeight = 24f;
        sliderLayout.minHeight = 24f;

        Slider slider = sliderRect.gameObject.AddComponent<Slider>();

        // Dựng đúng cấu trúc slider chuẩn của Unity (DefaultControls.CreateSlider):
        // track cao 50% ở giữa, Fill Area và Handle Slide Area thụt vào đúng bán kính handle.
        RectTransform background = CreateRect("Background", sliderRect);
        background.anchorMin = new Vector2(0f, 0.25f);
        background.anchorMax = new Vector2(1f, 0.75f);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        background.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.24f, 0.30f, 1f);

        RectTransform fillArea = CreateRect("Fill Area", sliderRect);
        fillArea.anchorMin = new Vector2(0f, 0.25f);
        fillArea.anchorMax = new Vector2(1f, 0.75f);
        fillArea.anchoredPosition = new Vector2(-5f, 0f);
        fillArea.sizeDelta = new Vector2(-20f, 0f);

        RectTransform fill = CreateRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.sizeDelta = new Vector2(10f, 0f);
        fill.gameObject.AddComponent<Image>().color = AccentColor;

        RectTransform handleArea = CreateRect("Handle Slide Area", sliderRect);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = Vector2.zero;
        handleArea.offsetMax = Vector2.zero;
        handleArea.sizeDelta = new Vector2(-20f, 0f);

        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(20f, 0f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));

        RectTransform valueRect = CreateRect("Value", row);
        TMP_Text valueText = valueRect.gameObject.AddComponent<TextMeshProUGUI>();
        valueText.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        valueText.fontSize = 20f;
        valueText.color = AccentColor;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.raycastTarget = false;
        SetLayoutWidth(valueRect, 72f);
        SetLayoutHeight(valueRect, 40f);

        slider.onValueChanged.AddListener(newValue =>
        {
            valueText.text = Mathf.RoundToInt(newValue * 100f) + "%";
            onChanged?.Invoke(newValue);
        });

        return slider;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static LayoutElement GetLayoutElement(RectTransform rect)
    {
        LayoutElement element = rect.gameObject.GetComponent<LayoutElement>();
        return element != null ? element : rect.gameObject.AddComponent<LayoutElement>();
    }

    private static void SetLayoutHeight(RectTransform rect, float height)
    {
        LayoutElement element = GetLayoutElement(rect);
        element.minHeight = height;
        element.preferredHeight = height;
    }

    private static void SetLayoutWidth(RectTransform rect, float width)
    {
        LayoutElement element = GetLayoutElement(rect);
        element.minWidth = width;
        element.preferredWidth = width;
    }
}
