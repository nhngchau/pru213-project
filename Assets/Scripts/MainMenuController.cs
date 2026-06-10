using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Guide Panel")]
    [SerializeField] private GameObject guidePanel;

    [Header("Button Hover Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 10f;

    private Vector3 normalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;
    }

    private void Start()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, animationSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = normalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }

    public void OpenGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }
    }

    public void CloseGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
