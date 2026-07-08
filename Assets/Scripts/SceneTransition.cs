using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    [SerializeField] private float defaultFadeOutDuration = 0.35f;
    [SerializeField] private float defaultFadeInDuration = 0.25f;
    [SerializeField] private Color fadeColor = Color.black;

    private CanvasGroup canvasGroup;
    private Image fadeImage;
    private bool isTransitioning;

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    public static void LoadScene(string sceneName)
    {
        EnsureInstance().StartTransition(sceneName);
    }

    public static void LoadScene(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        EnsureInstance().StartTransition(sceneName, fadeOutDuration, fadeInDuration);
    }

    private static SceneTransition EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject root = new GameObject("SceneTransition");
        instance = root.AddComponent<SceneTransition>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup == null)
        {
            BuildOverlay();
        }
    }

    private void BuildOverlay()
    {
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject imageObject = new GameObject("Fade Image", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true;
    }

    private void StartTransition(string sceneName)
    {
        StartTransition(sceneName, defaultFadeOutDuration, defaultFadeInDuration);
    }

    private void StartTransition(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        if (isTransitioning || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        StartCoroutine(TransitionRoutine(sceneName, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator TransitionRoutine(string sceneName, float fadeOutDuration, float fadeInDuration)
    {
        isTransitioning = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        yield return Fade(0f, 1f, fadeOutDuration);

        Time.timeScale = 1f;
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (loadOperation != null && !loadOperation.isDone)
        {
            yield return null;
        }

        yield return Fade(1f, 0f, fadeInDuration);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
