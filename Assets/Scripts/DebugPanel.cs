using UnityEngine;

/// <summary>
/// DEBUG / DEMO PANEL — chỉ hoạt động trong Unity Editor và Development Build.
/// TỰ ĐỘNG xuất hiện khi Play, KHÔNG CẦN add vào scene.
/// Nhấn F1 để bật/tắt.
/// </summary>
public class DebugPanel : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    // ── Tự tạo khi game bắt đầu ──────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<DebugPanel>() != null) return;

        GameObject go = new GameObject("[DebugPanel]");
        go.AddComponent<DebugPanel>();
        DontDestroyOnLoad(go);
    }

    // ── State ────────────────────────────────────────────────────────────────
    private bool isVisible = false;  // ẩn mặc định, bấm F1 để hiện
    private int  targetStage = 1;
    private bool godMode = false;

    // ── Cached refs ──────────────────────────────────────────────────────────
    private PlayerHealth playerHealth;

    // ── GUI Styles (lazy init) ───────────────────────────────────────────────
    private GUIStyle boxStyle;
    private GUIStyle titleStyle;
    private GUIStyle btnStyle;
    private GUIStyle btnGreenStyle;
    private GUIStyle btnGoldStyle;
    private GUIStyle labelStyle;
    private bool stylesReady = false;

    private const int PANEL_W = 265;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        targetStage = RunProgress.Stage;
        CacheRefs();
    }

    void Update()
    {
        // F1 để toggle
        if (Input.GetKeyDown(KeyCode.F1))
            isVisible = !isVisible;

        // God mode: liên tục hồi máu
        if (godMode)
        {
            if (playerHealth == null) CacheRefs();
            playerHealth?.DebugHealToFull();
        }
    }

    void OnGUI()
    {
        if (!isVisible) return;
        if (!stylesReady) InitStyles();

        float x = Screen.width  - PANEL_W - 10f;
        float y = 10f;
        float panelH = 340f;

        // Nền tối
        GUI.Box(new Rect(x - 8, y - 8, PANEL_W + 16, panelH + 16), GUIContent.none, boxStyle);

        float cx = x;
        float cy = y;

        // ── Tiêu đề ────────────────────────────────────────────────────────
        GUI.Label(new Rect(cx, cy, PANEL_W, 28), "⚙  DEBUG / DEMO PANEL", titleStyle);
        cy += 24f;

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            normal   = { textColor = new Color(0.5f, 0.5f, 0.5f) },
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(cx, cy, PANEL_W, 16), "F1 = ẩn/hiện", hintStyle);
        cy += 22f;

        // Đường kẻ ngang
        DrawLine(new Rect(cx, cy, PANEL_W, 1), new Color(0.3f, 0.3f, 0.5f));
        cy += 8f;

        // ── Stage hiện tại ─────────────────────────────────────────────────
        GUI.Label(new Rect(cx, cy, PANEL_W, 22), $"Stage hiện tại: <b>{RunProgress.Stage}</b>", labelStyle);
        cy += 26f;

        // ── Jump Stage ────────────────────────────────────────────────────
        GUI.Label(new Rect(cx, cy, 70, 24), "Nhảy stage:", labelStyle);
        if (GUI.Button(new Rect(cx + 74, cy, 26, 24), "−", btnStyle))
            targetStage = Mathf.Max(1, targetStage - 1);
        GUI.Label(new Rect(cx + 103, cy, 34, 24), $"{targetStage}", labelStyle);
        if (GUI.Button(new Rect(cx + 136, cy, 26, 24), "+", btnStyle))
            targetStage++;
        if (GUI.Button(new Rect(cx + 168, cy, 90, 24), "JUMP ▶", btnGoldStyle))
            JumpToStage(targetStage);
        cy += 34f;

        // ── Win ngay ──────────────────────────────────────────────────────
        if (GUI.Button(new Rect(cx, cy, PANEL_W, 30), "🏆  WIN STAGE NOW", btnGoldStyle))
            WinNow();
        cy += 38f;

        DrawLine(new Rect(cx, cy, PANEL_W, 1), new Color(0.3f, 0.3f, 0.5f));
        cy += 8f;

        // ── God Mode ──────────────────────────────────────────────────────
        string godLabel = godMode ? "🛡  GOD MODE : BẬT  (click để tắt)" : "🛡  GOD MODE : tắt  (click để bật)";
        if (GUI.Button(new Rect(cx, cy, PANEL_W, 28), godLabel, godMode ? btnGreenStyle : btnStyle))
            ToggleGodMode();
        cy += 34f;

        // ── Heal ──────────────────────────────────────────────────────────
        if (GUI.Button(new Rect(cx, cy, PANEL_W, 28), "💊  Hồi máu player về full", btnStyle))
            HealPlayer();
        cy += 34f;

        DrawLine(new Rect(cx, cy, PANEL_W, 1), new Color(0.3f, 0.3f, 0.5f));
        cy += 8f;

        // ── Time Scale ────────────────────────────────────────────────────
        GUI.Label(new Rect(cx, cy, 80, 24), "Tốc độ:", labelStyle);
        float bw = 56f;
        float[] scales = { 1f, 2f, 3f };
        for (int i = 0; i < scales.Length; i++)
        {
            bool active = Mathf.Approximately(Time.timeScale, scales[i]);
            if (GUI.Button(new Rect(cx + 82 + i * (bw + 4), cy, bw, 24),
                           $"x{scales[i]:0}", active ? btnGreenStyle : btnStyle))
                SetTimeScale(scales[i]);
        }
        cy += 34f;

        // ── DataPack ──────────────────────────────────────────────────────
        GUI.Label(new Rect(cx, cy, 70, 24), "DataPack:", labelStyle);
        if (GUI.Button(new Rect(cx + 74,  cy, 88, 24), "+500",  btnStyle)) RunProgress.AddDataPack(500);
        if (GUI.Button(new Rect(cx + 168, cy, 90, 24), "+2000", btnStyle)) RunProgress.AddDataPack(2000);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Actions
    // ─────────────────────────────────────────────────────────────────────────

    private void WinNow()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameEnded)
            GameManager.Instance.TriggerWin();
        else
            Debug.LogWarning("[DebugPanel] GameManager không tìm thấy hoặc game đã kết thúc.");
    }

    private void JumpToStage(int stage)
    {
        RunProgress.DebugSetStage(stage);
    }

    private void ToggleGodMode()
    {
        godMode = !godMode;
        CacheRefs();
        playerHealth?.DebugSetGodMode(godMode);
    }

    private void HealPlayer()
    {
        CacheRefs();
        playerHealth?.DebugHealToFull();
    }

    private void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void CacheRefs()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private static void DrawLine(Rect rect, Color color)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }

    private void InitStyles()
    {
        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0.07f, 0.07f, 0.13f, 0.95f)) }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.4f, 0.85f, 1f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 12,
            richText  = true,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(0.88f, 0.88f, 0.88f) }
        };

        btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 12,
            fontStyle = FontStyle.Bold,
            normal    = { background = MakeTex(2, 2, new Color(0.22f, 0.23f, 0.33f, 1f)), textColor = Color.white }
        };

        btnGreenStyle = new GUIStyle(btnStyle)
        {
            normal = { background = MakeTex(2, 2, new Color(0.1f, 0.42f, 0.15f, 1f)), textColor = Color.white }
        };

        btnGoldStyle = new GUIStyle(btnStyle)
        {
            normal = { background = MakeTex(2, 2, new Color(0.5f, 0.38f, 0.04f, 1f)), textColor = new Color(1f, 0.92f, 0.35f) }
        };

        stylesReady = true;
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = col;
        Texture2D t = new Texture2D(w, h);
        t.SetPixels(px);
        t.Apply();
        return t;
    }

#endif
}
