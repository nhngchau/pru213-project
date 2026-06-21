using UnityEngine;
using TMPro;

/// <summary>
/// Displays the 5-second penalty countdown (GDD v3.0 - Section V). Listens to PlayerEvents and
/// never references PlayerHealth directly. Lives on an always-active object (e.g. the Canvas) so
/// it keeps receiving events; it only toggles the child text object's visibility.
/// </summary>
public class PenaltyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text penaltyText;

    void OnEnable()
    {
        PlayerEvents.OnPenaltyCountdown += HandleCountdown;
        PlayerEvents.OnPlayerRespawned += Hide;
    }

    void OnDisable()
    {
        PlayerEvents.OnPenaltyCountdown -= HandleCountdown;
        PlayerEvents.OnPlayerRespawned -= Hide;
    }

    void Start()
    {
        Hide();
    }

    private void HandleCountdown(int secondsRemaining)
    {
        if (penaltyText == null)
        {
            return;
        }

        if (secondsRemaining <= 0)
        {
            Hide();
            return;
        }

        if (!penaltyText.gameObject.activeSelf)
        {
            penaltyText.gameObject.SetActive(true);
        }

        penaltyText.text = $"REBOOTING... {secondsRemaining}";
    }

    private void Hide()
    {
        if (penaltyText != null)
        {
            penaltyText.gameObject.SetActive(false);
        }
    }
}
