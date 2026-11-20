using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelUpCardManager : MonoBehaviour
{
    public static LevelUpCardManager Instance;

    private const int CardCount = 3;

    // ==================================
    // CARD UI
    // ==================================

    [System.Serializable]
    public class CardUI
    {
        [Header("Ana Kart")]
        public Button button;

        [Header("Reroll")]
        public Button rerollButton;
        public TextMeshProUGUI rerollText;

        [Header("Görseller")]
        public Image background;
        public Outline outline;
        public Image icon;

        [Header("Paneller")]
        public Image rarityPanel;
        public Image iconPanel;

        [Header("Yazılar")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI descriptionText;
    }

    // ==================================
    // CARD CHOICE
    // ==================================

    [System.Serializable]
    public class CardChoice
    {
        public UpgradeCardData data;
        public UpgradeCardData.Rarity rarity;
    }

    // ==================================
    // PANEL
    // ==================================

    [Header("Panel")]
    public GameObject upgradePanel;

    // ==================================
    // KART UI
    // ==================================

    // Inspector bağlantıları bozulmasın diye ayrı alanlar olarak kalıyor,
    // kod içinde cards dizisi üzerinden kullanılıyor.
    [Header("Kart UI")]
    public CardUI card1;
    public CardUI card2;
    public CardUI card3;

    // ==================================
    // KART HAVUZU
    // ==================================

    [Header("Kart Havuzu")]
    public List<UpgradeCardData> allCards =
        new List<UpgradeCardData>();

    // ==================================
    // REROLL AYARLARI
    // ==================================

    [Header("Reroll Ayarları")]

    [Tooltip("Her kartın ücretsiz reroll hakkı.")]
    public int freeRerollsPerCard = 1;

    [Tooltip("Ücretsiz haktan sonra yapılabilecek ücretli reroll sayısı.")]
    public int maxPaidRerollsPerCard = 2;

    [Tooltip("İlk ücretli reroll fiyatı.")]
    public int firstPaidRerollCost = 20;

    [Tooltip("Her ücretli rerollda eklenecek Gold.")]
    public int rerollCostIncrease = 20;

    // ==================================
    // STATE
    // ==================================

    private CardUI[] cards;

    private readonly CardChoice[] choices = new CardChoice[CardCount];
    private readonly int[] freeRerollsUsed = new int[CardCount];
    private readonly int[] paidRerollsUsed = new int[CardCount];
    private readonly Text[] rerollButtonLabels = new Text[CardCount];

    private bool isOpen = false;

    // Kart ekranı açıkken gelen level-up'lar kaybolmasın,
    // seçim yapılınca sırayla gösterilsin.
    private int pendingLevelUps = 0;

    // ==================================
    // AWAKE
    // ==================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        cards = new CardUI[] { card1, card2, card3 };
    }

    // ==================================
    // START
    // ==================================

    void Start()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        // Kalıcı "Şanslı El" yükseltmesi
        freeRerollsPerCard += PermanentUpgrades.BonusFreeRerolls;

        SetupButtons();

        // Yeni düşmanlara karşı kartlar (asset gerektirmez)
        AddRuntimeCards();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            TimeScaleController.Resume(this);
        }
    }

    // ==================================
    // BUTTON BAĞLANTILARI
    // ==================================

    void SetupButtons()
    {
        for (int i = 0; i < CardCount; i++)
        {
            CardUI cardUI = cards[i];

            if (cardUI == null)
                continue;

            // Lambda içinde döngü değişkenini değil kopyasını kullan.
            int index = i;

            if (cardUI.rerollButton != null)
            {
                cardUI.rerollButton.onClick.RemoveAllListeners();
                cardUI.rerollButton.onClick.AddListener(
                    () => RerollCard(index)
                );

                // Reroll durumu butonun kendi etiketine yazılır,
                // ayrı durum yazısı gizlenir (aynı şey iki kez yazmasın).
                rerollButtonLabels[i] = cardUI.rerollButton.GetComponentInChildren<Text>(true);

                if (rerollButtonLabels[i] != null && cardUI.rerollText != null)
                {
                    cardUI.rerollText.gameObject.SetActive(false);
                }
            }

            if (cardUI.button != null)
            {
                cardUI.button.onClick.RemoveAllListeners();
                cardUI.button.onClick.AddListener(
                    () => SelectCard(index)
                );
            }
        }
    }

    // ==================================
    // KARTLARI GÖSTER
    // ==================================

    public void ShowCards()
    {
        if (isOpen)
        {
            pendingLevelUps++;
            return;
        }

        if (PlayerStats.Instance == null)
        {
            Debug.LogError("PlayerStats bulunamadı!");
            return;
        }

        List<UpgradeCardData> availableCards =
            GetAvailableCards();

        if (availableCards.Count < CardCount)
        {
            Debug.LogWarning(
                "Seçilebilecek en az 3 kart kalmadı!"
            );

            pendingLevelUps = 0;
            return;
        }

        isOpen = true;

        // Her level-up'ta reroll hakları sıfırlanır.
        System.Array.Clear(freeRerollsUsed, 0, CardCount);
        System.Array.Clear(paidRerollsUsed, 0, CardCount);

        Shuffle(availableCards);

        for (int i = 0; i < CardCount; i++)
        {
            SetChoice(
                i,
                CreateChoice(availableCards[i])
            );
        }

        GameAnalytics.CardsOffered(CurrentPlayerLevel, GetOfferedCardIds());

        UpdateAllRerollButtons();

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }

        TimeScaleController.Pause(this);

        ShowFirstTimeHint();
    }

    // ==================================
    // ANALİTİK
    // ==================================

    static int CurrentPlayerLevel =>
        LevelSystem.Instance != null ? LevelSystem.Instance.currentLevel : 0;

    List<string> GetOfferedCardIds()
    {
        List<string> ids = new List<string>();

        foreach (CardChoice choice in choices)
        {
            if (choice != null && choice.data != null)
            {
                ids.Add(choice.data.upgradeType.ToString());
            }
        }

        return ids;
    }

    // ==================================
    // İLK SEFER İPUCU
    // ==================================

    private GameObject hintRoot;

    // Kart ekranı ilk kez açıldığında üstte kısa bir açıklama gösterir.
    void ShowFirstTimeHint()
    {
        if (PlayerPrefs.GetInt(GameKeys.CardHintSeen, 0) == 1 || hintRoot != null)
            return;

        UITheme theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "CardHintCanvas", 600);
        hintRoot = canvas.gameObject;

        RectTransform banner = UIBuilder.CreateRect("Hint", canvas);
        UIBuilder.Place(banner, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1250f, 150f));
        theme.ApplyNotice(banner.gameObject.AddComponent<Image>());

        TextMeshProUGUI text = UIBuilder.CreateText(
            theme,
            banner,
            UIBuilder.ColorTag(theme.mainRedHover, Loc.T("SEVİYE ATLADIN!")) +
            Loc.Pick("  Güçlenmek için bir kart seç.\n", "  Pick a card to grow stronger.\n") +
            Loc.Pick("<size=28>Nadir kartlar daha güçlüdür. YENİLE ile bir kartı yenisiyle değiştirebilirsin.</size>", "<size=28>Rarer cards are stronger. Use REROLL to swap a card for a new one.</size>"),
            36f,
            TextAlignmentOptions.Center,
            theme.mainWhite
        );
        UIBuilder.Stretch(text.rectTransform);
        text.margin = new Vector4(60f, 10f, 60f, 10f);
    }

    void HideFirstTimeHint()
    {
        if (hintRoot == null)
            return;

        Destroy(hintRoot);
        hintRoot = null;

        PlayerPrefs.SetInt(GameKeys.CardHintSeen, 1);
        PlayerPrefs.Save();
    }

    // ==================================
    // UYGUN KARTLARI BUL
    // ==================================

    // KALKAN KIRICI, BÖLÜNME ENGELİ ve BOSS AVCISI kartlarını kodla oluşturur.
    // Simgeleri mevcut kartlardan ödünç alır.
    void AddRuntimeCards()
    {
        AddRuntimeCard(
            UpgradeCardData.UpgradeType.ShieldBreaker,
            "KALKAN KIRICI",
            UpgradeCardData.UpgradeType.Piercing,
            3
        );

        AddRuntimeCard(
            UpgradeCardData.UpgradeType.SplitStopper,
            "BÖLÜNME ENGELİ",
            UpgradeCardData.UpgradeType.ExplosiveBullet,
            3
        );

        AddRuntimeCard(
            UpgradeCardData.UpgradeType.BossHunter,
            "BOSS AVCISI",
            UpgradeCardData.UpgradeType.CriticalChance,
            4
        );
    }

    void AddRuntimeCard(UpgradeCardData.UpgradeType type, string cardName, UpgradeCardData.UpgradeType iconFrom, int maxLevel)
    {
        foreach (UpgradeCardData existing in allCards)
        {
            if (existing != null && existing.upgradeType == type)
                return;
        }

        UpgradeCardData card = ScriptableObject.CreateInstance<UpgradeCardData>();
        card.name = cardName;
        card.cardName = cardName;
        card.upgradeType = type;
        card.maxLevel = maxLevel;

        foreach (UpgradeCardData existing in allCards)
        {
            if (existing != null && existing.upgradeType == iconFrom)
            {
                card.icon = existing.icon;
                break;
            }
        }

        allCards.Add(card);
    }

    // Yeni kartlar ilgili düşman / boss çıkmaya başlayınca sunulur.
    static bool IsCardUnlocked(UpgradeCardData.UpgradeType type, int playerLevel)
    {
        switch (type)
        {
            case UpgradeCardData.UpgradeType.ShieldBreaker: return playerLevel >= 5;
            case UpgradeCardData.UpgradeType.SplitStopper: return playerLevel >= 7;
            case UpgradeCardData.UpgradeType.BossHunter: return playerLevel >= 4;
            default: return true;
        }
    }

    List<UpgradeCardData> GetAvailableCards()
    {
        List<UpgradeCardData> availableCards =
            new List<UpgradeCardData>();

        if (PlayerStats.Instance == null)
            return availableCards;

        foreach (UpgradeCardData card in allCards)
        {
            if (card == null)
                continue;

            int currentLevel =
                PlayerStats.Instance.GetUpgradeLevel(
                    card.upgradeType
                );

            if (currentLevel < card.maxLevel && IsCardUnlocked(card.upgradeType, CurrentPlayerLevel))
            {
                availableCards.Add(card);
            }
        }

        return availableCards;
    }

    CardChoice CreateChoice(UpgradeCardData data)
    {
        return new CardChoice
        {
            data = data,
            rarity = RollRarity()
        };
    }

    void SetChoice(int cardIndex, CardChoice choice)
    {
        choices[cardIndex] = choice;

        SetupCard(
            cards[cardIndex],
            choice
        );
    }

    // ==================================
    // REROLL
    // ==================================

    public void RerollCard(int cardIndex)
    {
        if (!isOpen)
            return;

        if (cardIndex < 0 || cardIndex >= CardCount)
            return;

        if (PlayerStats.Instance == null)
            return;

        if (!CanReroll(cardIndex))
        {
            GameLog.Log("Bu kart için reroll hakkı bitti!");
            return;
        }

        bool isFree = HasFreeReroll(cardIndex);

        int cost = 0;

        if (!isFree)
        {
            cost = GetPaidRerollCost(cardIndex);

            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager bulunamadı!");
                return;
            }

            if (GameManager.Instance.gold < cost)
            {
                GameLog.Log("Yeterli Gold yok! Gerekli: " + cost);
                return;
            }
        }

        // Ekrandaki kartlar (değiştirilen dahil) tekrar çıkmasın.
        List<UpgradeCardData> availableCards =
            GetAvailableCards();

        foreach (CardChoice choice in choices)
        {
            if (choice != null && choice.data != null)
            {
                availableCards.Remove(choice.data);
            }
        }

        if (availableCards.Count == 0)
        {
            Debug.LogWarning("Reroll için farklı kart kalmadı!");
            return;
        }

        if (isFree)
        {
            freeRerollsUsed[cardIndex]++;
        }
        else
        {
            if (!GameManager.Instance.TrySpendGold(cost))
            {
                GameLog.Log("Gold harcanamadı!");
                return;
            }

            paidRerollsUsed[cardIndex]++;
        }

        UpgradeCardData newCard =
            availableCards[Random.Range(0, availableCards.Count)];

        UpgradeCardData oldCard = choices[cardIndex] != null ? choices[cardIndex].data : null;

        SetChoice(
            cardIndex,
            CreateChoice(newCard)
        );

        GameAnalytics.CardRerolled(
            CurrentPlayerLevel,
            oldCard != null ? oldCard.upgradeType.ToString() : string.Empty,
            newCard.upgradeType.ToString(),
            isFree
        );

        UpdateAllRerollButtons();

        GameLog.Log(
            isFree
                ? "Ücretsiz reroll kullanıldı."
                : "Reroll için " + cost + " Gold harcandı."
        );
    }

    bool HasFreeReroll(int cardIndex)
    {
        return freeRerollsUsed[cardIndex] < freeRerollsPerCard;
    }

    bool CanReroll(int cardIndex)
    {
        return HasFreeReroll(cardIndex) ||
               paidRerollsUsed[cardIndex] < maxPaidRerollsPerCard;
    }

    int GetPaidRerollCost(int cardIndex)
    {
        return firstPaidRerollCost +
               paidRerollsUsed[cardIndex] * rerollCostIncrease;
    }

    // ==================================
    // REROLL BUTTONLARI
    // ==================================

    void UpdateAllRerollButtons()
    {
        for (int i = 0; i < CardCount; i++)
        {
            UpdateRerollButton(cards[i], i);
        }
    }

    void UpdateRerollButton(CardUI cardUI, int cardIndex)
    {
        if (cardUI == null)
            return;

        string label;
        bool interactable;

        if (!CanReroll(cardIndex))
        {
            label = Loc.Pick("YENİLE - BİTTİ", "REROLL - NONE LEFT");
            interactable = false;
        }
        else if (HasFreeReroll(cardIndex))
        {
            label = Loc.Pick("YENİLE - ÜCRETSİZ", "REROLL - FREE");
            interactable = true;
        }
        else
        {
            int cost = GetPaidRerollCost(cardIndex);

            label = Loc.Pick("YENİLE - ", "REROLL - ") + cost + " " + Loc.T("ALTIN");
            interactable =
                GameManager.Instance != null &&
                GameManager.Instance.gold >= cost;
        }

        // Her şey butonun kendi etiketinde yazar; buton etiketi yoksa
        // eski ayrı durum yazısı kullanılır.
        Text buttonLabel = rerollButtonLabels[cardIndex];

        if (buttonLabel != null)
        {
            buttonLabel.text = label;
        }
        else if (cardUI.rerollText != null)
        {
            cardUI.rerollText.text = label;
        }

        if (cardUI.rerollButton != null)
        {
            cardUI.rerollButton.interactable = interactable;
        }
    }

    // ==================================
    // RARITY
    // ==================================

    UpgradeCardData.Rarity RollRarity()
    {
        float roll = Random.Range(0f, 100f);

        // COMMON %60
        if (roll < 60f)
            return UpgradeCardData.Rarity.Common;

        // RARE %25
        if (roll < 85f)
            return UpgradeCardData.Rarity.Rare;

        // EPIC %12
        if (roll < 97f)
            return UpgradeCardData.Rarity.Epic;

        // LEGENDARY %3
        return UpgradeCardData.Rarity.Legendary;
    }

    float GetRarityMultiplier(UpgradeCardData.Rarity rarity)
    {
        switch (rarity)
        {
            case UpgradeCardData.Rarity.Rare:
                return 1.5f;

            case UpgradeCardData.Rarity.Epic:
                return 2f;

            case UpgradeCardData.Rarity.Legendary:
                return 3f;

            default:
                return 1f;
        }
    }

    static string GetRarityName(UpgradeCardData.Rarity rarity)
    {
        switch (rarity)
        {
            case UpgradeCardData.Rarity.Rare:
                return Loc.Pick("NADİR", "RARE");

            case UpgradeCardData.Rarity.Epic:
                return Loc.Pick("EPİK", "EPIC");

            case UpgradeCardData.Rarity.Legendary:
                return Loc.Pick("EFSANEVİ", "LEGENDARY");

            default:
                return Loc.Pick("SIRADAN", "COMMON");
        }
    }

    Color GetRarityColor(UpgradeCardData.Rarity rarity)
    {
        switch (rarity)
        {
            case UpgradeCardData.Rarity.Common:
                return new Color(0.75f, 0.75f, 0.75f, 1f);

            case UpgradeCardData.Rarity.Rare:
                return new Color(0.20f, 0.60f, 1f, 1f);

            case UpgradeCardData.Rarity.Epic:
                return new Color(0.75f, 0.25f, 1f, 1f);

            case UpgradeCardData.Rarity.Legendary:
                return new Color(1f, 0.65f, 0.10f, 1f);

            default:
                return Color.white;
        }
    }

    // ==================================
    // KART UI
    // ==================================

    void SetupCard(CardUI cardUI, CardChoice choice)
    {
        if (cardUI == null ||
            choice == null ||
            choice.data == null)
        {
            return;
        }

        UpgradeCardData data = choice.data;

        if (cardUI.nameText != null)
        {
            cardUI.nameText.text = Loc.T(data.cardName);
        }

        if (cardUI.levelText != null &&
            PlayerStats.Instance != null)
        {
            int currentLevel =
                PlayerStats.Instance.GetUpgradeLevel(
                    data.upgradeType
                );

            cardUI.levelText.text =
                Loc.T("SV. ") + currentLevel + " / " + data.maxLevel;
        }

        Color rarityColor = GetRarityColor(choice.rarity);

        if (cardUI.rarityText != null)
        {
            cardUI.rarityText.text =
                GetRarityName(choice.rarity);

            // Panel renkli olduğu için yazı beyaz.
            cardUI.rarityText.color = Color.white;
        }

        if (cardUI.rarityPanel != null)
        {
            // Alt rarity paneli tam rarity renginde.
            cardUI.rarityPanel.color = rarityColor;
        }

        if (cardUI.iconPanel != null)
        {
            // Icon panel biraz daha koyu rarity renginde.
            Color iconPanelColor = rarityColor * 0.65f;

            // Çarpma işlemi alpha'yı da değiştirdiği için
            // tekrar 1 yapıyoruz.
            iconPanelColor.a = 1f;

            cardUI.iconPanel.color = iconPanelColor;
        }

        if (cardUI.outline != null)
        {
            cardUI.outline.effectColor = rarityColor;
            cardUI.outline.effectDistance = new Vector2(3f, -3f);
        }

        if (cardUI.descriptionText != null)
        {
            cardUI.descriptionText.text = GetCardDescription(choice);
        }

        if (cardUI.icon != null)
        {
            cardUI.icon.sprite = data.icon;
            cardUI.icon.enabled = data.icon != null;

            // Icon'un kendi rengini değiştirmiyoruz.
            // Orijinal sprite rengi korunuyor.
            cardUI.icon.color = Color.white;
        }
    }

    // ==================================
    // KART AÇIKLAMASI
    // ==================================

    private static readonly System.Globalization.CultureInfo Turkish =
        new System.Globalization.CultureInfo("tr-TR");

    // Kartın bu nadirlikte ve oyuncunun şu anki seviyesinde gerçekte ne
    // yapacağını yazar. Değerler ApplyUpgrade / PlayerStats ile aynı hesaplanır.
    string GetCardDescription(CardChoice choice)
    {
        UpgradeCardData data = choice.data;
        PlayerStats stats = PlayerStats.Instance;

        if (stats == null)
            return Loc.T(data.description);

        float rarity = GetRarityMultiplier(choice.rarity);
        float floatValue = data.floatValue * rarity;
        int intValue = Mathf.Max(1, Mathf.RoundToInt(data.intValue * rarity));

        int currentLevel = stats.GetUpgradeLevel(data.upgradeType);
        bool firstPick = currentLevel == 0;
        float step = PlayerStats.RarityStep(currentLevel + 1, rarity);

        switch (data.upgradeType)
        {
            case UpgradeCardData.UpgradeType.Damage:
                return L("Silah hasarını " + Percent(floatValue) + " artırır.", "Increases weapon damage by " + Percent(floatValue) + ".");

            case UpgradeCardData.UpgradeType.FireRate:
                return L("Ateş hızını " + Percent(floatValue) + " artırır.", "Increases fire rate by " + Percent(floatValue) + ".");

            case UpgradeCardData.UpgradeType.MaxHealth:
                return L("Maksimum canı " + intValue + " artırır.", "Increases max health by " + intValue + ".");

            case UpgradeCardData.UpgradeType.BulletSpeed:
                return L("Mermi hızını " + Percent(floatValue) + " artırır.", "Increases bullet speed by " + Percent(floatValue) + ".");

            case UpgradeCardData.UpgradeType.CriticalChance:
            {
                float total = Mathf.Clamp01(stats.criticalChance + floatValue);
                return L(
                    "Kritik vuruş şansını " + Percent(floatValue) + " artırır (toplam " + Percent(total) + "). Kritik vuruş " + Number(stats.criticalDamageMultiplier) + " kat hasar verir.",
                    "Increases critical chance by " + Percent(floatValue) + " (total " + Percent(total) + "). Critical hits deal " + Number(stats.criticalDamageMultiplier) + "x damage."
                );
            }

            case UpgradeCardData.UpgradeType.Piercing:
                return L(
                    "Mermiler +" + intValue + " düşmanı delip geçer (toplam " + (stats.piercing + intValue) + ").",
                    "Bullets pierce +" + intValue + " enemies (total " + (stats.piercing + intValue) + ")."
                );

            case UpgradeCardData.UpgradeType.ExtraBullet:
                return L(
                    "Mermi hasarını %100 artırır (toplam " + (stats.extraBullets + 2) + " kat hasar).",
                    "Increases bullet damage by 100% (total " + (stats.extraBullets + 2) + "x damage)."
                );

            case UpgradeCardData.UpgradeType.ExplosiveBullet:
            {
                float radius = stats.explosionRadius + stats.explosionRadiusPerLevel * step;
                float damage = stats.explosionDamageMultiplier + stats.explosionDamagePerLevel * step;

                return firstPick
                    ? L("Mermiler çarptığı yerde patlar: " + Number(radius) + " birim çevredeki düşmanlara " + Percent(damage) + " hasar verir.",
                        "Bullets explode on impact, dealing " + Percent(damage) + " damage to enemies within " + Number(radius) + " units.")
                    : L("Patlama alanı " + Number(radius) + " birim, patlama hasarı " + Percent(damage) + " olur.",
                        "Explosion radius becomes " + Number(radius) + " units, explosion damage " + Percent(damage) + ".");
            }

            case UpgradeCardData.UpgradeType.FreezeBullet:
            {
                float slow = Mathf.Clamp(stats.freezeSlowAmount + stats.freezeSlowPerLevel * step, 0f, 0.8f);
                float duration = stats.freezeDuration + stats.freezeDurationPerLevel * step;

                return firstPick
                    ? L("Mermiler düşmanları " + Number(duration) + " saniye boyunca " + Percent(slow) + " yavaşlatır.",
                        "Bullets slow enemies by " + Percent(slow) + " for " + Number(duration) + " seconds.")
                    : L("Yavaşlatma " + Percent(slow) + ", süresi " + Number(duration) + " saniye olur.",
                        "Slow becomes " + Percent(slow) + " for " + Number(duration) + " seconds.");
            }

            case UpgradeCardData.UpgradeType.BurnBullet:
            {
                int damage = stats.burnDamage + Mathf.RoundToInt(stats.burnDamagePerLevel * step);

                return firstPick
                    ? L("Mermiler düşmanları " + Number(stats.burnDuration) + " saniye boyunca saniyede " + damage + " hasarla yakar. Yanma kalkanları deler.",
                        "Bullets burn enemies for " + damage + " damage per second for " + Number(stats.burnDuration) + " seconds. Burn ignores shields.")
                    : L("Yanma hasarı saniyede " + damage + " olur.", "Burn damage becomes " + damage + " per second.");
            }

            case UpgradeCardData.UpgradeType.ChainLightning:
            {
                int count = stats.chainCount + Mathf.RoundToInt(stats.chainCountPerLevel * step);
                float range = stats.chainRange + stats.chainRangePerLevel * step;

                return firstPick
                    ? L("İsabet eden mermiden " + count + " düşmana sıçrayan yıldırım çıkar (" + Percent(stats.chainDamageMultiplier) + " hasar, " + Number(range) + " birim menzil).",
                        "Hits release lightning that jumps to " + count + " enemies (" + Percent(stats.chainDamageMultiplier) + " damage, " + Number(range) + " unit range).")
                    : L("Yıldırım " + count + " düşmana sıçrar, menzili " + Number(range) + " birim olur.",
                        "Lightning jumps to " + count + " enemies with " + Number(range) + " unit range.");
            }

            case UpgradeCardData.UpgradeType.ShieldBreaker:
            {
                float next = firstPick
                    ? 1f + Mathf.Max(1f, rarity)
                    : stats.shieldDamageMultiplier + 0.5f * Mathf.Max(1f, rarity);
                next = Mathf.Min(next, 4f);

                return L("Kalkanlara " + Number(next) + " kat hasar verirsin. Kalkanlı düşmanlar ve muhafız boss çok daha çabuk açılır.",
                         "You deal " + Number(next) + "x damage to shields. Shielded enemies and Guardian bosses open up much faster.");
            }

            case UpgradeCardData.UpgradeType.SplitStopper:
            {
                float chance = Mathf.Clamp01(stats.splitBlockChance + 0.35f * Mathf.Max(1f, rarity));
                return L("Bölünen düşmanlar " + Percent(chance) + " olasılıkla ölünce bölünmez.",
                         "Splitters have a " + Percent(chance) + " chance not to split when they die.");
            }

            case UpgradeCardData.UpgradeType.BossHunter:
            {
                float bonus = stats.bossDamageMultiplier - 1f + 0.25f * Mathf.Max(1f, rarity);
                return L("Bosslara " + Percent(bonus) + " fazla hasar verirsin (yanma dahil).",
                         "You deal " + Percent(bonus) + " more damage to bosses (burn included).");
            }

            default:
                return Loc.T(data.description);
        }
    }

    static string L(string turkish, string english) => Loc.Pick(turkish, english);

    static string Percent(float value)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        return Loc.IsEnglish ? percent + "%" : "%" + percent;
    }

    static string Number(float value)
    {
        return value.ToString("0.#", Loc.IsEnglish ? System.Globalization.CultureInfo.InvariantCulture : Turkish);
    }

    // ==================================
    // KART SEÇ
    // ==================================

    void SelectCard(int cardIndex)
    {
        if (!isOpen)
            return;

        ApplyUpgrade(choices[cardIndex]);

        CardChoice chosen = choices[cardIndex];

        if (chosen != null && chosen.data != null && PlayerStats.Instance != null)
        {
            GameAnalytics.CardChosen(
                CurrentPlayerLevel,
                chosen.data.upgradeType.ToString(),
                chosen.rarity.ToString(),
                PlayerStats.Instance.GetUpgradeLevel(chosen.data.upgradeType)
            );
        }

        isOpen = false;

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        HideFirstTimeHint();

        TimeScaleController.Resume(this);

        // Ekran açıkken biriken level-up varsa sıradakini göster.
        if (pendingLevelUps > 0)
        {
            pendingLevelUps--;
            ShowCards();
        }
    }

    // ==================================
    // UPGRADE UYGULA
    // ==================================

    void ApplyUpgrade(CardChoice choice)
    {
        if (choice == null ||
            choice.data == null ||
            PlayerStats.Instance == null)
        {
            return;
        }

        UpgradeCardData data = choice.data;
        PlayerStats stats = PlayerStats.Instance;

        float rarityMultiplier =
            GetRarityMultiplier(choice.rarity);

        float finalFloatValue =
            data.floatValue * rarityMultiplier;

        int finalIntValue =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    data.intValue * rarityMultiplier
                )
            );

        switch (data.upgradeType)
        {
            case UpgradeCardData.UpgradeType.Damage:
                stats.UpgradeDamage(finalFloatValue);
                break;

            case UpgradeCardData.UpgradeType.FireRate:
                stats.UpgradeFireRate(finalFloatValue);
                break;

            case UpgradeCardData.UpgradeType.MaxHealth:
                stats.UpgradeMaxHealth(finalIntValue);
                break;

            case UpgradeCardData.UpgradeType.BulletSpeed:
                stats.UpgradeBulletSpeed(finalFloatValue);
                break;

            case UpgradeCardData.UpgradeType.CriticalChance:
                stats.UpgradeCriticalChance(finalFloatValue);
                break;

            case UpgradeCardData.UpgradeType.Piercing:
                stats.AddPiercing(finalIntValue);
                break;

            case UpgradeCardData.UpgradeType.ExtraBullet:
                // Multishot rarity'den etkilenmez: her seçimde +1 mermi
                // (kart açıklamasıyla aynı, mermi sayısı kontrolden çıkmasın).
                stats.AddExtraBullet(1);
                break;

            case UpgradeCardData.UpgradeType.ExplosiveBullet:
                stats.UpgradeExplosiveBullets(rarityMultiplier);
                break;

            case UpgradeCardData.UpgradeType.FreezeBullet:
                stats.UpgradeFreezeBullets(rarityMultiplier);
                break;

            case UpgradeCardData.UpgradeType.BurnBullet:
                stats.UpgradeBurnBullets(rarityMultiplier);
                break;

            case UpgradeCardData.UpgradeType.ChainLightning:
                stats.UpgradeChainLightning(rarityMultiplier);
                break;

            case UpgradeCardData.UpgradeType.ShieldBreaker:
                stats.UpgradeShieldBreaker(rarityMultiplier);
                break;

            case UpgradeCardData.UpgradeType.SplitStopper:
                stats.UpgradeSplitStopper(rarityMultiplier);
                break;

            case UpgradeCardData.UpgradeType.BossHunter:
                stats.UpgradeBossHunter(rarityMultiplier);
                break;
        }

        GameLog.Log(
            data.cardName +
            " | " +
            choice.rarity +
            " seçildi. | x" +
            rarityMultiplier
        );
    }

    // ==================================
    // SHUFFLE
    // ==================================

    void Shuffle(List<UpgradeCardData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            UpgradeCardData temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
