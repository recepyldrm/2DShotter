using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Oyun sonu özeti: ekranın sağında, Game Over paneliyle birlikte görünür.
// Ulaşılan seviye / bölüm, öldürülen düşmanlar, bosslar, en çok hasar veren
// kaynak ve altın dökümü. Kendiliğinden oluşur.
public class RunSummaryUI : MonoBehaviour
{
    private static RunSummaryUI instance;

    private UITheme theme;
    private GameObject root;
    private TextMeshProUGUI bodyText;

    // Son gösterilen değerler (2x altın sonrası yenilemek için)
    private int score;
    private int level;
    private int kills;
    private int gold;
    private int doubleGoldBonus;
    private float seconds;

    public static void Show(int score, int level, int kills, int gold, float seconds)
    {
        if (instance == null)
        {
            instance = new GameObject("RunSummaryUI").AddComponent<RunSummaryUI>();
        }

        instance.score = score;
        instance.level = level;
        instance.kills = kills;
        instance.gold = gold;
        instance.seconds = seconds;
        instance.doubleGoldBonus = 0;
        instance.Refresh();
        instance.root.SetActive(true);
    }

    public static void Hide()
    {
        if (instance != null)
            instance.root.SetActive(false);
    }

    public static void SetDoubleGoldBonus(int bonus)
    {
        if (instance == null || !instance.root.activeSelf)
            return;

        instance.doubleGoldBonus = bonus;
        instance.Refresh();
    }

    void Awake()
    {
        theme = UITheme.Load();
        Build();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Refresh()
    {
        StringBuilder text = new StringBuilder();
        string accent = ColorUtility.ToHtmlStringRGB(theme.mainRedHover);
        string dim = ColorUtility.ToHtmlStringRGB(theme.greyLight);

        int stage = StageThemes.StageForLevel(Mathf.Max(1, level)) + 1;
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);

        Line(text, Loc.Pick("Seviye", "Level"), level.ToString());
        Line(text, Loc.Pick("Bölüm", "Stage"), stage + "  <size=75%><color=#" + dim + ">" + StageThemes.StageName(stage - 1) + "</color></size>");
        Line(text, Loc.Pick("Süre", "Time"), minutes + ":" + secs.ToString("00"));
        Line(text, Loc.Pick("Zorluk", "Difficulty"), Difficulty.Label(Difficulty.Current));

        text.Append('\n');

        Line(text, Loc.Pick("Öldürülen düşman", "Enemies killed"), kills.ToString());

        if (RunStats.RangedKills + RunStats.ShieldedKills + RunStats.SplitterKills > 0)
        {
            text.Append("<size=80%><color=#").Append(dim).Append(">   ")
                .Append(Loc.Pick("Nişancı ", "Gunners ")).Append(RunStats.RangedKills)
                .Append(Loc.Pick("  ·  Kalkanlı ", "  ·  Shielded ")).Append(RunStats.ShieldedKills)
                .Append(Loc.Pick("  ·  Bölünen ", "  ·  Splitters ")).Append(RunStats.SplitterKills)
                .Append("</color></size>\n");
        }

        Line(text, Loc.Pick("Yenilen boss", "Bosses defeated"), RunStats.BossKills.ToString());

        if (RunStats.BossStuns > 0)
            Line(text, Loc.Pick("Sersemletme", "Boss stuns"), RunStats.BossStuns.ToString());

        text.Append('\n');

        // En çok hasar veren kaynak ve payı
        long total = RunStats.TotalDamage;

        if (total > 0)
        {
            RunStats.DamageSource top = RunStats.TopSource;
            int share = Mathf.RoundToInt(100f * RunStats.GetDamage(top) / total);

            text.Append("<color=#").Append(dim).Append(">")
                .Append(Loc.Pick("En çok hasar veren", "Top damage source"))
                .Append("</color>\n<color=#").Append(accent).Append(">")
                .Append(RunStats.SourceName(top)).Append("</color>  ")
                .Append(Loc.IsEnglish ? share + "%" : "%" + share)
                .Append("\n\n");
        }

        // Altın dökümü
        text.Append("<color=#").Append(dim).Append(">").Append(Loc.Pick("Altın", "Gold")).Append("</color>\n");
        Line(text, Loc.Pick("Düşmanlardan", "From enemies"), RunStats.KillGold.ToString());

        int other = Mathf.Max(0, gold - RunStats.KillGold);
        if (other > 0)
            Line(text, Loc.Pick("Diğer", "Other"), other.ToString());

        if (doubleGoldBonus > 0)
            Line(text, Loc.Pick("Reklam (x2)", "Ad bonus (x2)"), "+" + doubleGoldBonus);

        if (Difficulty.Current != Difficulty.Level.Normal)
        {
            int percent = Mathf.RoundToInt(Difficulty.GoldMultiplier * 100f);
            text.Append("<size=80%><color=#").Append(dim).Append(">   ")
                .Append(Loc.Pick("Zorluk çarpanı ", "Difficulty multiplier "))
                .Append(Loc.IsEnglish ? percent + "%" : "%" + percent)
                .Append("</color></size>\n");
        }

        Line(text, "<b>" + Loc.Pick("TOPLAM", "TOTAL") + "</b>", "<color=#" + accent + "><b>" + (gold + doubleGoldBonus) + "</b></color>");

        bodyText.text = text.ToString();
    }

    static void Line(StringBuilder text, string label, string value)
    {
        text.Append(label).Append("<pos=62%>").Append(value).Append('\n');
    }

    void Build()
    {
        Transform canvas = UIBuilder.CreateCanvas(transform, "RunSummaryCanvas", 480);
        RectTransform safeRoot = UIBuilder.CreateSafeRoot(canvas);

        RectTransform panel = UIBuilder.CreateRect("RunSummary", safeRoot);
        // Ortadaki reklam butonlarıyla (±460) çakışmasın diye dar ve sağa yaslı.
        UIBuilder.Place(panel, new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(450f, 800f));

        Image background = panel.gameObject.AddComponent<Image>();
        theme.ApplyNotice(background);
        background.raycastTarget = false;

        TextMeshProUGUI title = UIBuilder.CreateText(theme, panel, Loc.Pick("OYUN ÖZETİ", "RUN SUMMARY"), 44f, TextAlignmentOptions.Center, theme.mainWhite);
        UIBuilder.AnchorTop(title.rectTransform, 22f, 60f);

        RectTransform divider = UIBuilder.CreateRect("Divider", panel);
        divider.anchorMin = new Vector2(0.08f, 1f);
        divider.anchorMax = new Vector2(0.92f, 1f);
        divider.pivot = new Vector2(0.5f, 1f);
        divider.offsetMin = new Vector2(0f, -92f);
        divider.offsetMax = new Vector2(0f, -89f);
        divider.gameObject.AddComponent<Image>().color = theme.mainRed;

        bodyText = UIBuilder.CreateText(theme, panel, string.Empty, 28f, TextAlignmentOptions.TopLeft, theme.mainWhite);
        bodyText.rectTransform.anchorMin = Vector2.zero;
        bodyText.rectTransform.anchorMax = Vector2.one;
        bodyText.rectTransform.offsetMin = new Vector2(40f, 30f);
        bodyText.rectTransform.offsetMax = new Vector2(-40f, -110f);
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 18f;
        bodyText.fontSizeMax = 28f;
        bodyText.richText = true;

        root = canvas.gameObject;
        root.SetActive(false);
    }
}
