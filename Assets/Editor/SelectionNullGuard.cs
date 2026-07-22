using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;

/// <summary>
/// Prevents Unity's Inspector from trying to rebuild editors for destroyed runtime UI objects
/// during domain reload / play-mode transitions.
/// </summary>
[InitializeOnLoad]
public static class SelectionNullGuard
{
    static SelectionNullGuard()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += ClearInspectorSelection;
        EditorApplication.delayCall += CleanNullSelection;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode
            || state == PlayModeStateChange.ExitingPlayMode)
        {
            ClearInspectorSelection();
            EditorApplication.delayCall += ClearInspectorSelection;
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode
            || state == PlayModeStateChange.EnteredEditMode)
        {
            CleanNullSelection();
            EditorApplication.delayCall += CleanNullSelection;
        }
    }

    private static void ClearInspectorSelection()
    {
        Selection.objects = System.Array.Empty<Object>();
        Selection.activeObject = null;
        RebuildInspector();
    }

    private static void CleanNullSelection()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            return;
        }

        Object[] validObjects = selectedObjects.Where(item => item != null).ToArray();
        if (validObjects.Length == selectedObjects.Length)
        {
            return;
        }

        Selection.objects = validObjects;
        if (validObjects.Length == 0)
        {
            Selection.activeObject = null;
        }

        RebuildInspector();
    }

    /// <summary>
    /// ForceRebuild() chỉ an toàn SAU khi thay đổi Selection đã có hiệu lực.
    ///
    /// Gán Selection.objects không tác động tức thì: ActiveEditorTracker vẫn giữ các object cũ cho
    /// tới tick kế tiếp. Gọi ForceRebuild() đồng bộ ngay trong beforeAssemblyReload hoặc lúc thoát
    /// Play mode nghĩa là bắt nó dựng Editor cho những object vừa bị huỷ — và đó chính là chỗ ném
    /// SerializedObjectNotCreatableException ("Object at index 0 is null"), tức hàm này tự gây ra
    /// đúng cái lỗi mà cả class đang cố dập.
    ///
    /// Hoãn một tick là đủ. Nếu domain reload xảy ra trước khi delayCall kịp chạy thì callback bị
    /// bỏ qua — không sao, sau reload Inspector tự dựng lại và CleanNullSelection chạy lại từ đầu.
    /// </summary>
    private static void RebuildInspector()
    {
        EditorApplication.delayCall += () =>
        {
            ActiveEditorTracker.sharedTracker.ForceRebuild();
            InternalEditorUtility.RepaintAllViews();
        };
    }
}
