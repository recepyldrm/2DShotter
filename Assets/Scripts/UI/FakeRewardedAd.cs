using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gerçek reklam ağı bağlanana kadar kullanılan test reklamı.
// Oyunun temasında (UITheme) bir "reklam" sayfası gösterir:
// geri sayım bitince CLAIM REWARD ile ödül alınır,
// SKIP ile erken kapatılırsa ödül verilmez (iptal senaryosunu test etmek için).
// Oyun durmuşken (timeScale = 0) de çalışır.
public class FakeRewardedAd : MonoBehaviour, IRewardedAdProvider
{
    private const float AdDuration = 5f;

    // Panel, çerçeve görselinin oranında (1011x1249)
    private static readonly Vector2 PanelSize = new Vector2(720f, 890f);

    private UITheme theme;

    private GameObject overlay;
    private TextMeshProUGUI rewardText;
    private TextMeshProUGUI countdownText;
    private RectTransform progressFill;
    private Button claimButton;
    private TextMeshProUGUI claimText;
    private Button skipButton;

    private Action<bool> onFinished;
    private float remaining;
    private bool isPlaying;

    public bool IsReady => !isPlaying;

    public static FakeRewardedAd Create()
    {
        GameObject go = new GameObject("FakeRewardedAd");
        DontDestroyOnLoad(go);

        FakeRewardedAd ad = go.AddComponent<FakeRewardedAd>();
        ad.theme = UITheme.Load();
        ad.BuildOverlay();

        return ad;
    }

    public void Show(string rewardLabel, Action<bool> onFinished)
    {
        this.onFinished = onFinished;

        remaining = AdDuration;
        isPlaying = true;

        rewardText.text = string.IsNullOrEmpty(rewardLabel) ? "ÖDÜL" : rewardLabel;

        UpdateView();
        overlay.SetActive(true);
    }

    void Update()
    {
        if (!isPlaying)
            return;

        if (remaining > 0f)
        {
            remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        }

        UpdateView();
    }

    void UpdateView()
    {
        bool finished = remaining <= 0f;

        float progress = 1f - remaining / AdDuration;
        progressFill.anchorMax = new Vector2(progress, 1f);

        countdownText.text =
            finished
                ? "ÖDÜL KAZANILDI"
                : "ÖDÜLE " + Mathf.CeilToInt(remaining);

        countdownText.color = finished ? theme.mainRedHover : theme.greyLight;

        claimButton.interactable = finished;
        claimText.text = finished ? "ÖDÜLÜ AL" : "BEKLE...";
        claimText.color = finished ? theme.mainWhite : theme.greyLight;

        skipButton.gameObject.SetActive(!finished);
    }

    void Finish(bool rewarded)
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        overlay.SetActive(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        Action<bool> callback = onFinished;
        onFinished = null;
        callback?.Invoke(rewarded);
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildOverlay()
    {
        GameObject canvasObject = new GameObject(
            "FakeAdCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Tam ekran karartma: arkadaki her şeye tıklamayı engeller.
        RectTransform dim = CreateRect("Overlay", canvasObject.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
        overlay = dim.gameObject;

        // Ana panel
        RectTransform panel = CreateRect("Panel", dim);
        Center(panel, Vector2.zero, PanelSize);
        theme.ApplyPanel(panel.gameObject.AddComponent<Image>());

        // Başlık
        TextMeshProUGUI title = CreateText(panel, "REKLAM", 60f, theme.mainWhite);
        Center(title.rectTransform, new Vector2(0f, 340f), new Vector2(640f, 90f));

        TextMeshProUGUI subtitle = CreateText(panel, "ödülünü kazanmak için izle", 30f, theme.greyLight);
        Center(subtitle.rectTransform, new Vector2(0f, 280f), new Vector2(640f, 50f));

        // Reklam alanı (bildirim şeridi)
        RectTransform notice = CreateRect("Notice", panel);
        Center(notice, new Vector2(0f, 150f), new Vector2(600f, 107f));
        theme.ApplyNotice(notice.gameObject.AddComponent<Image>());

        TextMeshProUGUI adLabel = CreateText(notice, "TEST REKLAMI", 56f, theme.mainRedHover);
        Stretch(adLabel.rectTransform);

        TextMeshProUGUI note = CreateText(panel, "Burada gerçek bir ödüllü reklam oynatılacak.", 26f, theme.greyLight);
        Center(note.rectTransform, new Vector2(0f, 65f), new Vector2(640f, 40f));

        // Ödül
        TextMeshProUGUI rewardTitle = CreateText(panel, "ÖDÜLÜN", 28f, theme.greyLight);
        Center(rewardTitle.rectTransform, new Vector2(0f, -15f), new Vector2(640f, 40f));

        rewardText = CreateText(panel, string.Empty, 64f, theme.mainWhite);
        Center(rewardText.rectTransform, new Vector2(0f, -75f), new Vector2(640f, 80f));

        // İlerleme çubuğu
        RectTransform progressBack = CreateRect("ProgressBack", panel);
        Center(progressBack, new Vector2(0f, -160f), new Vector2(520f, 16f));
        progressBack.gameObject.AddComponent<Image>().color = theme.greyDark;

        progressFill = CreateRect("ProgressFill", progressBack);
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = new Vector2(0f, 1f);
        progressFill.offsetMin = Vector2.zero;
        progressFill.offsetMax = Vector2.zero;
        progressFill.gameObject.AddComponent<Image>().color = theme.mainRed;

        countdownText = CreateText(panel, string.Empty, 32f, theme.greyLight);
        Center(countdownText.rectTransform, new Vector2(0f, -205f), new Vector2(640f, 50f));

        // Ödülü al
        claimButton = CreateButton(panel, "ClaimButton", true, out claimText);
        Center((RectTransform)claimButton.transform, new Vector2(0f, -330f), new Vector2(440f, 120f));
        claimText.fontSize = 42f;
        claimButton.onClick.AddListener(() => Finish(true));

        // Atla (ödülsüz)
        skipButton = CreateButton(panel, "SkipButton", false, out TextMeshProUGUI skipText);
        Center((RectTransform)skipButton.transform, new Vector2(0f, -470f), new Vector2(300f, 80f));
        skipText.text = "GEÇ";
        skipText.fontSize = 30f;
        skipText.color = theme.greyLight;
        skipButton.onClick.AddListener(() => Finish(false));

        overlay.SetActive(false);
    }

    Button CreateButton(RectTransform parent, string name, bool red, out TextMeshProUGUI label)
    {
        RectTransform rect = CreateRect(name, parent);

        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        theme.ApplyButton(button, image, red);

        label = CreateText(rect, string.Empty, 36f, theme.mainWhite);
        Stretch(label.rectTransform);

        return button;
    }

    TextMeshProUGUI CreateText(RectTransform parent, string text, float size, Color color)
    {
        RectTransform rect = CreateRect("Text", parent);

        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        theme.ApplyText(tmp, color);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return tmp;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
