using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Modal;

public class GameOverUI : Modal
{
    public void OnRestartClicked()
    {
        RunProgress.ResetRun();
        Time.timeScale = 1f;
        SceneTransition.LoadScene("GameScene");
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene("MainMenuScene");
    }
}
