#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Optional, SAFE one-time organization helpers (workspace cleanup only - no gameplay changes).
///
/// - "Organize Project Folders" uses AssetDatabase.MoveAsset, which moves the asset together with
///   its .meta file, so GUIDs/FileIDs are preserved and no references break.
/// - "Organize Scene Hierarchy" only creates empty parent groups and re-parents existing roots
///   (world position kept). Everything is wrapped in Undo, so Ctrl+Z reverts it.
///
/// Nothing is deleted automatically. Empty/junk folders and duplicate input-action files are only
/// reported, so you can verify and remove them by hand.
/// </summary>
public static class ProjectOrganizer
{
    // ---------------------------------------------------------------- FOLDERS

    [MenuItem("GDD Tools/Organize Project Folders")]
    public static void OrganizeFolders()
    {
        EnsureFolder("Assets", "ThirdParty");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Settings", "Input");

        // 1) Isolate third-party asset packs away from our own work.
        SafeMove("Assets/kenney_furniture-kit", "Assets/ThirdParty/kenney_furniture-kit");
        SafeMove("Assets/lpc_male_animations_2026-05-13T08-00-26", "Assets/ThirdParty/lpc_male_animations_2026-05-13T08-00-26");

        // 2) Consolidate the two prefab folders into the standard "Prefabs".
        SafeMove("Assets/_Prefabs/SyntaxError.prefab", "Assets/Prefabs/SyntaxError.prefab");
        SafeMove("Assets/_Prefabs/CodeBullet.prefab", "Assets/Prefabs/CodeBullet.prefab");

        // 3) Tidy the real input asset into Settings/Input.
        SafeMove("Assets/InputSystem_Actions.inputactions", "Assets/Settings/Input/InputSystem_Actions.inputactions");

        AssetDatabase.Refresh();

        // 4) Report (do NOT auto-delete) things that need a human decision.
        ReportIfEmpty("Assets/_Prefabs");
        ReportIfEmpty("Assets/Animations");
        ReportIfEmpty("Assets/Audio");
        ReportExists("Assets/New Actions.inputactions", "junk default name - delete if unused");
        ReportExists("Assets/New Actions 1.inputactions", "junk default name - delete if unused");

        Debug.Log("[Organizer] Folder pass done. Review the warnings above, then delete empty/junk items manually.");
    }

    // -------------------------------------------------------------- HIERARCHY

    [MenuItem("GDD Tools/Organize Scene Hierarchy")]
    public static void OrganizeHierarchy()
    {
        // Rename the mystery root that actually holds the GameManager.
        RenameIfExists("GameObject", "GameManager");
        RenameIfExists("run_18", "PlayerVisual");

        Transform managers = CreateGroup("--- MANAGERS ---");
        Transform entities = CreateGroup("--- ENTITIES ---");
        Transform environment = CreateGroup("--- ENVIRONMENT ---");
        Transform ui = CreateGroup("--- UI ---");

        Reparent("GameManager", managers);
        Reparent("EnemySpawner", managers);

        Reparent("Player", entities);
        Reparent("ServerCore", entities);

        Reparent("Grid", environment);
        Reparent("Background", environment);
        Reparent("MapColliders", environment);

        Reparent("Canvas", ui);
        Reparent("EventSystem", ui);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Organizer] Hierarchy grouped. 'Main Camera' left at root by convention. " +
                  "Consider moving the 'Server' visual (currently inside MapColliders) next to ServerCore manually.");
    }

    // ----------------------------------------------------------------- HELPERS

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
            Debug.Log($"[Organizer] Created folder {path}");
        }
    }

    private static void SafeMove(string from, string to)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(from) == null && !AssetDatabase.IsValidFolder(from))
        {
            return; // already moved or never existed
        }

        string error = AssetDatabase.MoveAsset(from, to);
        if (string.IsNullOrEmpty(error))
        {
            Debug.Log($"[Organizer] Moved {from} -> {to}");
        }
        else
        {
            Debug.LogWarning($"[Organizer] Could not move {from}: {error}");
        }
    }

    private static void ReportIfEmpty(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            string[] contents = AssetDatabase.FindAssets("", new[] { folder });
            if (contents.Length == 0)
            {
                Debug.LogWarning($"[Organizer] '{folder}' is empty - delete it manually if not needed.");
            }
        }
    }

    private static void ReportExists(string path, string note)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            Debug.LogWarning($"[Organizer] '{path}' ({note}).");
        }
    }

    private static Transform CreateGroup(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing.transform;
        }

        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, "Create Hierarchy Group");
        group.transform.position = Vector3.zero;
        return group.transform;
    }

    private static void Reparent(string childName, Transform parent)
    {
        GameObject go = GameObject.Find(childName);
        if (go == null)
        {
            Debug.LogWarning($"[Organizer] Hierarchy: '{childName}' not found; skipped.");
            return;
        }

        Undo.SetTransformParent(go.transform, parent, "Group " + childName);
        Debug.Log($"[Organizer] '{childName}' -> {parent.name}");
    }

    private static void RenameIfExists(string oldName, string newName)
    {
        GameObject go = GameObject.Find(oldName);
        if (go != null)
        {
            Undo.RecordObject(go, "Rename " + oldName);
            go.name = newName;
            Debug.Log($"[Organizer] Renamed '{oldName}' -> '{newName}'");
        }
    }
}
#endif
