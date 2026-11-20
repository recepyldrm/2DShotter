using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki dükkan.
// Arayüzü oyunun temasıyla (UITheme) kendisi oluşturur:
// sağ üstte "MAĞAZA" butonu ve açılır panel.
// - Gold paketleri: gerçek parayla kalıcı gold (IAPManager).
// - Dost skinleri: kalıcı gold ile ya da premium olanlar gerçek parayla.
// Skinler Assets/Resources/CompanionSkinDatabase'den okunur.
public class SkinShopUI : MonoBehaviour
{
    [Header("Opsiyonel")]
    [Tooltip("Kendi butonunu kullanmak istersen buraya sürükle. Boşsa sağ üste otomatik buton eklenir.")]
    public Button openButton;

    [Tooltip("Sprite'ı boş olan skinlerin (örn. varsayılan) önizlemesi. Dostun sahnedeki sprite'ı.")]
    public Sprite defaultPreviewSprite;

    [Tooltip("Boşsa temanın (UITheme) fontu kullanılır.")]
    public TMP_FontAsset font;

    [Header("Görünüm")]
    public Color dimColor = new Color(0f, 0f, 0f, 0.85f);

    private const string LoadingPrice = "...";
    private const float StatusDuration = 3f;

    private class ActionButton
    {
        public Button button;
        public Image image;
        public TextMeshProUGUI text;
    }

    private class SkinRow
    {
        public CompanionSkinDatabase.Skin skin;
        public Image preview;
        public ActionButton action;
    }

    private class GoldPackRow
    {
        public StoreCatalog.GoldPack pack;
        public ActionButton action;
    }

    private readonly List<SkinRow> skinRows = new List<SkinRow>();
    private readonly List<GoldPackRow> goldPackRows = new List<GoldPackRow>();

    private UITheme theme;
    private CompanionSkinDatabase database;
    private GameObject panel;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI statusLabel;
    private float statusHideTime;

    private IAPManager subscribedIap;

    // ==================================
    // START
    // ==================================

    void Start()
    {
        theme = UITheme.Load();
        database = CompanionSkinDatabase.Load();

        Transform canvas = CreateCanvas();

        if (openButton == null)
        {
            openButton = CreateOpenButton(canvas);
        }

        openButton.onClick.AddListener(Open);

        BuildPanel(canvas);
        panel.SetActive(false);

        PlayerProgress.OnTotalGoldChanged += OnTotalGoldChanged;

        EnsureIapSubscribed();
    }

    void OnDestroy()
    {
        PlayerProgress.OnTotalGoldChanged -= OnTotalGoldChanged;

        if (subscribedIap != null)
        {
            subscribedIap.OnProductsReady -= Refresh;
            subscribedIap.OnPurchaseSucceeded -= OnIapPurchaseSucceeded;
            subscribedIap.OnPurchaseFailed -= OnIapPurchaseFailed;
        }
    }

    // IAPManager oyun açılışında kendiliğinden oluşur; bu script'ten
    // sonra hazır olabileceği için abonelik gerektiğinde tekrar denenir.
    void EnsureIapSubscribed()
    {
        if (subscribedIap != null || IAPManager.Instance == null)
            return;

        subscribedIap = IAPManager.Instance;
        subscribedIap.OnProductsReady += Refresh;
        subscribedIap.OnPurchaseSucceeded += OnIapPurchaseSucceeded;
        subscribedIap.OnPurchaseFailed += OnIapPurchaseFailed;
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        // Panel açıkken animasyonlu skinlerin önizlemesini oynat.
        float time = Time.unscaledTime;

        foreach (SkinRow row in skinRows)
        {
            if (!row.skin.IsAnimated)
                continue;

            Sprite frame = row.skin.GetFrame(time);

            if (frame != null)
            {
                row.preview.sprite = frame;
            }
        }

        if (statusLabel != null &&
            statusLabel.gameObject.activeSelf &&
            Time.unscaledTime >= statusHideTime)
        {
            statusLabel.gameObject.SetActive(false);
        }
    }

    void OnTotalGoldChanged(int total)
    {
        if (panel != null && panel.activeSelf)
        {
            Refresh();
        }
    }

    // ==================================
    // AÇ / KAPAT
    // ==================================

    public void Open()
    {
        EnsureIapSubscribed();

        PlayClick();
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        PlayClick();
        panel.SetActive(false);
    }

    // ==================================
    // SATIN AL / SEÇ
    // ==================================

