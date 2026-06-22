using UnityEngine;
using UnityEngine.UI;

public class ServerCore : MonoBehaviour, IDamageable
{
    [Header("Server Settings")]
    [SerializeField] private int maxHP = 1000;
    [SerializeField] private Slider hpSlider;

    private int currentHP;

    void Start()
    {
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    // GDD v3.0 - Firewall System upgrade: serverMaxHP += 100; serverCurrentHP += 100;
    public void IncreaseMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    public void TakeDamage(int damage)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
        {
            return;
        }

        currentHP -= damage;

        if (currentHP < 0)
        {
            currentHP = 0;
        }

        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        Debug.Log("Server HP: " + currentHP + "/" + maxHP);

        if (currentHP <= 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
            else
            {
                Debug.Log("Game Over! The central server has been destroyed.");
                Time.timeScale = 0f;
            }
        }
    }
}
