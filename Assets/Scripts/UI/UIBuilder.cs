using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Kodla oluşturulan ekranlar (eğitim, günlük ödül, başarımlar) için
// oyunun temasına (UITheme) uygun ortak arayüz parçaları.
public static class UIBuilder
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    // Ana menünün sağ kenarındaki buton sütunu, yukarıdan aşağı sıra
    public const int MenuSlotShop = 0;
    public const int MenuSlotWeapons = 1;
    public const int MenuSlotUpgrades = 2;
    public const int MenuSlotAchievements = 3;
    public const int MenuSlotDaily = 4;
    public const int MenuSlotQuests = 5;
    public const int MenuSlotTutorial = 6;

    // En altta: Google Play Games bağlanana kadar görünmez, boşluk kalmasın.
    public const int MenuSlotLeaderboard = 7;

    public const float MenuButtonTop = 40f;
    public const float MenuButtonSpacing = 110f;
    public static readonly Vector2 MenuButtonSize = new Vector2(280f, 95f);

    public static Transform CreateCanvas(Transform parent, string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.transform;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        Place(rect, new Vector2(0.5f, 0.5f), position, size);
    }

    public static void AnchorTop(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -top - height);
        rect.offsetMax = new Vector2(0f, -top);
    }

    public static RectTransform CreateSafeRoot(Transform parent)
    {
        RectTransform safeRoot = CreateRect("SafeArea", parent);
        Stretch(safeRoot);
        safeRoot.gameObject.AddComponent<SafeArea>();
        return safeRoot;
    }

    // Tam ekran karartma: arkadaki her şeye tıklamayı engeller.
    public static RectTransform CreateDim(Transform parent, string name, float alpha = 0.85f)
    {
        RectTransform dim = CreateRect(name, parent);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, alpha);
        return dim;
    }

    // Dokulu zemin + kırmızı çerçeveli pencere (ekran oranına göre)
    public static RectTransform CreateWindow(UITheme theme, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform window = CreateRect("Window", parent);
        window.anchorMin = anchorMin;
        window.anchorMax = anchorMax;
        window.offsetMin = Vector2.zero;
        window.offsetMax = Vector2.zero;
        theme.ApplyWindow(window.gameObject.AddComponent<Image>());
        return window;
    }

    public static TextMeshProUGUI CreateText(UITheme theme, Transform parent, string text, float size, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect("Text", parent);

        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        theme.ApplyText(tmp, color);
        // İngilizce seçiliyse sözlükte karşılığı olan metinler çevrilir.
        tmp.text = Loc.T(text);
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        return tmp;
    }

    public static Button CreateButton(UITheme theme, Transform parent, string name, string label, bool red, out TextMeshProUGUI text)
    {
        RectTransform rect = CreateRect(name, parent);

        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        theme.ApplyButton(button, image, red);

        text = CreateText(theme, rect, label, 36f, TextAlignmentOptions.Center, theme.mainWhite);
        Stretch(text.rectTransform);
        text.margin = new Vector4(20f, 0f, 20f, 0f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 40f;

        return button;
    }

    // Başlık + altında ince kırmızı çizgi
    public static TextMeshProUGUI CreateWindowTitle(UITheme theme, RectTransform window, string title)
    {
        TextMeshProUGUI text = CreateText(theme, window, title, 68f, TextAlignmentOptions.Center, theme.mainWhite);
        AnchorTop(text.rectTransform, 25f, 100f);

        RectTransform divider = CreateRect("Divider", window);
        divider.anchorMin = new Vector2(0.06f, 1f);
        divider.anchorMax = new Vector2(0.94f, 1f);
        divider.pivot = new Vector2(0.5f, 1f);
        divider.offsetMin = new Vector2(0f, -137f);
        divider.offsetMax = new Vector2(0f, -134f);

        Image line = divider.gameObject.AddComponent<Image>();
        line.color = theme.mainRed;
        line.raycastTarget = false;

        return text;
    }

    public static Button CreateWindowCloseButton(UITheme theme, RectTransform window)
    {
        Button close = theme.CreateCloseButton(window);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-60f, -45f), new Vector2(60f, 60f));
        return close;
    }

    public static RectTransform CreateScrollList(Transform parent, Vector2 offsetMin, Vector2 offsetMax, float spacing = 14f)
    {
        RectTransform scrollRect = CreateRect("Scroll", parent);
        Stretch(scrollRect);
        scrollRect.offsetMin = offsetMin;
        scrollRect.offsetMax = offsetMax;

        ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = CreateRect("Viewport", scrollRect);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        // Boş alanda da sürükleyerek kaydırılabilsin diye görünmez bir Image.
        viewport.gameObject.AddComponent<Image>().color = Color.clear;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;

        return content;
    }

    // Ana menünün sağ kenarındaki sütuna buton ekler. slot: 0 = SHOP, 1 = altı...
    public static Button CreateMenuButton(UITheme theme, Transform canvas, string label, int slot, out RectTransform badge)
    {
        RectTransform safeRoot = CreateSafeRoot(canvas);

        Button button = CreateButton(theme, safeRoot, label + "Button", label, true, out _);

        Place(
            (RectTransform)button.transform,
            new Vector2(1f, 1f),
            new Vector2(-40f, -MenuButtonTop - slot * MenuButtonSpacing),
            MenuButtonSize
        );

        badge = CreateBadge(theme, (RectTransform)button.transform);

        return button;
    }

    // Butonun köşesindeki "!" bildirim rozeti (alınabilir ödül var)
    public static RectTransform CreateBadge(UITheme theme, RectTransform parent)
    {
        RectTransform badge = CreateRect("Badge", parent);
        Place(badge, new Vector2(0f, 1f), new Vector2(-10f, 10f), new Vector2(44f, 44f));

        Image image = badge.gameObject.AddComponent<Image>();
        image.color = theme.mainRedHover;
        image.raycastTarget = false;

        TextMeshProUGUI mark = CreateText(theme, badge, "!", 34f, TextAlignmentOptions.Center, Color.white);
        Stretch(mark.rectTransform);

        badge.gameObject.SetActive(false);
        return badge;
    }

    // Temaya uygun kaydırıcı (0-1). Koyu gri yol, kırmızı dolgu, açık renkli tutamak.
    public static Slider CreateSlider(UITheme theme, Transform parent, float value)
    {
        RectTransform root = CreateRect("Slider", parent);

        RectTransform background = CreateRect("Background", root);
        background.anchorMin = new Vector2(0f, 0.35f);
        background.anchorMax = new Vector2(1f, 0.65f);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        background.gameObject.AddComponent<Image>().color = theme.greyDark;

        RectTransform fillArea = CreateRect("FillArea", root);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        RectTransform fill = CreateRect("Fill", fillArea);
        fill.sizeDelta = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = theme.mainRed;

        RectTransform handleArea = CreateRect("HandleArea", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(20f, 0f);
        handleArea.offsetMax = new Vector2(-20f, 0f);

        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(40f, 0f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = theme.greyLight;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));

        ColorBlock colors = slider.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = theme.mainRedHover;
        slider.colors = colors;

        return slider;
    }

    // Arka planı koyu gri, dolgu kırmızı ilerleme çubuğu. Dolgu: anchorMax.x ile ayarlanır.
    public static RectTransform CreateProgressBar(UITheme theme, Transform parent, out RectTransform fill)
    {
        RectTransform back = CreateRect("ProgressBar", parent);
        back.gameObject.AddComponent<Image>().color = theme.greyDark;

        fill = CreateRect("Fill", back);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = theme.mainRed;

        return back;
    }

    public static string ColorTag(Color color, string text)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }

    public static void PlayClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }
}
