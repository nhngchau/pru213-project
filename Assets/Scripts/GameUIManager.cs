using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    private bool hasOpenModal;
    private Queue<System.Action> modalQueue = new Queue<System.Action>();
    private bool isProcessingQueue = false;

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
        if (modalContainer != null && hasOpenModal)
        {
            EnqueueModalAction(() => 
            {
                if (hasOpenModal)
                {
                    modalContainer.Pop(playAnimations).OnTerminate += () => hasOpenModal = false;
                }
            });
            return;
        }

        HideLegacyPanels();
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
            EnsurePausePanelHasAudioSettings();
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

    /// <summary>
    /// Đảm bảo màn pause có phần chỉnh âm lượng.
    ///
    /// Ba tình huống, và cả ba đều gặp trong project này:
    /// - Có PauseModal.prefab trong Resources -> Navigator lo, không đụng vào.
    /// - Chưa gán pausePanel -> dựng hẳn một bảng settings lúc chạy (giống ShowShop()).
    /// - Đã có PausePanel trong scene nhưng chỉ là khung rỗng -> gắn SettingsPanelUI vào chính nó,
    ///   phần EnsureRuntimeLayout() bên trong sẽ tự dựng slider. Gắn được cả khi object đang tắt:
    ///   Awake() chỉ chạy lúc nó được bật lên, tức đúng lúc ShowScreen() gọi SetActive(true).
    /// </summary>
    private void EnsurePausePanelHasAudioSettings()
    {
        if (CanShowWithNavigator(pauseModalKey))
        {
            return;
        }

        if (pausePanel == null)
        {
            pausePanel = SettingsPanelUI.CreateRuntimePanel(GetUiRoot());
            pausePanel.SetActive(false);
            return;
        }

        if (pausePanel.GetComponent<SettingsPanelUI>() == null)
        {
            pausePanel.AddComponent<SettingsPanelUI>();
        }
    }

    private void ShowScreen(string resourceKey, GameObject fallbackPanel)
    {
        ResolveModalContainer();

        if (CanShowWithNavigator(resourceKey))
        {
            EnqueueModalAction(() => 
            {
                hasOpenModal = true;
                modalContainer.Push(resourceKey, playAnimations).OnTerminate += () => { };
            });
            return;
        }

        if (fallbackPanel != null)
        {
            fallbackPanel.SetActive(true);
        }
    }

    private void EnqueueModalAction(System.Action action)
    {
        modalQueue.Enqueue(action);
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessModalQueue());
        }
    }

    private IEnumerator ProcessModalQueue()
    {
        isProcessingQueue = true;
        while (modalQueue.Count > 0)
        {
            while (modalContainer != null && modalContainer.IsInTransition)
            {
                yield return null;
            }
            
            var action = modalQueue.Dequeue();
            action?.Invoke();

            // Wait a frame so IsInTransition has a chance to turn true if a push/pop occurred
            yield return null;
        }
        isProcessingQueue = false;
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
