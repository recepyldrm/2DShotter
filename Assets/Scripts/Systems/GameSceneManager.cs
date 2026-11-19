using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    // Game over ekranından çağrıldığında oyun durmuş olabilir;
    // sahne değişmeden önce timeScale sıfırlanır.
    public void LoadMainMenu()
    {
        TimeScaleController.ResetAll();
        SceneManager.LoadScene(GameKeys.MainMenuScene);
    }

    public void RestartGame()
    {
        TimeScaleController.ResetAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
