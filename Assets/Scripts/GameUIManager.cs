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

    private bool hasOpenModal;

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
        GameEvents.OnLevelUpReady += ShowLevelUp;
        GameEvents.OnGameOver += ShowGameOver;
        GameEvents.OnGameWon += ShowWin;
        GameEvents.OnGamePaused += HandleGamePaused;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelUpReady -= ShowLevelUp;
        GameEvents.OnGameOver -= ShowGameOver;
        GameEvents.OnGameWon -= ShowWin;
        GameEvents.OnGamePaused -= HandleGamePaused;
    }

    public void CloseTopModal()
    {
        if (modalContainer != null && hasOpenModal)
        {
            modalContainer.Pop(playAnimations).OnTerminate += () => hasOpenModal = false;
            return;
        }

        HideLegacyPanels();
    }

    public void ShowShop()
    {
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
        ResolveModalContainer();

        if (CanShowWithNavigator(resourceKey))
        {
            hasOpenModal = true;
            modalContainer.Push(resourceKey, playAnimations).OnTerminate += () => { };
            return;
        }

        if (fallbackPanel != null)
        {
            fallbackPanel.SetActive(true);
        }
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
}
