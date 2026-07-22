using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị popup thông báo khi người chơi đạt Milestone (mỗi 3 stage).
/// Gắn script này lên MilestonePopup GameObject trong Canvas.
/// </summary>
public class MilestoneUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text bonusText;
    [SerializeField] private TMP_Text titleText;

    [Header("Animation")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeSpeed = 2f;

    private CanvasGroup canvasGroup;
    private Coroutine showRoutine;

    void Awake()
    {
        canvasGroup = popupRoot != null
            ? popupRoot.GetComponent<CanvasGroup>() ?? popupRoot.AddComponent<CanvasGroup>()
            : null;

        HideImmediate();
    }

    void OnEnable()
    {
        GameEvents.OnMilestoneReached += HandleMilestone;
    }

    void OnDisable()
    {
        GameEvents.OnMilestoneReached -= HandleMilestone;
    }

    private void HandleMilestone(int stage, int bonusDataPack)
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowRoutine(stage, bonusDataPack));
    }

    private IEnumerator ShowRoutine(int stage, int bonusDataPack)
    {
        // Cập nhật nội dung text
        if (titleText  != null) titleText.text  = "🏆 MILESTONE!";
        if (stageText  != null) stageText.text  = $"Stage {stage} Complete!";
        if (bonusText  != null) bonusText.text  = $"+{bonusDataPack} DataPack Bonus!";

        // Hiện popup với fade in
        if (popupRoot != null) popupRoot.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));

        // Giữ hiện trong displayDuration giây
        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f));

        HideImmediate();
        showRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float duration = 1f / fadeSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void HideImmediate()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}
