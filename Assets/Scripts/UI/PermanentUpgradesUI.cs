using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki kalıcı yükseltmeler ekranı: kalıcı altınla her oyunun
// başında geçerli küçük bonuslar alınır (PermanentUpgrades).
public class PermanentUpgradesUI : MonoBehaviour
{
    private class Row
    {
        public PermanentUpgrades.Upgrade upgrade;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI bonusText;
        public readonly List<Image> pips = new List<Image>();
        public Button button;
        public TextMeshProUGUI buttonText;
    }

    private UITheme theme;
    private GameObject panel;
    private RectTransform badge;
    private TextMeshProUGUI goldLabel;
    private readonly List<Row> rows = new List<Row>();

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "UpgradesCanvas", 160);

        Button openButton = UIBuilder.CreateMenuButton(theme, canvas, "GELİŞTİRMELER", UIBuilder.MenuSlotUpgrades, out badge);
        openButton.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        BuildPanel(canvas);

        PlayerProgress.OnTotalGoldChanged += OnGoldChanged;
        UpdateBadge();
    }

    void OnDestroy()
    {
        PlayerProgress.OnTotalGoldChanged -= OnGoldChanged;
    }

    void OnGoldChanged(int total)
    {
        UpdateBadge();

        if (panel != null && panel.activeSelf)
        {
            Refresh();
        }
    }

    // Alınabilecek bir yükseltme varsa "!" rozeti
    void UpdateBadge()
    {
        if (badge == null)
            return;

        int gold = PlayerProgress.TotalGold;
        bool affordable = false;

        foreach (PermanentUpgrades.Upgrade upgrade in PermanentUpgrades.All)
        {
            if (!PermanentUpgrades.IsMaxed(upgrade) && gold >= PermanentUpgrades.GetCost(upgrade))
            {
                affordable = true;
                break;
            }
        }

        badge.gameObject.SetActive(affordable);
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
    }

    void OnBuyClicked(PermanentUpgrades.Upgrade upgrade)
    {
        if (!PermanentUpgrades.TryBuy(upgrade))
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        GameAnalytics.PermanentUpgradeBought(upgrade.id, PermanentUpgrades.GetLevel(upgrade));

        Refresh();
    }

    void Refresh()
    {
        int gold = PlayerProgress.TotalGold;

        goldLabel.text = UIBuilder.ColorTag(theme.mainRedHover, "ALTIN") + "  " + gold;

        foreach (Row row in rows)
        {
            PermanentUpgrades.Upgrade upgrade = row.upgrade;

            int level = PermanentUpgrades.GetLevel(upgrade);
            bool maxed = level >= upgrade.maxLevel;

            row.levelText.text = Loc.T("SV. ") + level + " / " + upgrade.maxLevel;

            for (int i = 0; i < row.pips.Count; i++)
            {
                row.pips[i].color = i < level ? theme.mainRed : theme.greyDark;
            }

            // Mevcut bonus -> bir sonraki seviyedeki bonus
            string current = FormatBonus(upgrade, PermanentUpgrades.GetValueAtLevel(upgrade, level));

            row.bonusText.text = maxed
                ? current
                : current + "  " + UIBuilder.ColorTag(theme.greyLight, ">") + "  " +
                  UIBuilder.ColorTag(theme.mainRedHover, FormatBonus(upgrade, PermanentUpgrades.GetValueAtLevel(upgrade, level + 1)));

            if (maxed)
            {
                theme.ApplyButton(row.button, (Image)row.button.targetGraphic, false);
                row.button.interactable = false;
                row.buttonText.text = "MAKS.";
                row.buttonText.color = theme.mainRedHover;
                continue;
            }

            int cost = PermanentUpgrades.GetCost(upgrade);
            bool canAfford = gold >= cost;

            theme.ApplyButton(row.button, (Image)row.button.targetGraphic, true);
            row.button.interactable = canAfford;
            row.buttonText.text = cost + " ALTIN";
            row.buttonText.color = canAfford ? theme.mainWhite : theme.greyLight;
        }

        UpdateBadge();
    }

    static string FormatBonus(PermanentUpgrades.Upgrade upgrade, float value)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            upgrade.bonusFormat,
            Mathf.RoundToInt(value)
        );
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "UpgradesPanel");
        panel = dim.gameObject;

        RectTransform safeRoot = UIBuilder.CreateSafeRoot(dim);

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            safeRoot,
            new Vector2(0.15f, 0.06f),
            new Vector2(0.85f, 0.94f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "GELİŞTİRMELER");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        goldLabel = UIBuilder.CreateText(theme, window, string.Empty, 40f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        UIBuilder.AnchorTop(goldLabel.rectTransform, 25f, 100f);
        goldLabel.rectTransform.offsetMin = new Vector2(70f, goldLabel.rectTransform.offsetMin.y);

        TextMeshProUGUI hint = UIBuilder.CreateText(
            theme,
            window,
            "Kalıcı bonuslar her oyunun başında otomatik uygulanır.",
            28f,
            TextAlignmentOptions.Center,
            theme.greyLight
        );
        UIBuilder.AnchorTop(hint.rectTransform, 145f, 45f);

        RectTransform content = UIBuilder.CreateScrollList(window, new Vector2(60f, 45f), new Vector2(-60f, -200f));

        foreach (PermanentUpgrades.Upgrade upgrade in PermanentUpgrades.All)
        {
            rows.Add(CreateRow(content, upgrade));
        }

        panel.SetActive(false);
    }

    Row CreateRow(RectTransform content, PermanentUpgrades.Upgrade upgrade)
    {
        Row row = new Row { upgrade = upgrade };

        RectTransform rowRect = UIBuilder.CreateRect("Upgrade_" + upgrade.id, content);
        theme.ApplyNotice(rowRect.gameObject.AddComponent<Image>());
        rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;

        // Başlık
        TextMeshProUGUI title = UIBuilder.CreateText(theme, rowRect, upgrade.title, 40f, TextAlignmentOptions.BottomLeft, theme.mainWhite);
        title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(60f, 8f);
        title.rectTransform.offsetMax = new Vector2(-600f, -18f);

        // Seviye
        row.levelText = UIBuilder.CreateText(theme, rowRect, string.Empty, 26f, TextAlignmentOptions.BottomRight, theme.greyLight);
        row.levelText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        row.levelText.rectTransform.anchorMax = new Vector2(1f, 1f);
        row.levelText.rectTransform.pivot = new Vector2(1f, 0.5f);
        row.levelText.rectTransform.offsetMin = new Vector2(-560f, 8f);
        row.levelText.rectTransform.offsetMax = new Vector2(-390f, -18f);

        // Bonus açıklaması (mevcut -> sonraki)
        row.bonusText = UIBuilder.CreateText(theme, rowRect, string.Empty, 28f, TextAlignmentOptions.TopLeft, theme.mainWhite);
        row.bonusText.rectTransform.anchorMin = new Vector2(0f, 0f);
        row.bonusText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        row.bonusText.rectTransform.offsetMin = new Vector2(60f, 46f);
        row.bonusText.rectTransform.offsetMax = new Vector2(-390f, -4f);

        // Seviye noktaları
        RectTransform pipRow = UIBuilder.CreateRect("Pips", rowRect);
        pipRow.anchorMin = new Vector2(0f, 0f);
        pipRow.anchorMax = new Vector2(1f, 0f);
        pipRow.pivot = new Vector2(0f, 0f);
        pipRow.offsetMin = new Vector2(60f, 24f);
        pipRow.offsetMax = new Vector2(-390f, 40f);

        HorizontalLayoutGroup pipLayout = pipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        pipLayout.spacing = 8f;
        pipLayout.childAlignment = TextAnchor.MiddleLeft;
        pipLayout.childControlWidth = false;
        pipLayout.childControlHeight = true;
        pipLayout.childForceExpandWidth = false;

        for (int i = 0; i < upgrade.maxLevel; i++)
        {
            RectTransform pip = UIBuilder.CreateRect("Pip", pipRow);
            pip.sizeDelta = new Vector2(36f, 16f);

            Image pipImage = pip.gameObject.AddComponent<Image>();
            pipImage.raycastTarget = false;
            row.pips.Add(pipImage);
        }

        // Satın al
        row.button = UIBuilder.CreateButton(theme, rowRect, "BuyButton", string.Empty, true, out row.buttonText);
        UIBuilder.Place((RectTransform)row.button.transform, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(300f, 100f));
        row.button.onClick.AddListener(() => OnBuyClicked(upgrade));

        return row;
    }
}
