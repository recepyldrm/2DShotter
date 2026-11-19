using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// İlk açılış eğitimi: oyunu, kart sistemini ve shop'u kısaca tanıtan sayfalar.
// İlk açılışta otomatik gösterilir; ana menüdeki "NASIL OYNANIR" ile tekrar açılır.
public class TutorialUI : MonoBehaviour
{
    // Diğer ana menü ekranları (örn. günlük ödül) eğitim bitene kadar bekler.
    public static bool IsShowing { get; private set; }

    public static bool HasSeen =>
        PlayerPrefs.GetInt(GameKeys.TutorialSeen, 0) == 1;

    private struct Page
    {
        public string title;
        public string body;

        public Page(string title, string body)
        {
            this.title = title;
            this.body = body;
        }
    }

    private static readonly Page[] Pages =
    {
        new Page(
            "HOŞ GELDİN, ASKER",
            "Askerin otomatik olarak ateş eder.\n\nDüşmanlar bitmeyen dalgalar hâlinde gelir ve zamanla güçlenir. Olabildiğince uzun hayatta kal ve yeni bir rekor kır."
        ),
        new Page(
            "SEVİYE KARTLARI",
            "Düşman öldürerek tecrübe puanı kazan. Her seviye atladığında 3 geliştirme kartından birini seçersin.\n\nNADİR, EPİK ve EFSANEVİ kartlar daha güçlüdür. Bir kartı beğenmedin mi? YENİLE ile değiştir."
        ),
        new Page(
            "ALTIN",
            "Düşmanlar altın düşürür. Oyun sırasında altını kart yenilemeye ve geliştirmelere harcayabilirsin.\n\nBir oyunda kazandığın tüm altınlar MAĞAZA için kasana da eklenir."
        ),
        new Page(
            "MAĞAZA VE DOST",
            "Drone dostun seni takip eder ve hasarını artırır.\n\nBiriktirdiğin altınlarla MAĞAZA'dan ona yeni görünümler al."
        ),
        new Page(
            "HER GÜN GEL",
            "Her gün GÜNLÜK ÖDÜL'ünü al. 7. gün büyük bir ödül seni bekliyor!\n\nBAŞARIMLAR'ı tamamlayarak altın kazan ve özel görünümlerin kilidini aç."
        ),
    };

    private UITheme theme;
    private GameObject panel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private Button backButton;
    private Button nextButton;
    private TextMeshProUGUI nextText;
    private readonly List<Image> dots = new List<Image>();

