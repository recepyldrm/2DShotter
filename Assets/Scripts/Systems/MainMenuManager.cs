using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        TimeScaleController.ResetAll();
        SceneManager.LoadScene(GameKeys.FirstLevelScene);
    }

    public void ExitGame()
    {
        GameLog.Log("Oyundan çıkılıyor...");

        Application.Quit();
    }
}