    void OnSkinClicked(CompanionSkinDatabase.Skin skin)
    {
        if (database == null || skin == null)
            return;

        if (database.IsOwned(skin))
        {
            PlayClick();
            PlayerProgress.SelectedCompanionSkin = skin.id;
            Refresh();
            return;
        }

        // Başarım ödülü: satın alınamaz.
        if (skin.unlockedByAchievement)
            return;

        if (skin.IsPremium)
        {
            PlayClick();
            PurchaseWithRealMoney(skin.iapProductId);
            return;
        }

        if (!PlayerProgress.TrySpendGold(skin.price))
            return;

        PlayerProgress.UnlockCompanionSkin(skin.id);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        // Satın alınan skin otomatik seçilir.
        PlayerProgress.SelectedCompanionSkin = skin.id;

        Refresh();
    }

    void OnGoldPackClicked(StoreCatalog.GoldPack pack)
    {
        PlayClick();
        PurchaseWithRealMoney(pack.productId);
    }

    void PurchaseWithRealMoney(string productId)
    {
        EnsureIapSubscribed();

        if (IAPManager.Instance == null)
        {
            ShowStatus("MAĞAZA HAZIR DEĞİL");
            return;
        }

        IAPManager.Instance.Purchase(productId);
    }

    void OnIapPurchaseSucceeded(string productId)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        // Premium skin alındıysa otomatik seç.
        CompanionSkinDatabase.Skin skin =
            database != null ? database.FindByProductId(productId) : null;

        if (skin != null)
        {
            PlayerProgress.SelectedCompanionSkin = skin.id;
        }

