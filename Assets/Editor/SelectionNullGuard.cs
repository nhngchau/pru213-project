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

    private static void RebuildInspector()
    {
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        InternalEditorUtility.RepaintAllViews();
    }
}
