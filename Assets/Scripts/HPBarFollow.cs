using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này vào PlayerHPSlider (RectTransform trên Screen Space Canvas).
/// Nó sẽ tự động di chuyển thanh máu đến vị trí phía trên nhân vật mỗi frame.
/// </summary>
public class HPBarFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // kéo Player vào đây

    [Header("Offset (world units)")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.7f, 0f); // cao hơn nhân vật

    private RectTransform rectTransform;
    private Canvas parentCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas  = GetComponentInParent<Canvas>();
    }

    void LateUpdate()
    {
        if (target == null || Camera.main == null) return;

        // Tính vị trí world của điểm trên đầu nhân vật
        Vector3 worldPos = target.position + worldOffset;

        // Chuyển sang tọa độ screen
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // Nếu nhân vật nằm sau camera thì ẩn thanh máu đi
        if (screenPos.z < 0f)
        {
            rectTransform.gameObject.SetActive(false);
            return;
        }
        rectTransform.gameObject.SetActive(true);

        // Chuyển screen position sang local position trong Canvas
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.GetComponent<RectTransform>(),
                screenPos,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
                out Vector2 localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }
}
