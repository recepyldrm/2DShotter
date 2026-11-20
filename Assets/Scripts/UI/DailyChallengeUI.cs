using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menünün sol altındaki GÜNLÜK MEYDAN OKUMA butonu ve penceresi:
// günün silahı, bölümü, ek kuralı, günün en iyi skoru ve BAŞLA.
// Bugün henüz oynanmadıysa butonda "!" rozeti görünür.
public class DailyChallengeUI : MonoBehaviour
{
    private UITheme theme;
    private GameObject panel;
    private RectTransform badge;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI timerText;
    private float timerRefresh;

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "DailyChallengeCanvas", 145);
        RectTransform safeRoot = UIBuilder.CreateSafeRoot(canvas);

        Button open = UIBuilder.CreateButton(theme, safeRoot, "ChallengeButton", Loc.Pick("GÜNLÜK MEYDAN OKUMA", "DAILY CHALLENGE"), true, out _);
        UIBuilder.Place((RectTransform)open.transform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(440f, 95f));
        open.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        badge = UIBuilder.CreateBadge(theme, (RectTransform)open.transform);
        badge.gameObject.SetActive(!DailyChallenge.PlayedToday);

        BuildPanel(canvas);
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        timerRefresh -= Time.unscaledDeltaTime;

        if (timerRefresh > 0f)
            return;

        timerRefresh = 1f;

        System.TimeSpan left = DailyChallenge.TimeUntilNext;
        timerText.text = Loc.Pick("Yeni meydan okuma: ", "Next challenge: ") +
                         string.Format("{0:00}:{1:00}:{2:00}", (int)left.TotalHours, left.Minutes, left.Seconds);
    }

    void Show()
    {
        Refresh();
        timerRefresh = 0f;
        panel.SetActive(true);
    }

    void Close()
    {
        UIBuilder.PlayClick();
        panel.SetActive(false);
    }

    void Refresh()
    {
        string accent = ColorUtility.ToHtmlStringRGB(theme.mainRedHover);
        string dim = ColorUtility.ToHtmlStringRGB(theme.greyLight);

        WeaponCatalog.Weapon weapon = DailyChallenge.TodayWeapon;
        int stage = DailyChallenge.TodayStage;
        DailyChallenge.Modifier modifier = DailyChallenge.TodayModifier;

        string line(string label, string value) =>
            "<color=#" + dim + ">" + label + "</color>\n" + value + "\n\n";

        bodyText.text =
            line(Loc.Pick("SİLAH", "WEAPON"), "<color=#" + accent + ">" + Loc.T(weapon.title) + "</color>") +
            line(Loc.Pick("BÖLÜM", "STAGE"), StageThemes.StageName(stage)) +
            line(Loc.Pick("EK KURAL", "MODIFIER"), "<color=#" + accent + ">" + DailyChallenge.ModifierName(modifier) + "</color>  " +
                 "<size=80%>" + DailyChallenge.ModifierDescription(modifier) + "</size>") +
            "<size=80%><color=#" + dim + ">" +
            Loc.Pick(
                "Kalıcı geliştirmeler, silah seviyeleri ve drone yetenekleri kapalıdır. Herkes aynı şartlarda yarışır.",
                "Permanent upgrades, weapon levels and drone abilities are off. Everyone competes on equal terms.") +
            "</color></size>\n\n" +
            (DailyChallenge.PlayedToday
                ? Loc.Pick("Günün en iyi skorun: ", "Your best today: ") + "<color=#" + accent + ">" + DailyChallenge.BestToday + "</color>"
                : Loc.Pick("Günün ilk oyunu: ", "First run today: ") + "<color=#" + accent + ">+" + DailyChallenge.ParticipationReward + " " + Loc.T("ALTIN") + "</color>");
    }

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "ChallengePanel");
        panel = dim.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.25f, 0.08f),
            new Vector2(0.75f, 0.92f)
        );

        UIBuilder.CreateWindowTitle(theme, window, Loc.Pick("GÜNLÜK MEYDAN OKUMA", "DAILY CHALLENGE"));
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        timerText = UIBuilder.CreateText(theme, window, string.Empty, 28f, TextAlignmentOptions.Center, theme.greyLight);
        UIBuilder.AnchorTop(timerText.rectTransform, 150f, 45f);

        bodyText = UIBuilder.CreateText(theme, window, string.Empty, 36f, TextAlignmentOptions.Top, theme.mainWhite);
        bodyText.rectTransform.anchorMin = Vector2.zero;
        bodyText.rectTransform.anchorMax = Vector2.one;
        bodyText.rectTransform.offsetMin = new Vector2(70f, 170f);
        bodyText.rectTransform.offsetMax = new Vector2(-70f, -215f);
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 20f;
        bodyText.fontSizeMax = 36f;

        Button start = UIBuilder.CreateButton(theme, window, "StartChallenge", Loc.Pick("BAŞLA", "START"), true, out _);
        UIBuilder.Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(380f, 100f));
        start.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            DailyChallenge.Begin();
        });

        panel.SetActive(false);
    }
}
