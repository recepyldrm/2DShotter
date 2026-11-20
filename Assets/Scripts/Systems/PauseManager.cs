using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Duraklatma menüsü. Sahnedeki DURAKLAT butonu PauseGame'i çağırır.
// Menü oyunun temasıyla kodla kurulur (sahnedeki eski panel gizlenir):
// DEVAM ET, YENİDEN BAŞLA (iki kez basınca), AYARLAR, ANA MENÜ.
// Telefonun geri tuşu / Esc menüyü açıp kapatır; uygulama arka plana
// geçince oyun kendiliğinden duraklar.
public class PauseManager : MonoBehaviour
{
    [Header("UI (eski panel; kod menüsü kullanılırsa gizlenir)")]
    public GameObject pausePanel;

    [Header("Ses")]
    public TextMeshProUGUI soundButtonText;

    private bool isPaused = false;

    private UITheme theme;
    private GameObject menuRoot;
    private TextMeshProUGUI restartText;
    private TextMeshProUGUI soundText;
    private TextMeshProUGUI infoText;
    private SettingsUI settingsUI;

    // Yeniden başlat yanlışlıkla basılmasın: ilk basış onay ister.
    private bool restartArmed;

    public bool IsPaused => isPaused;

    void Start()
    {
        theme = UITheme.Load();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        BuildMenu();
        UpdateSoundButton();
    }

    void OnDestroy()
    {
        TimeScaleController.Resume(this);
    }

    void Update()
    {
        if (BackPressed())
        {
            OnBackPressed();
        }
    }

    static bool BackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        // Android'in geri tuşu Esc olarak gelir.
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    void OnBackPressed()
    {
        if (isPaused)
        {
            // Ayarlar açıksa önce onu kapat.
            if (settingsUI != null && settingsUI.IsOpen)
            {
                settingsUI.Hide();
                return;
            }

            ResumeGame();
            return;
        }

        if (CanPause())
        {
            PauseGame();
        }
    }

    // Kart ekranı, game over, tanıtım vb. açıkken duraklatma menüsü açılmaz.
    bool CanPause()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return false;

        return !TimeScaleController.IsPaused;
    }

    // Telefon başka uygulamaya geçince oyun durur.
    void OnApplicationPause(bool paused)
    {
        if (paused && !isPaused && CanPause())
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        isPaused = true;
        restartArmed = false;

        if (menuRoot != null)
        {
            RefreshMenu();
            menuRoot.SetActive(true);
        }
        else if (pausePanel != null)
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

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsUI != null)
        {
            settingsUI.Hide();
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
        if (AudioManager.Instance == null)
            return;

        string label = Loc.T(AudioManager.Instance.IsAllSoundMuted() ? "SES: KAPALI" : "SES: AÇIK");

        if (soundButtonText != null)
            soundButtonText.text = label;

        if (soundText != null)
            soundText.text = label;
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

    // =====================================
    // MENÜ
    // =====================================

    void OnRestartClicked()
    {
        UIBuilder.PlayClick();

        if (!restartArmed)
        {
            restartArmed = true;
            RefreshMenu();
            return;
        }

        RestartGame();
    }

    void OnSettingsClicked()
    {
        UIBuilder.PlayClick();

        if (settingsUI == null)
        {
            settingsUI = gameObject.AddComponent<SettingsUI>();
            settingsUI.OnClosed += UpdateSoundButton;
        }

        settingsUI.Show();
    }

    void RefreshMenu()
    {
        restartText.text = restartArmed
            ? Loc.Pick("EMİN MİSİN? TEKRAR BAS", "SURE? TAP AGAIN")
            : Loc.Pick("YENİDEN BAŞLA", "RESTART");

        restartText.color = restartArmed ? theme.mainRedHover : theme.mainWhite;

        // Kısa oyun bilgisi: seviye ve skor
        int level = LevelSystem.Instance != null ? LevelSystem.Instance.currentLevel : 0;
        int score = GameManager.Instance != null ? GameManager.Instance.score : 0;

        infoText.text =
            Loc.Pick("Seviye ", "Level ") + level + "   ·   " +
            Loc.Pick("Skor ", "Score ") + score + "   ·   " +
            Difficulty.Label(Difficulty.Current);
    }

    void BuildMenu()
    {
        Transform canvas = UIBuilder.CreateCanvas(transform, "PauseCanvas", 420);
        RectTransform dim = UIBuilder.CreateDim(canvas, "PauseMenu", 0.75f);
        menuRoot = canvas.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.33f, 0.1f),
            new Vector2(0.67f, 0.9f)
        );

        UIBuilder.CreateWindowTitle(theme, window, Loc.Pick("DURAKLATILDI", "PAUSED"));

        infoText = UIBuilder.CreateText(theme, window, string.Empty, 28f, TextAlignmentOptions.Center, theme.greyLight);
        UIBuilder.AnchorTop(infoText.rectTransform, 150f, 45f);

        RectTransform column = UIBuilder.CreateRect("Buttons", window);
        UIBuilder.Stretch(column);
        column.offsetMin = new Vector2(70f, 50f);
        column.offsetMax = new Vector2(-70f, -215f);

        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddButton(column, "DEVAM ET", true, ResumeGameWithClick, out _);
        AddButton(column, "YENİDEN BAŞLA", false, OnRestartClicked, out restartText);
        AddButton(column, "AYARLAR", false, OnSettingsClicked, out _);
        AddButton(column, string.Empty, false, () => { UIBuilder.PlayClick(); ToggleSound(); }, out soundText);
        AddButton(column, "ANA MENÜ", false, () => { UIBuilder.PlayClick(); GoToMainMenu(); }, out _);

        menuRoot.SetActive(false);
    }

    void ResumeGameWithClick()
    {
        UIBuilder.PlayClick();
        ResumeGame();
    }

    void AddButton(RectTransform parent, string label, bool red, UnityEngine.Events.UnityAction action, out TextMeshProUGUI text)
    {
        Button button = UIBuilder.CreateButton(theme, parent, "PauseButton", label, red, out text);
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;
        button.onClick.AddListener(action);
    }
}
