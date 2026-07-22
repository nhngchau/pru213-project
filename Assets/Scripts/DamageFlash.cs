using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng màn hình chớp đỏ khi Player hoặc Server bị damage.
/// Gắn script này lên một GameObject trong Canvas.
/// </summary>
public class DamageFlash : MonoBehaviour
{
    [Header("Flash Image")]
    [Tooltip("Image fullscreen màu cam — tạo trong Canvas rồi kéo vào đây")]
    [SerializeField] private Image flashImage;

    [Header("Server Hit Flash")]
    [SerializeField] private Color serverHitColor  = new Color(1f, 0.3f, 0f, 0.45f);
    [SerializeField] private float serverFadeDuration = 0.4f;

    private Coroutine flashRoutine;

    void Awake()
    {
        // Tự tạo Image nếu chưa gán
        if (flashImage == null)
        {
            flashImage = GetComponent<Image>();
        }

        if (flashImage != null)
        {
            flashImage.raycastTarget = false; // không block click
            SetAlpha(0f);
        }
    }

    void OnEnable()
    {
        GameEvents.OnServerHealthChanged += HandleServerDamage;
    }

    void OnDisable()
    {
        GameEvents.OnServerHealthChanged -= HandleServerDamage;
    }

    private int lastServerHP = -1;

    private void HandleServerDamage(int current, int max)
    {
        // Không log ở đây: hàm chạy mỗi lần Server đổi máu, tức mỗi con quái chạm vào.
        if (lastServerHP >= 0 && current < lastServerHP)
        {
            Flash(serverHitColor, serverFadeDuration);
        }
        lastServerHP = current;
    }

    private void Flash(Color color, float duration)
    {
        if (flashImage == null) return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        // Hiện ngay lập tức
        flashImage.color = color;

        // Fade dần về trong suốt
        float elapsed = 0f;
        Color startColor = color;
        Color endColor   = new Color(color.r, color.g, color.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // dùng unscaled để hoạt động khi game pause
            flashImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }

        SetAlpha(0f);
        flashRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (flashImage == null) return;
        Color c = flashImage.color;
        c.a = alpha;
        flashImage.color = c;
    }
}
