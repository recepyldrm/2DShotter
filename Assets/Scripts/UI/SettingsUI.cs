using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ayarlar penceresi: müzik / efekt sesi, titreşim, grafik kalitesi ve dil.
// SettingsManager açar; kendi arayüzünü kodla kurar.
public class SettingsUI : MonoBehaviour
{
    private UITheme theme;
    private GameObject panel;

    private Slider musicSlider;
    private Slider sfxSlider;
    private TextMeshProUGUI vibrationText;
    private TextMeshProUGUI graphicsText;
    private TextMeshProUGUI languageText;

    public System.Action OnClosed;

    void Awake()
    {
        theme = UITheme.Load();
        Transform canvas = UIBuilder.CreateCanvas(transform, "SettingsCanvas", 300);
        Build(canvas);
    }

    public void Show()
    {
        Refresh();
        panel.SetActive(true);
    }

    void Close()
    {
        UIBuilder.PlayClick();
        panel.SetActive(false);
        OnClosed?.Invoke();
    }

    void Refresh()
    {
        if (AudioManager.Instance != null)
        {
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSfxVolume());
        }

        vibrationText.text = Loc.T(Haptics.Enabled ? "AÇIK" : "KAPALI");
        graphicsText.text = GraphicsQuality.Label(GraphicsQuality.Current);
        languageText.text = Loc.LanguageName(Loc.Current);
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void Build(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "SettingsPanel");
        panel = dim.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.22f, 0.08f),
            new Vector2(0.78f, 0.92f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "AYARLAR");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        RectTransform content = UIBuilder.CreateScrollList(window, new Vector2(70f, 40f), new Vector2(-70f, -160f), 18f);

        musicSlider = UIBuilder.CreateSlider(theme, CreateRow(content, "MÜZİK SESİ"), 0.5f);
        Stretch(musicSlider);
        musicSlider.onValueChanged.AddListener(value =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
        });

        sfxSlider = UIBuilder.CreateSlider(theme, CreateRow(content, "EFEKT SESİ"), 1f);
        Stretch(sfxSlider);
        sfxSlider.onValueChanged.AddListener(value =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSfxVolume(value);
        });

        // Bırakınca örnek ses çalsın
        AddPointerUp(sfxSlider.gameObject, () =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayGoldPickup();
        });

        vibrationText = CreateToggleButton(CreateRow(content, "TİTREŞİM"), () =>
        {
            Haptics.Enabled = !Haptics.Enabled;
            Haptics.Medium();
        });

        graphicsText = CreateToggleButton(CreateRow(content, "GRAFİK"), () =>
        {
            GraphicsQuality.Current = (GraphicsQuality.Level)(((int)GraphicsQuality.Current + 1) % 3);
        });

        TextMeshProUGUI hint = UIBuilder.CreateText(
            theme,
            content,
            "Düşük: pil tasarrufu (30 FPS). Yüksek: en akıcı görüntü.",
            24f,
            TextAlignmentOptions.TopRight,
            theme.greyLight
        );
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        languageText = CreateToggleButton(CreateRow(content, "DİL"), () =>
        {
            // Sahne yeniden yüklenir, bütün yazılar yeni dille kurulur.
            Loc.SetLanguage(Loc.IsEnglish ? GameLanguage.Turkish : GameLanguage.English);
        });

        panel.SetActive(false);
    }

    // Solda başlık, sağda kontrol alanı olan satır. Kontrol alanını döner.
    RectTransform CreateRow(RectTransform content, string label)
    {
        RectTransform row = UIBuilder.CreateRect("Row_" + label, content);
        theme.ApplyNotice(row.gameObject.AddComponent<Image>());
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;

        TextMeshProUGUI title = UIBuilder.CreateText(theme, row, label, 38f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        title.rectTransform.anchorMin = new Vector2(0f, 0f);
        title.rectTransform.anchorMax = new Vector2(0.45f, 1f);
        title.rectTransform.offsetMin = new Vector2(45f, 0f);
        title.rectTransform.offsetMax = Vector2.zero;
        title.enableAutoSizing = true;
        title.fontSizeMin = 22f;
        title.fontSizeMax = 38f;

        RectTransform control = UIBuilder.CreateRect("Control", row);
        control.anchorMin = new Vector2(0.48f, 0f);
        control.anchorMax = new Vector2(1f, 1f);
        control.offsetMin = new Vector2(0f, 15f);
        control.offsetMax = new Vector2(-40f, -15f);

        return control;
    }

    TextMeshProUGUI CreateToggleButton(RectTransform control, System.Action onClick)
    {
        Button button = UIBuilder.CreateButton(theme, control, "ToggleButton", string.Empty, true, out TextMeshProUGUI text);
        UIBuilder.Stretch((RectTransform)button.transform);

        button.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            onClick();
            Refresh();
        });

        return text;
    }

    static void Stretch(Slider slider)
    {
        UIBuilder.Stretch((RectTransform)slider.transform);
    }

    static void AddPointerUp(GameObject target, System.Action action)
    {
        UnityEngine.EventSystems.EventTrigger trigger = target.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp
        };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }
}
