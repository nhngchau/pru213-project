using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityScreenNavigator.Runtime.Core.Modal;

public class GameOverUI : Modal
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        AutoBindButtons();
        EnsureButtons();
        BindButtonActions();
    }

    public override void DidPushEnter()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OnRestartClicked()
    {
        // Quay về stage mốc gần nhất (3, 6, 9) thay vì reset về stage 1
        RunProgress.RestartFromCheckpoint();
        Time.timeScale = 1f;
        SceneTransition.LoadScene("GameScene");
    }

    public void OnMainMenuClicked()
    {
        RunProgress.ClearSavedRun();
        Time.timeScale = 1f;
        SceneTransition.LoadScene("MainMenuScene");
    }

    private void AutoBindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            string objectName = button.name.ToLowerInvariant();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string labelText = label != null ? label.text.ToLowerInvariant() : string.Empty;

            if (restartButton == null
                && (objectName.Contains("restart")
                    || objectName.Contains("again")
                    || labelText.Contains("restart")
                    || labelText.Contains("again")
                    || labelText.Contains("play")))
            {
                restartButton = button;
                continue;
            }

            if (mainMenuButton == null
                && (objectName.Contains("mainmenu")
                    || objectName.Contains("main menu")
                    || objectName.Contains("menu")
                    || labelText.Contains("main menu")
                    || labelText.Contains("menu")))
            {
                mainMenuButton = button;
            }
        }
    }

    private void EnsureButtons()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
        {
            root = gameObject.AddComponent<RectTransform>();
        }

        if (restartButton == null)
        {
            restartButton = CreateButton("RestartButton", root, "PLAY AGAIN");
            SetRect((RectTransform)restartButton.transform, new Vector2(0.52f, 0.22f), new Vector2(0.78f, 0.22f), new Vector2(0f, 0f), new Vector2(0f, 58f));
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = CreateButton("MainMenuButton", root, "MAIN MENU");
            SetRect((RectTransform)mainMenuButton.transform, new Vector2(0.22f, 0.22f), new Vector2(0.48f, 0.22f), new Vector2(0f, 0f), new Vector2(0f, 58f));
        }
    }

    private void BindButtonActions()
    {
        if (restartButton != null && restartButton.onClick.GetPersistentEventCount() == 0)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null && mainMenuButton.onClick.GetPersistentEventCount() == 0)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.7f, 0.08f, 0.12f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        RectTransform textRect = CreateRect("Label", buttonObject.transform);
        SetRect(textRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
