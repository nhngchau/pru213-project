using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

public static class UnityScreenNavigatorSetupTool
{
    private const string ContainerName = "GameModalContainer";

    [MenuItem("Tools/The Senior Defender/UI/Setup UnityScreenNavigator")]
    public static void Setup()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[UnityScreenNavigatorSetup] No Canvas found in the active scene.");
            return;
        }

        GameObject containerObject = GameObject.Find(ContainerName);
        if (containerObject == null)
        {
            containerObject = new GameObject(ContainerName, typeof(RectTransform), typeof(RectMask2D), typeof(ModalContainer));
            Undo.RegisterCreatedObjectUndo(containerObject, "Create GameModalContainer");
            containerObject.transform.SetParent(canvas.transform, false);
        }

        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        ModalContainer modalContainer = containerObject.GetComponent<ModalContainer>();
        SerializedObject containerSerialized = new SerializedObject(modalContainer);
        containerSerialized.FindProperty("_name").stringValue = ContainerName;
        containerSerialized.ApplyModifiedProperties();

        GameUIManager uiManager = canvas.GetComponent<GameUIManager>();
        if (uiManager == null)
        {
            uiManager = Undo.AddComponent<GameUIManager>(canvas.gameObject);
        }

        SerializedObject managerSerialized = new SerializedObject(uiManager);
        managerSerialized.FindProperty("modalContainer").objectReferenceValue = modalContainer;
        managerSerialized.FindProperty("modalContainerName").stringValue = ContainerName;
        managerSerialized.FindProperty("upgradePanel").objectReferenceValue = GameObject.Find("UpgradePanel");
        managerSerialized.FindProperty("gameOverPanel").objectReferenceValue = GameObject.Find("GameOverPanel");
        managerSerialized.FindProperty("winPanel").objectReferenceValue = GameObject.Find("WinPanel");
        managerSerialized.ApplyModifiedProperties();

        EnsureResourcesFolders();

        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(containerObject);
        Debug.Log("[UnityScreenNavigatorSetup] Added GameModalContainer and wired GameUIManager. Next: convert UpgradePanel, GameOverPanel, and WinPanel into Resources/UI/Modals prefabs.");
    }

    private static void EnsureResourcesFolders()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/UI");
        EnsureFolder("Assets/Resources/UI/Modals");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
