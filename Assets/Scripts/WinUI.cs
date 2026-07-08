using UnityEngine;

public class WinUI : MonoBehaviour
{
    public void OnRestartClicked()
    {
        RunProgress.AdvanceStage();
        Time.timeScale = 1f;
        SceneTransition.LoadScene("GameScene");
    }

    public void OnShopClicked()
    {
        GameUIManager.Instance?.ShowShop();
    }

    public void OnNextStageClicked()
    {
        OnRestartClicked();
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene("MainMenuScene");
    }
}
