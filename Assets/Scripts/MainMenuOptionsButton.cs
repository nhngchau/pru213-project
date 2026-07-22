using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Chèn button OPTION vào Main Menu lúc runtime, nằm giữa "How to play" và "Quit".
/// Cách làm: nhân bản chính button Quit để giữ nguyên style, thay component Button
/// (vì onClick gán sẵn trong Inspector không xoá được bằng RemoveAllListeners),
/// rồi đặt sibling index ngay trước Quit — ButtonGroup có VerticalLayoutGroup nên tự xếp chỗ.
///
/// Nếu bạn tự dựng button OPTION trong scene và gắn <c>MainMenuController.OpenOptions()</c>,
/// hãy đặt tên object chứa chữ "option" — script này sẽ tự nhường.
/// </summary>
public class MainMenuOptionsButton : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // RuntimeInitializeOnLoadMethod chỉ chạy một lần lúc mở app; phải bám sceneLoaded
        // thì quay lại Main Menu từ trong game mới có nút.
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
        if (SceneManager.GetActiveScene().name != "MainMenuScene")
        {
            return;
        }

        if (FindFirstObjectByType<MainMenuOptionsButton>() != null || FindButton("option") != null)
        {
            return;
        }

        new GameObject("MainMenuOptionsButton").AddComponent<MainMenuOptionsButton>();
    }

    private void Start()
    {
        Button quitButton = FindButton("quit", "exit");
        if (quitButton != null && TryCloneAsOptionButton(quitButton))
        {
            return;
        }

        Debug.LogWarning("MainMenuOptionsButton: không tìm thấy nút Quit để chèn cạnh, " +
                         "tạo nút OPTION rời ở góc dưới trái.");
        CreateStandaloneButton();
    }

    private bool TryCloneAsOptionButton(Button template)
    {
        Transform parent = template.transform.parent;
        if (parent == null)
        {
            return false;
        }

        GameObject clone = Instantiate(template.gameObject, parent);
        clone.name = "OptionButton";

        // Chèn ngay TRƯỚC Quit => nằm giữa "How to play" và "Quit".
        clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex());

        Button oldButton = clone.GetComponent<Button>();
        if (oldButton == null)
        {
            Destroy(clone);
            return false;
        }

        // Giữ lại phần nhìn của button gốc trước khi thay component.
        Selectable.Transition transition = oldButton.transition;
        ColorBlock colors = oldButton.colors;
        SpriteState spriteState = oldButton.spriteState;
        Graphic targetGraphic = oldButton.targetGraphic;

        DestroyImmediate(oldButton);

        Button optionButton = clone.AddComponent<Button>();
        optionButton.transition = transition;
        optionButton.colors = colors;
        optionButton.spriteState = spriteState;
        optionButton.targetGraphic = targetGraphic;
        optionButton.onClick.AddListener(() => OptionsPanelUI.Show());

        SetLabel(clone.transform, "OPTION");
        return true;
    }

    private static void SetLabel(Transform root, string label)
    {
        TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private static Button FindButton(params string[] keywords)
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string labelText = label != null ? label.text.ToLowerInvariant() : string.Empty;
            string objectName = button.name.ToLowerInvariant();

            foreach (string keyword in keywords)
            {
                if (labelText.Contains(keyword) || objectName.Contains(keyword))
                {
                    return button;
                }
            }
        }

        return null;
    }

    /// <summary>Phương án dự phòng khi không tìm thấy nút Quit trong scene.</summary>
    private void CreateStandaloneButton()
    {
        Canvas canvas = RuntimeUI.CreateOverlayCanvas("MainMenuOptionsCanvas", 400);
        canvas.transform.SetParent(transform, false);

        RectTransform rect = RuntimeUI.CreateRect("OptionButton", canvas.transform);
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(40f, 40f);
        rect.sizeDelta = new Vector2(230f, 62f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = RuntimeUI.ButtonColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => OptionsPanelUI.Show());

        RectTransform labelRect = RuntimeUI.CreateRect("Label", rect);
        RuntimeUI.Stretch(labelRect);

        TMP_Text label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = "OPTION";
        label.fontSize = 26f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }
}
