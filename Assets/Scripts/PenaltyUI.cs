using UnityEngine;
using TMPro;

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
