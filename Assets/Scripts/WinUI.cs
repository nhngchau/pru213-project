using UnityEngine;

public class WinUI : MonoBehaviour
{
    public void OnRestartClicked()
    {
        OnShopClicked();
    }

    public void OnShopClicked()
    {
        GameUIManager.Instance?.ShowShop();
    }

    public void OnNextStageClicked()
    {
        OnShopClicked();
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene("MainMenuScene");
    }
}
