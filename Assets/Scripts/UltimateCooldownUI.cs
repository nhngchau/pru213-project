using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom-left cooldown box for the Refactor Ultimate. Bright when ready; darkened with a countdown
/// number while on cooldown, brightening back to normal as the cooldown reaches 0. Pure presentation -
/// it just polls PlayerUltimate (found automatically in the scene).
/// </summary>
public class UltimateCooldownUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;        // the square box
    [SerializeField] private TMP_Text countdownText; // remaining seconds (hidden when ready)

    [Header("Colors")]
    [SerializeField] private Color readyColor = new Color(0.35f, 0.9f, 1f, 1f);        // bright (matches the wave)
    [SerializeField] private Color cooldownColor = new Color(0.12f, 0.18f, 0.22f, 1f); // dark

    private PlayerUltimate ultimate;

    void Start()
    {
        ultimate = FindFirstObjectByType<PlayerUltimate>();
        Refresh();
    }

    void Update()
    {
        if (ultimate == null)
        {
            return; // no Ultimate in the scene -> leave the box as-is
        }
        Refresh();
    }

    private void Refresh()
    {
        if (ultimate == null || ultimate.IsReady)
        {
            // Full / ready: bright box, no number.
            if (iconImage != null) iconImage.color = readyColor;
            if (countdownText != null && countdownText.text.Length != 0) countdownText.text = string.Empty;
            return;
        }

        // On cooldown: dark -> bright as it recovers, plus the remaining whole seconds.
        if (iconImage != null)
        {
            iconImage.color = Color.Lerp(cooldownColor, readyColor, ultimate.CooldownProgress);
        }
        if (countdownText != null)
        {
            countdownText.text = Mathf.CeilToInt(ultimate.CooldownRemaining).ToString();
        }
    }
}
