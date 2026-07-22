using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Modal;

/// <summary>
/// Thin UI state router for screen-level panels. Realtime HUD widgets still listen to events directly.
/// Uses UnityScreenNavigator when a ModalContainer is available, with legacy panel fallbacks while migrating.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;
    public static bool UsesNavigatorUpgradeModal => Instance != null && Instance.CanShowWithNavigator(Instance.upgradeModalKey);

    [Header("UnityScreenNavigator")]
    [SerializeField] private ModalContainer modalContainer;
    [SerializeField] private string modalContainerName = "GameModalContainer";
    [SerializeField] private bool playAnimations = true;

    [Header("Resource Keys")]
    [SerializeField] private string upgradeModalKey = "UI/Modals/UpgradeModal";
    [SerializeField] private string gameOverModalKey = "UI/Modals/GameOverModal";
    [SerializeField] private string winModalKey = "UI/Modals/WinModal";
    [SerializeField] private string shopModalKey = "UI/Modals/ShopModal";
    [SerializeField] private string pauseModalKey = "UI/Modals/PauseModal";

    [Header("Legacy Panel Fallbacks")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject pausePanel;

    private enum ModalOpType { Push, Pop }

    private struct ModalOp
    {
        public ModalOpType Type;
        public string ResourceKey;
        public GameObject FallbackPanel;
    }

    // All navigator Push/Pop calls funnel through this queue so we never start a
    // transition while another is still running (which throws "already in transition"
    // and leaves Time.timeScale stuck at 0, freezing the game).
    private readonly Queue<ModalOp> modalOps = new Queue<ModalOp>();
    private Coroutine modalWorker;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveModalContainer();
        HideLegacyPanels();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnLevelUpReady   += ShowLevelUp;
        GameEvents.OnGameOver       += ShowGameOver;
        GameEvents.OnGameWon        += ShowWin;
        GameEvents.OnGameCompleted  += ShowGameCompleted;
        GameEvents.OnGamePaused     += HandleGamePaused;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelUpReady   -= ShowLevelUp;
        GameEvents.OnGameOver       -= ShowGameOver;
        GameEvents.OnGameWon        -= ShowWin;
        GameEvents.OnGameCompleted  -= ShowGameCompleted;
        GameEvents.OnGamePaused     -= HandleGamePaused;
    }

    public void CloseTopModal()
    {
        EnqueueModalOp(new ModalOp { Type = ModalOpType.Pop });
    }

    public void ShowShop()
    {
        if (!CanShowWithNavigator(shopModalKey) && shopPanel == null)
        {
            shopPanel = ShopPanelUI.CreateRuntimePanel(GetUiRoot());
            shopPanel.SetActive(false);
        }

        ShowScreen(shopModalKey, shopPanel);
    }

    private void ShowLevelUp(int _)
    {
        ShowScreen(upgradeModalKey, upgradePanel);
    }

    private void ShowGameOver()
    {
        ShowScreen(gameOverModalKey, gameOverPanel);
    }

    private void ShowWin()
    {
        // Thắng stage thường → mở Shop để chuẩn bị stage tiếp theo
        ShowShop();
    }

    private void ShowGameCompleted()
    {
        // Clear Stage 10 → thắng toàn bộ game!
        // Xóa save để lần sau bắt đầu mới
        RunProgress.ClearSavedRun();
        UnityEngine.Debug.Log("[GameUIManager] GAME COMPLETED! Player cleared all 10 stages!");

        // Hiện WinModal nếu có, fallback về winPanel
        ShowScreen(winModalKey, winPanel);
    }

    private void HandleGamePaused(bool isPaused)
    {
        if (isPaused)
        {
            ShowScreen(pauseModalKey, pausePanel);
        }
        else
        {
            // Close pause panel
            if (pausePanel != null && pausePanel.activeSelf)
            {
                pausePanel.SetActive(false);
            }
            // If using Navigator, might need to close top modal if it's the pause modal
            CloseTopModal();
        }
    }

    private void ShowScreen(string resourceKey, GameObject fallbackPanel)
    {
        EnqueueModalOp(new ModalOp
        {
            Type = ModalOpType.Push,
            ResourceKey = resourceKey,
            FallbackPanel = fallbackPanel,
        });
    }

    private void EnqueueModalOp(ModalOp op)
    {
        modalOps.Enqueue(op);
        if (modalWorker == null)
        {
            modalWorker = StartCoroutine(ProcessModalOps());
        }
    }

    // Serialises modal transitions: wait for the container to be idle, run one op,
    // wait for it to finish, then move on. "yield return null" ticks even at
    // timeScale 0 and the navigator animates on unscaled time, so this never deadlocks.
    private IEnumerator ProcessModalOps()
    {
        while (modalOps.Count > 0)
        {
            ResolveModalContainer();

            while (modalContainer != null && modalContainer.IsInTransition)
            {
                yield return null;
            }

            ExecuteModalOp(modalOps.Dequeue());

            yield return null;
            while (modalContainer != null && modalContainer.IsInTransition)
            {
                yield return null;
            }
        }

        modalWorker = null;
    }

    private void ExecuteModalOp(ModalOp op)
    {
        if (op.Type == ModalOpType.Push)
        {
            if (CanShowWithNavigator(op.ResourceKey))
            {
                modalContainer.Push(op.ResourceKey, playAnimations);
                return;
            }

            if (op.FallbackPanel != null)
            {
                op.FallbackPanel.SetActive(true);
            }

            return;
        }

        // Pop: close the top navigator modal, or fall back to hiding legacy panels.
        if (modalContainer != null && modalContainer.OrderedModalIds.Count > 0)
        {
            modalContainer.Pop(playAnimations);
            return;
        }

        HideLegacyPanels();
    }

    private void ResolveModalContainer()
    {
        if (modalContainer == null && !string.IsNullOrWhiteSpace(modalContainerName))
        {
            modalContainer = ModalContainer.Find(modalContainerName);
        }
    }

    private bool CanShowWithNavigator(string resourceKey)
    {
        ResolveModalContainer();
        return modalContainer != null
            && !string.IsNullOrWhiteSpace(resourceKey)
            && Resources.Load<GameObject>(resourceKey) != null;
    }

    private void HideLegacyPanels()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private Transform GetUiRoot()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        return canvas != null ? canvas.transform : transform;
    }
}
