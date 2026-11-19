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
            UIBuilder.ColorTag(theme.mainRedHover, "SEVİYE ATLADIN!") +
            "  Güçlenmek için bir kart seç.\n" +
            "<size=28>Nadir kartlar daha güçlüdür. YENİLE ile bir kartı yenisiyle değiştirebilirsin.</size>",
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

            if (currentLevel < card.maxLevel)
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
            label = "YENİLE - BİTTİ";
            interactable = false;
        }
        else if (HasFreeReroll(cardIndex))
        {
            label = "YENİLE - ÜCRETSİZ";
            interactable = true;
        }
        else
        {
            int cost = GetPaidRerollCost(cardIndex);

            label = "YENİLE - " + cost + " ALTIN";
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
                return "NADİR";

            case UpgradeCardData.Rarity.Epic:
                return "EPİK";

            case UpgradeCardData.Rarity.Legendary:
                return "EFSANEVİ";

            default:
                return "SIRADAN";
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
            cardUI.nameText.text = data.cardName;
        }

        if (cardUI.levelText != null &&
            PlayerStats.Instance != null)
        {
            int currentLevel =
                PlayerStats.Instance.GetUpgradeLevel(
                    data.upgradeType
                );

            cardUI.levelText.text =
                "SV. " + currentLevel + " / " + data.maxLevel;
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
            cardUI.descriptionText.text = data.description;
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
