using UnityEngine;

public class ServerCore : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private int maxHP = 100;

    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
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
