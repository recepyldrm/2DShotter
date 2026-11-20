using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game Over ekranındaki ödüllü reklam seçenekleri:
// - CONTINUE: reklam izle, canın bir kısmıyla devam et (oyun başına 1 kez)
// - 2x GOLD: reklam izle, bu oyunda kazanılan gold'u ikiye katla (oyun başına 1 kez)
//
// Butonları kendin tasarlamak istersen bu component'i sahneye ekleyip
// alanlara kendi butonlarını sürükle. Hiç eklemezsen GameManager otomatik
// ekler ve butonları Restart'ın altında oyunun temasıyla (UITheme) oluşturur.
public class GameOverAdOffers : MonoBehaviour
{
    private static string ContinueReward => Loc.Pick("DEVAM ET", "CONTINUE");
    private static string DoubleGoldReward => Loc.Pick("2x ALTIN", "2x GOLD");

    [Header("Opsiyonel: kendi butonların")]
    public Button continueButton;
    public Button doubleGoldButton;

    [Header("Otomatik butonlar")]
    [Tooltip("Otomatik oluşturulan butonların ekran ortasına göre konumu (1920x1080 referans).")]
    public Vector2 autoButtonsPosition = new Vector2(0f, -220f);

    private GameObject autoRoot;
    private bool initialized;

    void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        if (continueButton == null || doubleGoldButton == null)
        {
            BuildAutoButtons();
        }

        continueButton.onClick.AddListener(OnContinueClicked);
        doubleGoldButton.onClick.AddListener(OnDoubleGoldClicked);
    }

    // GameManager.GameOver çağırır.
    public void Show()
    {
        Initialize();
        Refresh();
    }

    void Refresh()
    {
        GameManager gameManager = GameManager.Instance;

        bool canRevive = gameManager != null && gameManager.CanRevive;
        bool canDouble = gameManager != null && gameManager.CanDoubleGold;

        continueButton.gameObject.SetActive(canRevive);
        doubleGoldButton.gameObject.SetActive(canDouble);

        bool adReady = AdsManager.IsRewardedReady;
        continueButton.interactable = adReady;
        doubleGoldButton.interactable = adReady;

        if (autoRoot != null)
        {
            autoRoot.SetActive(canRevive || canDouble);
        }
    }

    void Hide()
    {
        continueButton.gameObject.SetActive(false);
        doubleGoldButton.gameObject.SetActive(false);

        if (autoRoot != null)
        {
            autoRoot.SetActive(false);
        }
    }

    void OnContinueClicked()
    {
        PlayClick();

        AdsManager.ShowRewarded(ContinueReward, rewarded =>
        {
            GameAnalytics.AdWatched("game_over_continue", rewarded);

            if (rewarded && GameManager.Instance != null && GameManager.Instance.CanRevive)
            {
                Hide();
                GameManager.Instance.Revive();
                return;
            }

            Refresh();
        });

        Refresh();
    }

    void OnDoubleGoldClicked()
    {
        PlayClick();

        AdsManager.ShowRewarded(DoubleGoldReward, rewarded =>
        {
            GameAnalytics.AdWatched("game_over_double_gold", rewarded);

            if (rewarded && GameManager.Instance != null)
            {
                GameManager.Instance.DoubleRunGold();
            }

            Refresh();
        });

        Refresh();
    }

    void PlayClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    // ==================================
    // OTOMATİK BUTONLAR
    // ==================================

    void BuildAutoButtons()
    {
        UITheme theme = UITheme.Load();

        GameObject canvasObject = new GameObject(
            "GameOverAdCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        autoRoot = canvasObject;

        // Devam etmek ana teklif: kırmızı. 2x gold ikincil: gri.
        if (continueButton == null)
        {
            continueButton = CreateButton(
                theme,
                canvasObject.transform,
                "ContinueAdButton",
                ContinueReward,
                true,
                autoButtonsPosition + new Vector2(-240f, 0f)
            );
        }

        if (doubleGoldButton == null)
        {
            doubleGoldButton = CreateButton(
                theme,
                canvasObject.transform,
                "DoubleGoldAdButton",
                DoubleGoldReward,
                false,
                autoButtonsPosition + new Vector2(240f, 0f)
            );
        }
    }

    static Button CreateButton(UITheme theme, Transform parent, string name, string label, bool red, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(440f, 130f);

        Image image = buttonObject.AddComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        theme.ApplyButton(button, image, red);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        theme.ApplyText(text, theme.mainWhite);

        // Alt satır: küçük, soluk "watch ad" notu
        string subColor = ColorUtility.ToHtmlStringRGB(theme.greyLight);
        text.text = label + "\n<size=24><color=#" + subColor + ">" + Loc.Pick("reklam izle", "watch ad") + "</color></size>";
        text.fontSize = 44f;
        text.lineSpacing = -10f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }
}
