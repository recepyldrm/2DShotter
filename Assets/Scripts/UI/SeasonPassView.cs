using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Görevler penceresindeki SEZON sekmesi: sezon seviyesi, kalan süre,
// premium satın alma ve 30 seviyenin ücretsiz / premium ödülleri.
public class SeasonPassView
{
    private class TierRow
    {
        public int tier;
        public TextMeshProUGUI label;
        public Button freeButton;
        public TextMeshProUGUI freeText;
        public Button premiumButton;
        public TextMeshProUGUI premiumText;
    }

    private readonly UITheme theme;
    private readonly GameObject root;
    private readonly List<TierRow> rows = new List<TierRow>();

    private TextMeshProUGUI headerText;
    private TextMeshProUGUI progressText;
    private RectTransform progressFill;
    private Button premiumButton;
    private TextMeshProUGUI premiumButtonText;
    private TextMeshProUGUI statusText;

    public bool IsVisible => root.activeSelf;

    public SeasonPassView(UITheme theme, RectTransform window)
    {
        this.theme = theme;

        RectTransform container = UIBuilder.CreateRect("SeasonPass", window);
        UIBuilder.Stretch(container);
        container.offsetMin = new Vector2(60f, 40f);
        container.offsetMax = new Vector2(-60f, -245f);
        root = container.gameObject;

        Build(container);
        root.SetActive(false);

        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSucceeded += OnPurchaseSucceeded;
            IAPManager.Instance.OnPurchaseFailed += OnPurchaseFailed;
            IAPManager.Instance.OnProductsReady += Refresh;
        }
    }

    public void Dispose()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSucceeded -= OnPurchaseSucceeded;
            IAPManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
            IAPManager.Instance.OnProductsReady -= Refresh;
        }
    }

    public void SetVisible(bool visible)
    {
        root.SetActive(visible);

        if (visible)
        {
            statusText.text = string.Empty;
            Refresh();
        }
    }

    void OnPurchaseSucceeded(string productId)
    {
        if (productId != SeasonPass.ProductId)
            return;

        statusText.text = Loc.Pick("PREMİUM AÇILDI!", "PREMIUM UNLOCKED!");
        Refresh();
    }

    void OnPurchaseFailed(string productId, string message)
    {
        if (productId == SeasonPass.ProductId && root.activeSelf)
        {
            statusText.text = Loc.T(message);
        }
    }

    public void UpdateTimer()
    {
        if (!root.activeSelf)
            return;

        System.TimeSpan left = SeasonPass.TimeLeft;
        string time = Loc.Pick((int)left.TotalDays + " gün " + left.Hours + " sa", (int)left.TotalDays + "d " + left.Hours + "h");

        headerText.text =
            Loc.Pick("SEZON ", "SEASON ") + SeasonPass.SeasonNumber +
            UIBuilder.ColorTag(theme.greyLight, "   <size=75%>" + Loc.Pick("Kalan: ", "Ends in: ") + time + "</size>");
    }

    public void Refresh()
    {
        int tier = SeasonPass.Tier;
        bool premium = SeasonPass.IsPremium;

        UpdateTimer();

        progressFill.anchorMax = new Vector2(SeasonPass.TierProgress, 1f);

        progressText.text = tier >= SeasonPass.TierCount
            ? Loc.Pick("SEZON TAMAMLANDI", "SEASON COMPLETE")
            : Loc.Pick("Seviye ", "Tier ") + tier + "  ·  " +
              (SeasonPass.Points % SeasonPass.PointsPerTier) + " / " + SeasonPass.PointsPerTier;

        Image premiumImage = (Image)premiumButton.targetGraphic;

        if (premium)
        {
            theme.ApplyButton(premiumButton, premiumImage, false);
            premiumButton.interactable = false;
            premiumButtonText.text = Loc.Pick("PREMİUM AKTİF", "PREMIUM ACTIVE");
            premiumButtonText.color = theme.mainRedHover;
        }
        else
        {
            string price = IAPManager.Instance != null ? IAPManager.Instance.GetPriceString(SeasonPass.ProductId) : string.Empty;

            theme.ApplyButton(premiumButton, premiumImage, true);
            premiumButton.interactable = true;
            premiumButtonText.text = Loc.Pick("PREMİUM AL", "GET PREMIUM") + (string.IsNullOrEmpty(price) ? string.Empty : "  " + price);
            premiumButtonText.color = theme.mainWhite;
        }

        foreach (TierRow row in rows)
        {
            bool reached = row.tier <= tier;

            row.label.text = Loc.Pick("SV. ", "LV. ") + row.tier;
            row.label.color = reached ? theme.mainWhite : theme.greyLight;

            ApplyReward(row.freeButton, row.freeText, SeasonPass.FreeReward(row.tier),
                SeasonPass.IsFreeClaimed(row.tier), SeasonPass.CanClaimFree(row.tier), reached, true);

            ApplyReward(row.premiumButton, row.premiumText, SeasonPass.PremiumReward(row.tier),
                SeasonPass.IsPremiumClaimed(row.tier), SeasonPass.CanClaimPremium(row.tier), reached && premium, premium);
        }
    }

    void ApplyReward(Button button, TextMeshProUGUI text, SeasonPass.Reward reward, bool claimed, bool claimable, bool unlocked, bool trackOpen)
    {
        Image image = (Image)button.targetGraphic;
        string label = RewardLabel(reward);

        if (claimed)
        {
            theme.ApplyButton(button, image, false);
            button.interactable = false;
            text.text = Loc.T("ALINDI");
            text.color = theme.mainRedHover;
        }
        else if (claimable)
        {
            theme.ApplyButton(button, image, true);
            button.interactable = true;
            text.text = Loc.Pick("AL ", "CLAIM ") + label;
            text.color = theme.mainWhite;
        }
        else
        {
            theme.ApplyButton(button, image, false);
            button.interactable = false;
            // Premium alınmadıysa premium ödüller soluk görünür.
            text.text = label;
            text.color = unlocked && trackOpen ? theme.mainWhite : theme.greyLight;
        }
    }

    string RewardLabel(SeasonPass.Reward reward)
    {
        if (reward.IsSkin)
        {
            CompanionSkinDatabase database = CompanionSkinDatabase.Load();
            CompanionSkinDatabase.Skin skin = database != null ? database.Find(reward.skinId) : null;
            string name = skin != null ? skin.displayName : reward.skinId;
            return UIBuilder.ColorTag(theme.mainRedHover, name) + " + " + reward.gold;
        }

        return "+" + reward.gold + " " + Loc.T("ALTIN");
    }

    void OnClaim(int tier, bool premium)
    {
        bool claimed = premium ? SeasonPass.ClaimPremium(tier) : SeasonPass.ClaimFree(tier);

        if (!claimed)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUpgradePurchase();

        Haptics.Light();
        Refresh();
    }

    void OnBuyPremium()
    {
        UIBuilder.PlayClick();

        if (IAPManager.Instance == null)
        {
            statusText.text = Loc.Pick("MAĞAZA HAZIR DEĞİL", "STORE NOT READY");
            return;
        }

        statusText.text = string.Empty;
        IAPManager.Instance.Purchase(SeasonPass.ProductId);
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void Build(RectTransform container)
    {
        headerText = UIBuilder.CreateText(theme, container, string.Empty, 40f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        UIBuilder.AnchorTop(headerText.rectTransform, 0f, 55f);
        headerText.rectTransform.offsetMax = new Vector2(-420f, headerText.rectTransform.offsetMax.y);

        premiumButton = UIBuilder.CreateButton(theme, container, "PremiumButton", string.Empty, true, out premiumButtonText);
        UIBuilder.Place((RectTransform)premiumButton.transform, new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(400f, 80f));
        premiumButton.onClick.AddListener(OnBuyPremium);

        RectTransform bar = UIBuilder.CreateProgressBar(theme, container, out progressFill);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.offsetMin = new Vector2(0f, -92f);
        bar.offsetMax = new Vector2(-420f, -68f);

        progressText = UIBuilder.CreateText(theme, container, string.Empty, 24f, TextAlignmentOptions.MidlineLeft, theme.greyLight);
        UIBuilder.AnchorTop(progressText.rectTransform, 95f, 35f);

        statusText = UIBuilder.CreateText(theme, container, string.Empty, 24f, TextAlignmentOptions.MidlineRight, theme.mainRedHover);
        UIBuilder.AnchorTop(statusText.rectTransform, 95f, 35f);

        // Sütun başlıkları
        TextMeshProUGUI freeHeader = UIBuilder.CreateText(theme, container, Loc.Pick("ÜCRETSİZ", "FREE"), 26f, TextAlignmentOptions.Center, theme.greyLight);
        freeHeader.rectTransform.anchorMin = new Vector2(0.2f, 1f);
        freeHeader.rectTransform.anchorMax = new Vector2(0.58f, 1f);
        freeHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
        freeHeader.rectTransform.offsetMin = new Vector2(0f, -170f);
        freeHeader.rectTransform.offsetMax = new Vector2(0f, -135f);

        TextMeshProUGUI premiumHeader = UIBuilder.CreateText(theme, container, "PREMIUM", 26f, TextAlignmentOptions.Center, theme.mainRedHover);
        premiumHeader.rectTransform.anchorMin = new Vector2(0.6f, 1f);
        premiumHeader.rectTransform.anchorMax = new Vector2(1f, 1f);
        premiumHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
        premiumHeader.rectTransform.offsetMin = new Vector2(0f, -170f);
        premiumHeader.rectTransform.offsetMax = new Vector2(0f, -135f);

        RectTransform content = UIBuilder.CreateScrollList(container, Vector2.zero, new Vector2(0f, -175f), 10f);

        for (int tier = 1; tier <= SeasonPass.TierCount; tier++)
        {
            rows.Add(CreateRow(content, tier));
        }
    }

    TierRow CreateRow(RectTransform content, int tier)
    {
        RectTransform rowRect = UIBuilder.CreateRect("Tier_" + tier, content);
        theme.ApplyNotice(rowRect.gameObject.AddComponent<Image>());
        rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;

        TextMeshProUGUI label = UIBuilder.CreateText(theme, rowRect, string.Empty, 34f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        label.rectTransform.anchorMin = new Vector2(0f, 0f);
        label.rectTransform.anchorMax = new Vector2(0.2f, 1f);
        label.rectTransform.offsetMin = new Vector2(30f, 0f);
        label.rectTransform.offsetMax = Vector2.zero;

        Button freeButton = CreateRewardButton(rowRect, 0.2f, 0.58f, out TextMeshProUGUI freeText);
        freeButton.onClick.AddListener(() => OnClaim(tier, false));

        Button premiumRewardButton = CreateRewardButton(rowRect, 0.6f, 1f, out TextMeshProUGUI premiumText);
        premiumRewardButton.onClick.AddListener(() => OnClaim(tier, true));

        return new TierRow
        {
            tier = tier,
            label = label,
            freeButton = freeButton,
            freeText = freeText,
            premiumButton = premiumRewardButton,
            premiumText = premiumText
        };
    }

    Button CreateRewardButton(RectTransform row, float minX, float maxX, out TextMeshProUGUI text)
    {
        Button button = UIBuilder.CreateButton(theme, row, "Reward", string.Empty, false, out text);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(maxX, 1f);
        rect.offsetMin = new Vector2(10f, 12f);
        rect.offsetMax = new Vector2(-15f, -12f);
        text.fontSizeMax = 30f;
        return button;
    }
}
