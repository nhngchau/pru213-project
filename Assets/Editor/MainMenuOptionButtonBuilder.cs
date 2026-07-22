using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Tạo button OPTION thật trong MainMenuScene (không phải dựng lúc runtime).
/// Nhân bản nút "How to play" để giữ nguyên style, chèn ngay sau nó — tức nằm
/// giữa "How to play" và "Quit" — rồi nối onClick vào MainMenuController.OpenOptions().
/// </summary>
public static class MainMenuOptionButtonBuilder
{
    private const string OptionObjectName = "OptionButton";

    [MenuItem("GDD Tools/Add Option Button To Main Menu")]
    private static void AddOptionButton()
    {
        if (FindButton(OptionObjectName.ToLowerInvariant(), "option") != null)
        {
            EditorUtility.DisplayDialog("Option Button",
                "Scene đã có button OPTION rồi. Không tạo thêm.", "OK");
            return;
        }

        // Ưu tiên nhân bản "How to play" (nút vàng, hành động thường).
        // Không thấy thì lùi về nút Quit.
        bool insertAfterTemplate = true;
        Button template = FindButton("how to play", "howtoplay", "how", "guide");
        if (template == null)
        {
            template = FindButton("quit", "exit");
            insertAfterTemplate = false; // chèn TRƯỚC Quit
        }

        if (template == null || template.transform.parent == null)
        {
            EditorUtility.DisplayDialog("Option Button",
                "Không tìm thấy nút 'How to play' hay 'Quit' trong scene đang mở.\n" +
                "Hãy mở MainMenuScene rồi chạy lại.", "OK");
            return;
        }

        GameObject clone = Object.Instantiate(template.gameObject, template.transform.parent);
        clone.name = OptionObjectName;
        Undo.RegisterCreatedObjectUndo(clone, "Add Option Button");

        int templateIndex = template.transform.GetSiblingIndex();
        clone.transform.SetSiblingIndex(insertAfterTemplate ? templateIndex + 1 : templateIndex);

        SetLabel(clone, "OPTION");
        WireOnClick(clone);

        Selection.activeGameObject = clone;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[MainMenuOptionButtonBuilder] Đã tạo '{OptionObjectName}' " +
                  $"(nhân bản từ '{template.name}'). Nhớ Ctrl+S để lưu scene.");
    }

    /// <summary>
    /// Đồng bộ font/cỡ chữ/kích thước của toàn bộ button trong ButtonGroup theo nút đầu tiên
    /// (Start), để nút OPTION mới thêm không bị lệch style so với 3 nút cũ.
    /// </summary>
    [MenuItem("GDD Tools/Balance Main Menu Buttons")]
    private static void BalanceMainMenuButtons()
    {
        Button reference = FindButton("start", "play game", "new game");
        if (reference == null || reference.transform.parent == null)
        {
            EditorUtility.DisplayDialog("Balance Buttons",
                "Không tìm thấy nút 'Start' làm mẫu. Hãy mở MainMenuScene rồi chạy lại.", "OK");
            return;
        }

        TMP_Text referenceLabel = reference.GetComponentInChildren<TMP_Text>(true);
        if (referenceLabel == null)
        {
            EditorUtility.DisplayDialog("Balance Buttons", "Nút 'Start' không có TMP_Text để lấy mẫu.", "OK");
            return;
        }

        RectTransform group = (RectTransform)reference.transform.parent;
        RectTransform referenceRect = (RectTransform)reference.transform;

        // Chốt kích thước ĐANG hiển thị của mọi con TRƯỚC khi đụng vào layout group.
        // Bật ChildControl sẽ khiến layout tự quyết kích thước con, nên nếu không ghi lại
        // trước thì TitleText / Text (TMP) sẽ bị co lại theo preferred size của TMP.
        Dictionary<RectTransform, Vector2> sizeBefore = new Dictionary<RectTransform, Vector2>();
        foreach (Transform child in group)
        {
            if (child is RectTransform childRect)
            {
                sizeBefore[childRect] = childRect.rect.size;
            }
        }

        Vector2 buttonSize = sizeBefore.TryGetValue(referenceRect, out Vector2 refSize)
            ? refSize
            : referenceRect.rect.size;

        // Đây là gốc của cả hai triệu chứng: ForceExpand bật trong khi ChildControl tắt
        // => layout muốn giãn con nhưng không có quyền đổi kích thước con, nên chỉ rải
        // khoảng trắng thừa quanh chúng.
        HorizontalOrVerticalLayoutGroup layout = group.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layout != null)
        {
            Undo.RecordObject(layout, "Balance Main Menu Buttons");
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            EditorUtility.SetDirty(layout);
        }

        int buttonCount = 0;

        foreach (Transform child in group)
        {
            if (!(child is RectTransform childRect))
            {
                continue;
            }

            Button button = child.GetComponent<Button>();

            // Button dùng chung kích thước của Start; tiêu đề/phụ đề giữ nguyên kích thước cũ.
            Vector2 size = button != null
                ? buttonSize
                : (sizeBefore.TryGetValue(childRect, out Vector2 old) ? old : childRect.rect.size);

            ApplyLayoutElement(childRect.gameObject, size);

            if (button == null)
            {
                continue;
            }

            buttonCount++;

            if (button == reference)
            {
                continue;
            }

            Undo.RecordObject(childRect, "Balance Main Menu Buttons");
            childRect.localScale = referenceRect.localScale;
            EditorUtility.SetDirty(childRect);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                Undo.RecordObject(label, "Balance Main Menu Buttons");
                label.font = referenceLabel.font;
                label.fontSharedMaterial = referenceLabel.fontSharedMaterial;
                label.fontSize = referenceLabel.fontSize;
                label.fontStyle = referenceLabel.fontStyle;
                label.fontWeight = referenceLabel.fontWeight;
                label.color = referenceLabel.color;
                label.alignment = referenceLabel.alignment;
                label.characterSpacing = referenceLabel.characterSpacing;
                label.enableAutoSizing = referenceLabel.enableAutoSizing;
                label.fontSizeMin = referenceLabel.fontSizeMin;
                label.fontSizeMax = referenceLabel.fontSizeMax;
                EditorUtility.SetDirty(label);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[MainMenuOptionButtonBuilder] Đã cân {buttonCount} button theo mẫu '{reference.name}' " +
                  $"({buttonSize.x:0} x {buttonSize.y:0}). Sửa kích thước sau này bằng LayoutElement trên từng nút. " +
                  "Nhớ Ctrl+S để lưu scene.");
    }

    /// <summary>Chốt kích thước của một con bằng LayoutElement để layout không tự đổi lung tung.</summary>
    private static void ApplyLayoutElement(GameObject target, Vector2 size)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = Undo.AddComponent<LayoutElement>(target);
        }
        else
        {
            Undo.RecordObject(element, "Balance Main Menu Buttons");
        }

        element.preferredWidth = size.x;
        element.preferredHeight = size.y;
        element.minWidth = -1f;
        element.minHeight = -1f;
        element.flexibleWidth = -1f;
        element.flexibleHeight = -1f;
        EditorUtility.SetDirty(element);
    }

    private static void WireOnClick(GameObject clone)
    {
        Button button = clone.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        // Xoá listener gán sẵn trong Inspector của nút gốc (ví dụ OpenGuide / QuitGame),
        // nếu không nút OPTION sẽ chạy đúng hành vi của nút bị nhân bản.
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        MainMenuController controller = clone.GetComponent<MainMenuController>();
        if (controller == null)
        {
            controller = Object.FindFirstObjectByType<MainMenuController>();
        }

        if (controller == null)
        {
            Debug.LogWarning("[MainMenuOptionButtonBuilder] Không tìm thấy MainMenuController để nối onClick. " +
                             "Hãy tự kéo nó vào ô On Click của button OPTION.");
            return;
        }

        UnityEventTools.AddVoidPersistentListener(button.onClick, new UnityAction(controller.OpenOptions));
    }

    private static void SetLabel(GameObject root, string label)
    {
        TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            Undo.RecordObject(text, "Set Option Label");
            text.text = label;
            EditorUtility.SetDirty(text);
        }
    }

    private static Button FindButton(params string[] keywords)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
}
