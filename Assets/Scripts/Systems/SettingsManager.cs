using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Main Menu Canvas Group")]
    public CanvasGroup mainMenuCanvasGroup;

    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Karanlık Efekt")]
    [Range(0f, 1f)]
    public float darkAmount = 0.5f;

    void Start()
    {
        // Settings başlangıçta kapalı
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Main Menu açık
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        // Main Menu normal görünüm
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1f;
            mainMenuCanvasGroup.interactable = true;
            mainMenuCanvasGroup.blocksRaycasts = true;
        }

        // Ses ayarlarını yükle
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
                musicSlider.value = AudioManager.Instance.GetMusicVolume();

            if (sfxSlider != null)
                sfxSlider.value = AudioManager.Instance.GetSfxVolume();
        }

        CreateVibrationToggle();
    }

    // ==================================
    // TİTREŞİM AYARI
    // ==================================

    private TMPro.TextMeshProUGUI vibrationText;

    // Ayarlar panelinin altına "TİTREŞİM: AÇIK/KAPALI" butonu ekler.
    void CreateVibrationToggle()
    {
        if (settingsPanel == null)
            return;

        UITheme theme = UITheme.Load();

        Button button = UIBuilder.CreateButton(theme, settingsPanel.transform, "VibrationButton", string.Empty, false, out vibrationText);
        UIBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 90f));

        button.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Haptics.Enabled = !Haptics.Enabled;
            UpdateVibrationText();

            // Açılınca hissettir
            Haptics.Medium();
        });

        UpdateVibrationText();
    }

    void UpdateVibrationText()
    {
        if (vibrationText != null)
        {
            vibrationText.text = Haptics.Enabled ? "TİTREŞİM: AÇIK" : "TİTREŞİM: KAPALI";
        }
    }

    // SETTINGS AÇ
    public void OpenSettings()
    {
        // Main Menu arkada kalır
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        // Settings açılır
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        // Main Menu karartılır
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1f - darkAmount;
            mainMenuCanvasGroup.interactable = false;
            mainMenuCanvasGroup.blocksRaycasts = false;
        }
    }

    // BACK BUTONU
    public void CloseSettings()
    {
        // Settings kapanır
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Main Menu tekrar açılır
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        // Main Menu normale döner
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.alpha = 1f;
            mainMenuCanvasGroup.interactable = true;
            mainMenuCanvasGroup.blocksRaycasts = true;
        }
    }

    // MUSIC
    public void ChangeMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    // SFX
    public void ChangeSfxVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(value);
        }
    }
}