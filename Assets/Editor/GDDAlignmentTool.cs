#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-time alignment helper for GDD v3.0. Applies the scene/structural fixes that are
/// unsafe to hand-edit in YAML (sprite-dependent collider sizing, UI anchoring) using
/// the live editor data instead. Run from the menu: "GDD Tools > Apply v3.0 Scene Alignment".
///
/// Scope: ONLY upgrades existing objects. It never creates missing UI or gameplay features;
/// anything absent is reported as a warning so we keep this a clean, single-purpose commit.
/// Everything is wrapped in Undo, so the whole pass is reversible with Ctrl+Z.
/// </summary>
public static class GDDAlignmentTool
{
    private const string MenuPath = "GDD Tools/Apply v3.0 Scene Alignment";

    [MenuItem(MenuPath)]
    public static void ApplyAlignment()
    {
        int fixedCount = 0;

        fixedCount += FitServerColliderToSprite();
        fixedCount += AlignUiLayout();
        ReportCamera();

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[GDD Alignment] Done. Applied {fixedCount} fix(es). Review, play-test, then save the scene (Ctrl+S).");
        }
        else
        {
            Debug.Log("[GDD Alignment] No applicable objects found in the open scene. See warnings above.");
        }
    }

    /// <summary>
    /// GDD: the Server BoxCollider2D must perfectly wrap its sprite. We read the actual
    /// SpriteRenderer bounds at edit time (impossible to compute safely from raw YAML) and
    /// resize/centre the BoxCollider2D to match.
    /// </summary>
    private static int FitServerColliderToSprite()
    {
        int count = 0;

        // Look at both the visual "Server" object and the "ServerCore" damage object.
        foreach (string name in new[] { "Server", "ServerCore" })
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                continue;
            }

            SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
            BoxCollider2D box = go.GetComponent<BoxCollider2D>();

            if (sr == null || box == null)
            {
                Debug.LogWarning($"[GDD Alignment] '{name}' skipped: needs both a SpriteRenderer and a BoxCollider2D to auto-fit.");
                continue;
            }

            Undo.RecordObject(box, "Fit Server Collider To Sprite");

            // Convert sprite world bounds into the collider's local space (accounts for scale).
            Bounds worldBounds = sr.bounds;
            Vector3 lossy = box.transform.lossyScale;
            Vector2 localSize = new Vector2(
                Mathf.Abs(lossy.x) > 0.0001f ? worldBounds.size.x / Mathf.Abs(lossy.x) : worldBounds.size.x,
                Mathf.Abs(lossy.y) > 0.0001f ? worldBounds.size.y / Mathf.Abs(lossy.y) : worldBounds.size.y);

            Vector3 localCenter = box.transform.InverseTransformPoint(worldBounds.center);

            box.size = localSize;
            box.offset = new Vector2(localCenter.x, localCenter.y);

            EditorUtility.SetDirty(box);
            Debug.Log($"[GDD Alignment] '{name}' BoxCollider2D fitted to sprite. size={box.size}, offset={box.offset}.");
            count++;
        }

        return count;
    }

    /// <summary>
    /// GDD UI layout: Player HP (top-left), Build Progress (top-center), Server HP (above the
    /// Server). Repositions EXISTING bars only. Missing bars are reported, not created.
    /// </summary>
    private static int AlignUiLayout()
    {
        int count = 0;

        count += AnchorBar("PlayerHPBar", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), "top-left");
        count += AnchorBar("BuildProgressBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), "top-center");

        // Server HP bar exists today but is anchored to screen-centre. GDD wants it above the
        // Server. If it lives under a screen-space Canvas we re-anchor it to top-centre as the
        // closest faithful position; a true world-space follow is a later UI task.
        count += AnchorBar("ServerHPBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), "above-centre (interim)");

        return count;
    }

    private static int AnchorBar(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, string label)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning($"[GDD Alignment] UI '{name}' not found. Per GDD it should exist ({label}); creating it is a separate UI task, not part of this refactor.");
            return 0;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogWarning($"[GDD Alignment] '{name}' has no RectTransform; skipped.");
            return 0;
        }

        Undo.RecordObject(rt, "Align UI Bar");
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        EditorUtility.SetDirty(rt);

        Debug.Log($"[GDD Alignment] '{name}' re-anchored to {label}.");
        return 1;
    }

    /// <summary>
    /// The GDD does NOT define a camera orthographic size or position, so we deliberately do
    /// not change it (no numeric spec to enforce). We only log the current value for review.
    /// </summary>
    private static void ReportCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[GDD Alignment] No camera tagged MainCamera found.");
            return;
        }

        Debug.Log($"[GDD Alignment] Camera review only (no GDD spec): orthographic={cam.orthographic}, size={cam.orthographicSize}, pos={cam.transform.position}. No change applied.");
    }
}
#endif
