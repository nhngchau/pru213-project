using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nối nút 3 gạch có sẵn trong GameScene vào PauseController, thay vì để script
/// tự tạo một nút mới lúc runtime (nút tự tạo nằm lệch so với UI đã thiết kế).
///
/// Cách dùng: mở GameScene → chọn nút 3 gạch trong Hierarchy →
/// GDD Tools → Wire Selected Button As Pause Button.
/// </summary>
public static class PauseButtonWiringTool
{
    [MenuItem("GDD Tools/Wire Selected Button As Pause Button")]
    private static void WireSelectedButton()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || !selected.scene.IsValid())
        {
            EditorUtility.DisplayDialog("Pause Button",
                "Hãy chọn nút 3 gạch trong Hierarchy (object thuộc scene) rồi chạy lại.", "OK");
            return;
        }

        Button button = selected.GetComponent<Button>();
        if (button == null)
        {
            // Nút chỉ là Image trang trí -> gắn Button cho nó bấm được.
            Image image = selected.GetComponent<Image>();
            if (image == null)
            {
                EditorUtility.DisplayDialog("Pause Button",
                    $"'{selected.name}' không có Button lẫn Image nên không bấm được.\n" +
                    "Hãy chọn đúng object chứa hình nút 3 gạch.", "OK");
                return;
            }

            button = Undo.AddComponent<Button>(selected);
            button.targetGraphic = image;
            Debug.Log($"[PauseButtonWiringTool] '{selected.name}' chưa có Button, đã tự thêm.");
        }

        // Không bật Raycast Target thì bấm không ăn - đây là lý do phổ biến nhất
        // khiến nút "nhìn thấy mà không click được".
        Graphic graphic = selected.GetComponent<Graphic>();
        if (graphic != null && !graphic.raycastTarget)
        {
            Undo.RecordObject(graphic, "Wire Pause Button");
            graphic.raycastTarget = true;
            EditorUtility.SetDirty(graphic);
            Debug.Log($"[PauseButtonWiringTool] Đã bật Raycast Target cho '{selected.name}'.");
        }

        if (!button.interactable)
        {
            Undo.RecordObject(button, "Wire Pause Button");
            button.interactable = true;
            EditorUtility.SetDirty(button);
        }

        if (button.onClick.GetPersistentEventCount() > 0)
        {
            Debug.LogWarning($"[PauseButtonWiringTool] '{selected.name}' đang có sẵn " +
                             $"{button.onClick.GetPersistentEventCount()} listener trong On Click. " +
                             "PauseController sẽ thêm listener riêng lúc chạy, nên nút sẽ làm CẢ HAI việc. " +
                             "Xoá bớt trong Inspector nếu không muốn.");
        }

        PauseController controller = Object.FindFirstObjectByType<PauseController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("PauseController");
            Undo.RegisterCreatedObjectUndo(controllerObject, "Wire Pause Button");
            controller = Undo.AddComponent<PauseController>(controllerObject);
        }

        // pauseButton / createButtonIfMissing là private nên phải gán qua SerializedObject.
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty buttonProperty = serialized.FindProperty("pauseButton");
        SerializedProperty createProperty = serialized.FindProperty("createButtonIfMissing");

        if (buttonProperty == null || createProperty == null)
        {
            EditorUtility.DisplayDialog("Pause Button",
                "Không tìm thấy field 'pauseButton' / 'createButtonIfMissing' trong PauseController.", "OK");
            return;
        }

        buttonProperty.objectReferenceValue = button;
        createProperty.boolValue = false; // đã có nút thật -> không tạo nút runtime nữa
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = controller.gameObject;

        Debug.Log($"[PauseButtonWiringTool] Đã nối '{selected.name}' vào PauseController " +
                  "và tắt tạo nút runtime. Nhớ Ctrl+S để lưu scene.");
    }
}