        ShowStatus("TEŞEKKÜRLER!");
        Refresh();
    }

    void OnIapPurchaseFailed(string productId, string message)
    {
        ShowStatus(message);
        Refresh();
    }

    void RestorePurchases()
    {
        PlayClick();
        EnsureIapSubscribed();

        if (IAPManager.Instance == null)
        {
            ShowStatus("MAĞAZA HAZIR DEĞİL");
            return;
        }

        ShowStatus("GERİ YÜKLENİYOR...");

        IAPManager.Instance.RestorePurchases(success =>
        {
            ShowStatus(success ? "SATIN ALIMLAR GERİ YÜKLENDİ" : "GERİ YÜKLEME BAŞARISIZ");
            Refresh();
        });
    }

    // ==================================
    // GÖRÜNÜM GÜNCELLEME
    // ==================================

    private enum ButtonStyle
    {
        Buy,        // kırmızı, tıklanabilir
        Select,     // gri, tıklanabilir
        Selected,   // pasif, kırmızı yazı
        Locked      // pasif, soluk yazı
    }

    void Refresh()
    {
        if (theme == null)
            return;

        int totalGold = PlayerProgress.TotalGold;

        if (goldLabel != null)
        {
            goldLabel.text = ColorTag(theme.mainRedHover, Loc.T("ALTIN")) + "  " + totalGold;
        }

        bool storeReady = IAPManager.Instance != null && IAPManager.Instance.IsReady;

        foreach (GoldPackRow row in goldPackRows)
        {
            SetAction(
                row.action,
                GetPriceLabel(row.pack.productId, storeReady),
                storeReady ? ButtonStyle.Buy : ButtonStyle.Locked
            );
        }

        if (database == null)
            return;

        CompanionSkinDatabase.Skin selected = database.GetSelectedSkin();

        foreach (SkinRow row in skinRows)
        {
            if (row.skin == selected)
            {
                SetAction(row.action, "SEÇİLİ", ButtonStyle.Selected);
            }
            else if (database.IsOwned(row.skin))
            {
                SetAction(row.action, "SEÇ", ButtonStyle.Select);
            }
            else if (row.skin.unlockedByAchievement)
            {
                SetAction(row.action, "KİLİTLİ", ButtonStyle.Locked);
            }
            else if (row.skin.IsPremium)
            {
                SetAction(
                    row.action,
                    GetPriceLabel(row.skin.iapProductId, storeReady),
                    storeReady ? ButtonStyle.Buy : ButtonStyle.Locked
                );
            }
            else
            {
                SetAction(
                    row.action,
                    row.skin.price + " " + Loc.T("ALTIN"),
                    totalGold >= row.skin.price ? ButtonStyle.Buy : ButtonStyle.Locked
                );
            }
        }
    }

    void SetAction(ActionButton action, string label, ButtonStyle style)
    {
        bool red = style == ButtonStyle.Buy || style == ButtonStyle.Locked;

        theme.ApplyButton(action.button, action.image, red);

        action.button.interactable =
            style == ButtonStyle.Buy || style == ButtonStyle.Select;

        action.text.text = Loc.T(label);

        switch (style)
        {
            case ButtonStyle.Selected:
                action.text.color = theme.mainRedHover;
                break;

            case ButtonStyle.Locked:
                action.text.color = theme.greyLight;
                break;

            default:
                action.text.color = theme.mainWhite;
                break;
        }
    }

    static string GetPriceLabel(string productId, bool storeReady)
    {
        if (!storeReady)
            return LoadingPrice;

        string price = IAPManager.Instance.GetPriceString(productId);

        if (string.IsNullOrEmpty(price))
            return Loc.T("SATIN AL");

        // Fontta ₺ işareti yok: "₺49,99" -> "49,99 TL"
        if (price.Contains("₺"))
        {
            price = price.Replace("₺", string.Empty).Trim() + " TL";
        }

        return price;
    }

    static string ColorTag(Color color, string text)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }

    void ShowStatus(string message)
    {
        if (statusLabel == null || string.IsNullOrEmpty(message))
            return;

        statusLabel.text = Loc.T(message);
        statusLabel.gameObject.SetActive(true);
        statusHideTime = Time.unscaledTime + StatusDuration;
    }

    void PlayClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    // ==================================
    // ARAYÜZ OLUŞTURMA
    // ==================================

    Transform CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "ShopCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.transform;
    }

    Button CreateOpenButton(Transform canvas)
    {
        RectTransform safeRoot = CreateSafeRoot("OpenButtonSafeArea", canvas);

        ActionButton open = CreateActionButton(safeRoot, "ShopButton", true);
        open.text.text = Loc.T("MAĞAZA");
        open.text.fontSize = 44f;

        RectTransform rect = (RectTransform)open.button.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
        rect.sizeDelta = new Vector2(280f, 100f);

        return open.button;
    }

    void BuildPanel(Transform canvas)
    {
        // Tam ekran karartma: arkadaki menüye tıklanmasını da engeller.
        RectTransform dim = CreateRect("ShopPanel", canvas);
        Stretch(dim);
        AddImage(dim, dimColor);
        panel = dim.gameObject;

        RectTransform safeRoot = CreateSafeRoot("SafeArea", dim);

        // Pencere: dokulu zemin + kırmızı çerçeve
        RectTransform window = CreateRect("Window", safeRoot);
        window.anchorMin = new Vector2(0.15f, 0.06f);
        window.anchorMax = new Vector2(0.85f, 0.94f);
        window.offsetMin = Vector2.zero;
        window.offsetMax = Vector2.zero;
        theme.ApplyWindow(window.gameObject.AddComponent<Image>());

        // Başlık
        TextMeshProUGUI title = CreateText("Title", window, "MAĞAZA", 72f, TextAlignmentOptions.Center, theme.mainWhite);
        AnchorTop(title.rectTransform, 25f, 100f);

        // Başlığın altındaki ince kırmızı çizgi
        RectTransform divider = CreateRect("Divider", window);
        divider.anchorMin = new Vector2(0.06f, 1f);
        divider.anchorMax = new Vector2(0.94f, 1f);
        divider.pivot = new Vector2(0.5f, 1f);
        divider.offsetMin = new Vector2(0f, -137f);
        divider.offsetMax = new Vector2(0f, -134f);
        AddImage(divider, theme.mainRed).raycastTarget = false;

        // Kalıcı gold
        goldLabel = CreateText("Gold", window, string.Empty, 40f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        AnchorTop(goldLabel.rectTransform, 25f, 100f);
        goldLabel.rectTransform.offsetMin = new Vector2(70f, goldLabel.rectTransform.offsetMin.y);

        // Kapat
        Button closeButton = theme.CreateCloseButton(window);
        RectTransform closeRect = (RectTransform)closeButton.transform;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-60f, -45f);
        closeRect.sizeDelta = new Vector2(60f, 60f);
        closeButton.onClick.AddListener(Close);

        // iOS'ta "Restore Purchases" butonu zorunlu.
        // Android'de satın alımlar her açılışta otomatik geri yüklenir.
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            ActionButton restore = CreateActionButton(window, "RestoreButton", false);
            restore.text.text = Loc.T("GERİ YÜKLE");
            restore.text.fontSize = 28f;

            RectTransform restoreRect = (RectTransform)restore.button.transform;
            restoreRect.anchorMin = new Vector2(1f, 1f);
            restoreRect.anchorMax = new Vector2(1f, 1f);
            restoreRect.pivot = new Vector2(1f, 1f);
            restoreRect.anchoredPosition = new Vector2(-150f, -40f);
            restoreRect.sizeDelta = new Vector2(220f, 70f);
            restore.button.onClick.AddListener(RestorePurchases);
        }

        // Durum mesajı (satın alma iptal edildi, teşekkürler...)
        statusLabel = CreateText("Status", window, string.Empty, 34f, TextAlignmentOptions.Center, theme.mainRedHover);
        statusLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        statusLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
        statusLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
        statusLabel.rectTransform.offsetMin = new Vector2(0f, 30f);
        statusLabel.rectTransform.offsetMax = new Vector2(0f, 85f);
        statusLabel.gameObject.SetActive(false);

        // Kaydırılabilir liste
        RectTransform content = CreateScrollList(window);

        // Gold paketleri
        if (StoreCatalog.GoldPacks.Length > 0)
        {
            CreateSectionHeader(content, "ALTIN PAKETLERİ");

            foreach (StoreCatalog.GoldPack pack in StoreCatalog.GoldPacks)
            {
                CreateGoldPackRow(content, pack);
            }
        }

        // Dost skinleri
        CreateSectionHeader(content, "DOST GÖRÜNÜMLERİ");

        if (database == null || database.skins.Count == 0)
        {
            TextMeshProUGUI empty = CreateText("Empty", content, "HENÜZ GÖRÜNÜM YOK", 40f, TextAlignmentOptions.Center, theme.greyLight);
            empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
            return;
        }

        HashSet<string> usedIds = new HashSet<string>();

        foreach (CompanionSkinDatabase.Skin skin in database.skins)
        {
            if (skin == null)
                continue;

            if (!database.IsDefault(skin) && string.IsNullOrEmpty(skin.id))
            {
                Debug.LogWarning("SkinShopUI: Id'si boş skin atlandı: " + skin.displayName);
                continue;
            }

            if (!string.IsNullOrEmpty(skin.id) && !usedIds.Add(skin.id))
            {
                Debug.LogWarning("SkinShopUI: Aynı Id iki kez kullanılmış, ikincisi atlandı: " + skin.id);
                continue;
            }

            CreateSkinRow(content, skin);
        }
    }

    RectTransform CreateScrollList(RectTransform window)
    {
        RectTransform scrollRect = CreateRect("Scroll", window);
        Stretch(scrollRect);
        scrollRect.offsetMin = new Vector2(60f, 95f);
        scrollRect.offsetMax = new Vector2(-60f, -150f);

        ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = CreateRect("Viewport", scrollRect);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        // Boş alanda da sürükleyerek kaydırılabilsin diye görünmez bir Image.
        AddImage(viewport, Color.clear);

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
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

    void CreateSectionHeader(RectTransform content, string label)
    {
        TextMeshProUGUI header = CreateText("Header_" + label, content, label, 38f, TextAlignmentOptions.BottomLeft, theme.mainRedHover);
        header.margin = new Vector4(10f, 0f, 0f, 6f);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 75f;
    }

    RectTransform CreateRowBackground(RectTransform content, string name, float height)
    {
        RectTransform rowRect = CreateRect(name, content);
        theme.ApplyNotice(rowRect.gameObject.AddComponent<Image>());
        rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return rowRect;
    }

    void CreateGoldPackRow(RectTransform content, StoreCatalog.GoldPack pack)
    {
        RectTransform rowRect = CreateRowBackground(content, "Pack_" + pack.productId, 130f);

        string label = "+" + pack.gold + " " + ColorTag(theme.mainRedHover, Loc.T("ALTIN"));

        TextMeshProUGUI amountText = CreateText("Amount", rowRect, label, 48f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        Stretch(amountText.rectTransform);
        amountText.rectTransform.offsetMin = new Vector2(60f, 0f);
        amountText.rectTransform.offsetMax = new Vector2(-320f, 0f);

        ActionButton action = CreateRowButton(rowRect);
        action.button.onClick.AddListener(() => OnGoldPackClicked(pack));

        goldPackRows.Add(new GoldPackRow
        {
            pack = pack,
            action = action
        });
    }

    void CreateSkinRow(RectTransform content, CompanionSkinDatabase.Skin skin)
    {
        RectTransform rowRect = CreateRowBackground(content, "Skin_" + skin.id, 160f);

        // Önizleme
        RectTransform previewRect = CreateRect("Preview", rowRect);
        previewRect.anchorMin = new Vector2(0f, 0.5f);
        previewRect.anchorMax = new Vector2(0f, 0.5f);
        previewRect.pivot = new Vector2(0f, 0.5f);
        previewRect.anchoredPosition = new Vector2(45f, 0f);
        previewRect.sizeDelta = new Vector2(110f, 110f);

        Sprite previewSprite = skin.FirstSprite != null ? skin.FirstSprite : defaultPreviewSprite;

        Image preview = AddImage(previewRect, skin.tint);
        preview.sprite = previewSprite;
        preview.preserveAspect = true;
        preview.raycastTarget = false;
        preview.enabled = previewSprite != null;

        if (skin.flipX)
        {
            previewRect.localScale = new Vector3(-1f, 1f, 1f);
        }

        // İsim
        string displayName = string.IsNullOrEmpty(skin.displayName) ? skin.id : Loc.T(skin.displayName);

        if (skin.unlockedByAchievement)
        {
            // Hangi başarımla açıldığını göster.
            AchievementCatalog.Achievement achievement = AchievementCatalog.FindBySkinReward(skin.id);
            // Türkçe büyük harf: "i" -> "İ"
            string hint = achievement != null
                ? (Loc.IsEnglish ? Loc.T(achievement.description).ToUpperInvariant() : achievement.description.ToUpper(new System.Globalization.CultureInfo("tr-TR")))
                : skin.id == SeasonPass.RewardSkinId ? Loc.Pick("SEZON BİLETİ ÖDÜLÜ", "SEASON PASS REWARD") : Loc.Pick("BAŞARIM ÖDÜLÜ", "ACHIEVEMENT REWARD");

            displayName += "\n<size=24>" + ColorTag(theme.greyLight, hint) + "</size>";
        }
        else if (skin.IsPremium)
        {
            displayName += "\n<size=26>" + ColorTag(theme.mainRedHover, "PREMIUM") + "</size>";
        }

        // Drone yeteneği
        displayName += "\n<size=24>" + ColorTag(new Color(0.55f, 0.85f, 1f, 1f), CompanionAbilities.Describe(CompanionAbilities.For(skin.id))) + "</size>";

        TextMeshProUGUI nameText = CreateText("Name", rowRect, displayName, 42f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 20f;
        nameText.fontSizeMax = 42f;
        Stretch(nameText.rectTransform);
        nameText.rectTransform.offsetMin = new Vector2(180f, 0f);
        nameText.rectTransform.offsetMax = new Vector2(-320f, 0f);
        nameText.lineSpacing = -15f;

        // Satın al / seç butonu
        ActionButton action = CreateRowButton(rowRect);
        action.button.onClick.AddListener(() => OnSkinClicked(skin));

        skinRows.Add(new SkinRow
        {
            skin = skin,
            preview = preview,
            action = action
        });
    }

    ActionButton CreateRowButton(RectTransform rowRect)
    {
        ActionButton action = CreateActionButton(rowRect, "ActionButton", true);

        RectTransform buttonRect = (RectTransform)action.button.transform;
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-40f, 0f);
        buttonRect.sizeDelta = new Vector2(270f, 95f);

        return action;
    }

    ActionButton CreateActionButton(Transform parent, string name, bool red)
    {
        RectTransform rect = CreateRect(name, parent);

        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        theme.ApplyButton(button, image, red);

        TextMeshProUGUI text = CreateText("Label", rect, string.Empty, 34f, TextAlignmentOptions.Center, theme.mainWhite);
        Stretch(text.rectTransform);

        return new ActionButton
        {
            button = button,
            image = image,
            text = text
        };
    }

    // ==================================
    // YARDIMCILAR
    // ==================================

    RectTransform CreateSafeRoot(string name, Transform parent)
    {
        RectTransform safeRoot = CreateRect(name, parent);
        Stretch(safeRoot);
        safeRoot.gameObject.AddComponent<SafeArea>();
        return safeRoot;
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

    static void AnchorTop(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -top - height);
        rect.offsetMax = new Vector2(0f, -top);
    }

    static Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();

        theme.ApplyText(tmp, color);

        // Inspector'da özel font verildiyse temanınkini ez.
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = Loc.T(text);
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        return tmp;
    }
}
