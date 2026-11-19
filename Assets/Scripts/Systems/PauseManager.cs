using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;

    [Header("Ses")]
    public TextMeshProUGUI soundButtonText;

    private bool isPaused = false;

    void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        UpdateSoundButton();
    }

    void OnDestroy()
    {
        TimeScaleController.Resume(this);
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        UpdateSoundButton();

        TimeScaleController.Pause(this);
    }

    // Sadece pause'un kendi isteğini kaldırır. Kart ekranı veya
    // game over açıksa oyun durmaya devam eder.
    public void ResumeGame()
    {
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        TimeScaleController.Resume(this);
    }

    // =====================================
    // SES AÇ / KAPAT
    // =====================================

    public void ToggleSound()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ToggleAllSound();

        UpdateSoundButton();
    }

    private void UpdateSoundButton()
    {
        if (soundButtonText == null)
            return;

        if (AudioManager.Instance == null)
            return;

        if (AudioManager.Instance.IsAllSoundMuted())
        {
            soundButtonText.text =
                "SES: KAPALI";
        }
        else
        {
            soundButtonText.text =
                "SES: AÇIK";
        }
    }

    // =====================================
    // RESTART
    // =====================================

    public void RestartGame()
    {
        TimeScaleController.ResetAll();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // =====================================
    // MAIN MENU
    // =====================================

    public void GoToMainMenu()
    {
        TimeScaleController.ResetAll();

        SceneManager.LoadScene(
            GameKeys.MainMenuScene
        );
    }
}
