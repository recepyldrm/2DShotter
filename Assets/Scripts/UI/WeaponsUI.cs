using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki silah seçimi: silahlar kalıcı altınla açılır,
// seçilen silah bir sonraki oyunda kullanılır (WeaponCatalog).
public class WeaponsUI : MonoBehaviour
{
    private class Row
    {
        public WeaponCatalog.Weapon weapon;
        public Button button;
        public TextMeshProUGUI buttonText;
        public Button upgradeButton;
        public TextMeshProUGUI upgradeText;
        public TextMeshProUGUI title;
        public TextMeshProUGUI stats;
    }

    private UITheme theme;
    private GameObject panel;
    private TextMeshProUGUI goldLabel;
    private readonly List<Row> rows = new List<Row>();

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "WeaponsCanvas", 165);

        Button openButton = UIBuilder.CreateMenuButton(theme, canvas, "SİLAHLAR", UIBuilder.MenuSlotWeapons, out _);
        openButton.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        BuildPanel(canvas);

        PlayerProgress.OnTotalGoldChanged += OnGoldChanged;
    }

    void OnDestroy()
    {
        PlayerProgress.OnTotalGoldChanged -= OnGoldChanged;
    }

    void OnGoldChanged(int total)
    {
        if (panel != null && panel.activeSelf)
        {
            Refresh();
        }
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

    void OnWeaponClicked(WeaponCatalog.Weapon weapon)
    {
        if (!WeaponCatalog.IsOwned(weapon))
        {
            if (!WeaponCatalog.TryBuy(weapon))
                return;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUpgradePurchase();
            }
        }
        else
        {
            UIBuilder.PlayClick();
        }

        // Alınan / tıklanan silah seçilir.
        PlayerProgress.SelectedWeapon = weapon.id;

        Refresh();
    }

    void OnUpgradeClicked(WeaponCatalog.Weapon weapon)
    {
        if (!WeaponCatalog.TryUpgrade(weapon))
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        Haptics.Light();
        Refresh();
    }

    void RefreshUpgrade(Row row, int gold)
    {
        WeaponCatalog.Weapon weapon = row.weapon;
        int level = WeaponCatalog.GetLevel(weapon);
        bool owned = WeaponCatalog.IsOwned(weapon);

        row.title.text =
            Loc.T(weapon.title) + "  " +
            UIBuilder.ColorTag(theme.greyLight, "<size=65%>" + Loc.T("SV. ") + level + "/" + WeaponCatalog.MaxLevel + "</size>");

        row.stats.text = GetStatsLine(weapon, level);

        Image image = (Image)row.upgradeButton.targetGraphic;

        if (!owned)
        {
            row.upgradeButton.gameObject.SetActive(false);
            return;
        }

        row.upgradeButton.gameObject.SetActive(true);

        if (level >= WeaponCatalog.MaxLevel)
        {
            theme.ApplyButton(row.upgradeButton, image, false);
            row.upgradeButton.interactable = false;
            row.upgradeText.text = Loc.T("MAKS");
            row.upgradeText.color = theme.mainRedHover;
            return;
        }

        int cost = WeaponCatalog.UpgradeCost(weapon);
        bool canAfford = gold >= cost;

        theme.ApplyButton(row.upgradeButton, image, false);
        row.upgradeButton.interactable = canAfford;
        row.upgradeText.text = Loc.T("GÜÇLENDİR") + "  " + cost;
        row.upgradeText.color = canAfford ? theme.mainWhite : theme.greyLight;
    }

    void Refresh()
    {
        int gold = PlayerProgress.TotalGold;
        WeaponCatalog.Weapon selected = WeaponCatalog.Selected;

        goldLabel.text = UIBuilder.ColorTag(theme.mainRedHover, Loc.T("ALTIN")) + "  " + gold;

        foreach (Row row in rows)
        {
            RefreshUpgrade(row, gold);

            Image image = (Image)row.button.targetGraphic;

            if (row.weapon == selected)
            {
                theme.ApplyButton(row.button, image, false);
                row.button.interactable = false;
                row.buttonText.text = Loc.T("SEÇİLİ");
                row.buttonText.color = theme.mainRedHover;
            }
            else if (WeaponCatalog.IsOwned(row.weapon))
            {
                theme.ApplyButton(row.button, image, false);
                row.button.interactable = true;
                row.buttonText.text = Loc.T("SEÇ");
                row.buttonText.color = theme.mainWhite;
            }
            else
            {
                bool canAfford = gold >= row.weapon.price;

                theme.ApplyButton(row.button, image, true);
                row.button.interactable = canAfford;
                row.buttonText.text = row.weapon.price + " " + Loc.T("ALTIN");
                row.buttonText.color = canAfford ? theme.mainWhite : theme.greyLight;
            }
        }
    }

    // Tüfeğe göre kısa karşılaştırma; güçlendirme seviyesi de hesaba katılır.
    static string GetStatsLine(WeaponCatalog.Weapon weapon, int level = 1)
    {
        float fireRate = 100f / weapon.fireIntervalMultiplier * (1f + WeaponCatalog.FireRatePercentAt(level) / 100f);
        float damage = weapon.damageMultiplier * 100f * (1f + WeaponCatalog.DamagePercentAt(level) / 100f);

        List<string> parts = new List<string>
        {
            Loc.Pick("Atış hızı %", "Fire rate ") + Mathf.RoundToInt(fireRate) + Loc.Pick("", "%"),
            Loc.Pick("Mermi hasarı %", "Damage ") + Mathf.RoundToInt(damage) + Loc.Pick("", "%")
        };

        if (weapon.pellets > 1)
            parts.Add(weapon.pellets + Loc.Pick(" saçma", " pellets"));

        if (weapon.extraPierce >= 100)
            parts.Add(Loc.Pick("Sınırsız delme", "Infinite pierce"));
        else if (weapon.extraPierce > 0)
            parts.Add("+" + weapon.extraPierce + Loc.Pick(" delme", " pierce"));

        if (weapon.bonusCritChance > 0f)
            parts.Add("+" + Loc.Pick("%" + Mathf.RoundToInt(weapon.bonusCritChance * 100f) + " kritik", Mathf.RoundToInt(weapon.bonusCritChance * 100f) + "% crit"));

        if (weapon.randomSpread >= 5f)
            parts.Add(Loc.Pick("Dağınık atış", "Wide spread"));

        if (weapon.explosionRadius > 0f)
            parts.Add(weapon.explosionRadius.ToString("0.#", CultureInfo.InvariantCulture) + Loc.Pick(" birim patlama", " unit blast"));

        if (weapon.lifetimeMultiplier < 0.5f)
            parts.Add(Loc.Pick("Kısa menzil", "Short range"));

        return string.Join("   ·   ", parts);
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "WeaponsPanel");
        panel = dim.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.15f, 0.06f),
            new Vector2(0.85f, 0.94f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "SİLAHLAR");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        goldLabel = UIBuilder.CreateText(theme, window, string.Empty, 40f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        UIBuilder.AnchorTop(goldLabel.rectTransform, 25f, 100f);
        goldLabel.rectTransform.offsetMin = new Vector2(70f, goldLabel.rectTransform.offsetMin.y);

        TextMeshProUGUI hint = UIBuilder.CreateText(
            theme,
            window,
            "Seçtiğin silah bir sonraki oyunda kullanılır.",
            28f,
            TextAlignmentOptions.Center,
            theme.greyLight
        );
        UIBuilder.AnchorTop(hint.rectTransform, 145f, 45f);

        RectTransform content = UIBuilder.CreateScrollList(window, new Vector2(60f, 45f), new Vector2(-60f, -200f));

        foreach (WeaponCatalog.Weapon weapon in WeaponCatalog.All)
        {
            rows.Add(CreateRow(content, weapon));
        }

        panel.SetActive(false);
    }

    Row CreateRow(RectTransform content, WeaponCatalog.Weapon weapon)
    {
        RectTransform rowRect = UIBuilder.CreateRect("Weapon_" + weapon.id, content);
        theme.ApplyNotice(rowRect.gameObject.AddComponent<Image>());
        rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 175f;

        // Mermi rengini gösteren küçük şerit
        RectTransform swatch = UIBuilder.CreateRect("Swatch", rowRect);
        UIBuilder.Place(swatch, new Vector2(0f, 0.5f), new Vector2(45f, 0f), new Vector2(14f, 110f));
        Image swatchImage = swatch.gameObject.AddComponent<Image>();
        swatchImage.color = weapon.tint;
        swatchImage.raycastTarget = false;

        TextMeshProUGUI title = UIBuilder.CreateText(theme, rowRect, weapon.title, 42f, TextAlignmentOptions.BottomLeft, theme.mainWhite);
        title.rectTransform.anchorMin = new Vector2(0f, 0.6f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(85f, 0f);
        title.rectTransform.offsetMax = new Vector2(-380f, -14f);

        TextMeshProUGUI description = UIBuilder.CreateText(theme, rowRect, weapon.description, 27f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        description.rectTransform.anchorMin = new Vector2(0f, 0.3f);
        description.rectTransform.anchorMax = new Vector2(1f, 0.6f);
        description.rectTransform.offsetMin = new Vector2(85f, 0f);
        description.rectTransform.offsetMax = new Vector2(-380f, 0f);
        description.enableAutoSizing = true;
        description.fontSizeMin = 18f;
        description.fontSizeMax = 27f;

        TextMeshProUGUI stats = UIBuilder.CreateText(theme, rowRect, GetStatsLine(weapon), 22f, TextAlignmentOptions.TopLeft, theme.greyLight);
        stats.rectTransform.anchorMin = new Vector2(0f, 0f);
        stats.rectTransform.anchorMax = new Vector2(1f, 0.3f);
        stats.rectTransform.offsetMin = new Vector2(85f, 14f);
        stats.rectTransform.offsetMax = new Vector2(-380f, 0f);
        stats.enableAutoSizing = true;
        stats.fontSizeMin = 16f;
        stats.fontSizeMax = 22f;

        Button button = UIBuilder.CreateButton(theme, rowRect, "ActionButton", string.Empty, true, out TextMeshProUGUI buttonText);
        UIBuilder.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-40f, 42f), new Vector2(300f, 76f));
        button.onClick.AddListener(() => OnWeaponClicked(weapon));

        // Güçlendirme: sahip olunan silah 5 seviyeye kadar altınla güçlenir.
        Button upgradeButton = UIBuilder.CreateButton(theme, rowRect, "UpgradeButton", string.Empty, false, out TextMeshProUGUI upgradeText);
        UIBuilder.Place((RectTransform)upgradeButton.transform, new Vector2(1f, 0.5f), new Vector2(-40f, -42f), new Vector2(300f, 76f));
        upgradeButton.onClick.AddListener(() => OnUpgradeClicked(weapon));

        return new Row
        {
            weapon = weapon,
            button = button,
            buttonText = buttonText,
            upgradeButton = upgradeButton,
            upgradeText = upgradeText,
            title = title,
            stats = stats
        };
    }
}