    private int pageIndex;

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "TutorialCanvas", 300);

        Button openButton = UIBuilder.CreateMenuButton(theme, canvas, "NASIL OYNANIR", UIBuilder.MenuSlotTutorial, out _);
        openButton.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        BuildPanel(canvas);

        if (!HasSeen)
        {
            Show();
        }
    }

    void OnDestroy()
    {
        if (panel != null && panel.activeSelf)
        {
            IsShowing = false;
        }
    }

    public void Show()
    {
        pageIndex = 0;
        IsShowing = true;
        panel.SetActive(true);
        UpdatePage();
    }

    void Close()
    {
        UIBuilder.PlayClick();

        PlayerPrefs.SetInt(GameKeys.TutorialSeen, 1);
        PlayerPrefs.Save();

        panel.SetActive(false);
        IsShowing = false;
    }

    void Next()
    {
        UIBuilder.PlayClick();

        if (pageIndex >= Pages.Length - 1)
        {
            Close();
            return;
        }

        pageIndex++;
        UpdatePage();
    }

    void Back()
    {
        UIBuilder.PlayClick();

        if (pageIndex <= 0)
            return;

        pageIndex--;
        UpdatePage();
    }

    void UpdatePage()
    {
        Page page = Pages[pageIndex];

        titleText.text = page.title;
        bodyText.text = page.body;

        bool isLast = pageIndex == Pages.Length - 1;
        nextText.text = isLast ? "BAŞLA" : "İLERİ";

        backButton.gameObject.SetActive(pageIndex > 0);

        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i == pageIndex ? theme.mainRedHover : theme.greyDark;
        }
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "TutorialPanel", 0.9f);
        panel = dim.gameObject;

        RectTransform safeRoot = UIBuilder.CreateSafeRoot(dim);

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            safeRoot,
            new Vector2(0.22f, 0.1f),
            new Vector2(0.78f, 0.9f)
        );

        // Atla
        Button skipButton = UIBuilder.CreateButton(theme, window, "SkipButton", "GEÇ", false, out TextMeshProUGUI skipText);
        UIBuilder.Place((RectTransform)skipButton.transform, new Vector2(1f, 1f), new Vector2(-45f, -40f), new Vector2(200f, 70f));
        skipText.color = theme.greyLight;
        skipButton.onClick.AddListener(Close);

        // Sayfa başlığı
        titleText = UIBuilder.CreateText(theme, window, string.Empty, 64f, TextAlignmentOptions.Center, theme.mainRedHover);
        UIBuilder.AnchorTop(titleText.rectTransform, 120f, 100f);
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 36f;
        titleText.fontSizeMax = 64f;
        titleText.margin = new Vector4(60f, 0f, 60f, 0f);

        // İnce kırmızı çizgi
        RectTransform divider = UIBuilder.CreateRect("Divider", window);
        divider.anchorMin = new Vector2(0.2f, 1f);
        divider.anchorMax = new Vector2(0.8f, 1f);
        divider.pivot = new Vector2(0.5f, 1f);
        divider.offsetMin = new Vector2(0f, -233f);
        divider.offsetMax = new Vector2(0f, -230f);
        Image line = divider.gameObject.AddComponent<Image>();
        line.color = theme.mainRed;
        line.raycastTarget = false;

        // Açıklama
        bodyText = UIBuilder.CreateText(theme, window, string.Empty, 40f, TextAlignmentOptions.Top, theme.mainWhite);
        UIBuilder.Stretch(bodyText.rectTransform);
        bodyText.rectTransform.offsetMin = new Vector2(90f, 230f);
        bodyText.rectTransform.offsetMax = new Vector2(-90f, -270f);
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 24f;
        bodyText.fontSizeMax = 40f;
        bodyText.lineSpacing = 10f;

        // Sayfa noktaları
        RectTransform dotRow = UIBuilder.CreateRect("Dots", window);
        UIBuilder.Place(dotRow, new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(Pages.Length * 40f, 18f));

        HorizontalLayoutGroup dotLayout = dotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        dotLayout.spacing = 22f;
        dotLayout.childAlignment = TextAnchor.MiddleCenter;
        dotLayout.childControlWidth = false;
        dotLayout.childControlHeight = false;

        for (int i = 0; i < Pages.Length; i++)
        {
            RectTransform dot = UIBuilder.CreateRect("Dot", dotRow);
            dot.sizeDelta = new Vector2(18f, 18f);

            Image dotImage = dot.gameObject.AddComponent<Image>();
            dotImage.raycastTarget = false;
            dots.Add(dotImage);
        }

        // Geri / İleri
        backButton = UIBuilder.CreateButton(theme, window, "BackButton", "GERİ", false, out _);
        UIBuilder.Place((RectTransform)backButton.transform, new Vector2(0f, 0f), new Vector2(70f, 50f), new Vector2(280f, 100f));
        backButton.onClick.AddListener(Back);

        nextButton = UIBuilder.CreateButton(theme, window, "NextButton", "İLERİ", true, out nextText);
        UIBuilder.Place((RectTransform)nextButton.transform, new Vector2(1f, 0f), new Vector2(-70f, 50f), new Vector2(280f, 100f));
        nextButton.onClick.AddListener(Next);

        panel.SetActive(false);
    }
}
