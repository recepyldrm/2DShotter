using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bölümler: her LevelsPerStage seviyede arka plan katmanları ve kamera rengi
// yavaşça yeni bölümün rengine döner, ekranda bölüm adı görünür.
// Ek görsel gerektirmez; mevcut arka plan sprite'larını renklendirir.
public class StageThemes : MonoBehaviour
{
    public const int LevelsPerStage = 10;

    private const float TransitionDuration = 2.5f;

    // Bölüm: renk + oyun kuralı (düşman çarpanları ve özel düşman olasılıkları)
    private class Theme
    {
        public string name;
        public Color tint;

        public float health = 1f;
        public float speed = 1f;
        public float damage = 1f;
        public float rangedChance = 1f;
        public float shieldedChance = 1f;
        public float splitterChance = 1f;

        public string ruleTurkish = string.Empty;
        public string ruleEnglish = string.Empty;
    }

    private static readonly Theme[] Themes =
    {
        new Theme
        {
            name = "ÇORAK TOPRAKLAR",
            tint = Color.white
        },
        new Theme
        {
            name = "KIZIL ÇÖL",
            tint = new Color(1f, 0.7f, 0.58f, 1f),
            rangedChance = 2.2f,
            ruleTurkish = "Nişancılar çoğaldı",
            ruleEnglish = "More gunners"
        },
        new Theme
        {
            name = "ZEHİRLİ BATAKLIK",
            tint = new Color(0.68f, 0.95f, 0.62f, 1f),
            splitterChance = 2.2f,
            speed = 1.05f,
            ruleTurkish = "Bölünenler çoğaldı, düşmanlar biraz hızlı",
            ruleEnglish = "More splitters, enemies slightly faster"
        },
        new Theme
        {
            name = "BUZUL SINIR",
            tint = new Color(0.68f, 0.86f, 1f, 1f),
            speed = 0.8f,
            health = 1.25f,
            ruleTurkish = "Düşmanlar yavaş ama dayanıklı",
            ruleEnglish = "Enemies are slow but tough"
        },
        new Theme
        {
            name = "KARANLIK BÖLGE",
            tint = new Color(0.6f, 0.52f, 0.8f, 1f),
            shieldedChance = 2.2f,
            damage = 1.15f,
            ruleTurkish = "Kalkanlılar çoğaldı, düşmanlar daha sert vurur",
            ruleEnglish = "More shielded enemies, enemies hit harder"
        },
        new Theme
        {
            name = "KÜL VADİSİ",
            tint = new Color(0.88f, 0.76f, 0.66f, 1f),
            rangedChance = 1.5f,
            shieldedChance = 1.5f,
            splitterChance = 1.5f,
            health = 1.1f,
            ruleTurkish = "Her türden özel düşman çoğaldı",
            ruleEnglish = "More special enemies of every kind"
        }
    };

    // Şu anki bölümün kuralları (bölüm 1'de hepsi 1)
    private static Theme activeTheme = Themes[0];

    public static float HealthMultiplier => activeTheme.health;
    public static float SpeedMultiplier => activeTheme.speed;
    public static float DamageMultiplier => activeTheme.damage;
    public static float RangedChanceMultiplier => activeTheme.rangedChance;
    public static float ShieldedChanceMultiplier => activeTheme.shieldedChance;
    public static float SplitterChanceMultiplier => activeTheme.splitterChance;

    public static string StageName(int stage) => Loc.T(ThemeForStage(stage).name);

    private static StageThemes instance;

    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private readonly List<Color> originalColors = new List<Color>();
    private Camera mainCamera;
    private Color originalCameraColor;

    private Color currentTint = Color.white;
    private int currentStage;
    private Coroutine transition;

    private UITheme theme;
    private CanvasGroup bannerGroup;
    private TextMeshProUGUI bannerTitle;
    private TextMeshProUGUI bannerName;
    private TextMeshProUGUI bannerRule;

    public static int StageForLevel(int level) => Mathf.Max(0, (level - 1) / LevelsPerStage);

    // 1. bölüm ilk temadır; sonrakiler 2-5 arasında döner.
    static Theme ThemeForStage(int stage)
    {
        if (stage < Themes.Length)
            return Themes[stage];

        return Themes[1 + (stage - 1) % (Themes.Length - 1)];
    }

    public static int ThemeCount => Themes.Length;

    // Günlük meydan okuma: oyun baştan bu bölümün renk ve kurallarıyla oynanır,
    // seviye atladıkça bölüm değişmez.
    private bool fixedStage;

    public static void StartFixed(int themeIndex)
    {
        if (instance == null)
        {
            instance = new GameObject("StageThemes").AddComponent<StageThemes>();
        }

        Theme target = Themes[Mathf.Clamp(themeIndex, 0, Themes.Length - 1)];

        instance.fixedStage = true;
        instance.currentStage = themeIndex;
        activeTheme = target;
        instance.ApplyTint(target.tint);
        instance.StartCoroutine(instance.ShowChallengeBanner(target));
    }

    IEnumerator ShowChallengeBanner(Theme target)
    {
        yield return new WaitForSeconds(0.5f);

        bannerTitle.text = Loc.Pick("GÜNLÜK MEYDAN OKUMA", "DAILY CHALLENGE");
        bannerName.text = Loc.T(target.name);
        bannerRule.text = Loc.Pick(target.ruleTurkish, target.ruleEnglish) + "  ·  " +
                          DailyChallenge.ModifierName(DailyChallenge.TodayModifier);

        yield return FadeBanner();
    }

    public static void NotifyLevelUp(int level)
    {
        if (instance != null && instance.fixedStage)
            return;

        int stage = StageForLevel(level);

        if (stage == 0 && instance == null)
            return;

        if (instance == null)
        {
            instance = new GameObject("StageThemes").AddComponent<StageThemes>();
        }

        instance.GoToStage(stage);
    }

    void Awake()
    {
        theme = UITheme.Load();
        CollectRenderers();
        BuildBanner();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;

            // Yeni oyunda bölüm 1 kurallarıyla başla
            activeTheme = Themes[0];
        }
    }

    // Arka plan: ParallaxLayer altındaki bütün sprite'lar
    void CollectRenderers()
    {
        foreach (ParallaxLayer layer in FindObjectsByType<ParallaxLayer>())
        {
            foreach (SpriteRenderer renderer in layer.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderers.Contains(renderer))
                    continue;

                renderers.Add(renderer);
                originalColors.Add(renderer.color);
            }
        }

        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            originalCameraColor = mainCamera.backgroundColor;
        }
    }

    void GoToStage(int stage)
    {
        if (stage == currentStage)
            return;

        currentStage = stage;
        Theme target = ThemeForStage(stage);

        if (transition != null)
        {
            StopCoroutine(transition);
        }

        activeTheme = target;
        transition = StartCoroutine(Transition(target.tint));
        StartCoroutine(ShowBanner(stage, target));
    }

    IEnumerator Transition(Color targetTint)
    {
        Color startTint = currentTint;
        float elapsed = 0f;

        while (elapsed < TransitionDuration)
        {
            // Kart ekranı açıkken de renk değişimi sürsün
            elapsed += Time.unscaledDeltaTime;
            ApplyTint(Color.Lerp(startTint, targetTint, Mathf.SmoothStep(0f, 1f, elapsed / TransitionDuration)));
            yield return null;
        }

        ApplyTint(targetTint);
        transition = null;
    }

    void ApplyTint(Color tint)
    {
        currentTint = tint;

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = originalColors[i] * tint;
            }
        }

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = originalCameraColor * tint;
        }
    }

    // ==================================
    // BÖLÜM ADI
    // ==================================

    IEnumerator ShowBanner(int stage, Theme stageTheme)
    {
        // Seviye kartı kapanana kadar bekle
        while (TimeScaleController.IsPaused)
            yield return null;

        bannerTitle.text = Loc.T("BÖLÜM") + " " + (stage + 1);
        bannerName.text = Loc.T(stageTheme.name);
        bannerRule.text = Loc.Pick(stageTheme.ruleTurkish, stageTheme.ruleEnglish);

        yield return FadeBanner();
    }

    IEnumerator FadeBanner()
    {
        bannerGroup.gameObject.SetActive(true);

        const float fade = 0.5f;
        const float hold = 2.2f;
        float elapsed = 0f;

        while (elapsed < fade * 2f + hold)
        {
            elapsed += Time.unscaledDeltaTime;

            float alpha = elapsed < fade
                ? elapsed / fade
                : elapsed > fade + hold ? 1f - (elapsed - fade - hold) / fade : 1f;

            bannerGroup.alpha = Mathf.Clamp01(alpha);
            yield return null;
        }

        bannerGroup.gameObject.SetActive(false);
    }

    void BuildBanner()
    {
        Transform canvas = UIBuilder.CreateCanvas(transform, "StageCanvas", 380);
        RectTransform safeRoot = UIBuilder.CreateSafeRoot(canvas);

        RectTransform banner = UIBuilder.CreateRect("StageBanner", safeRoot);
        UIBuilder.Place(banner, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(900f, 215f));

        Image background = banner.gameObject.AddComponent<Image>();
        theme.ApplyNotice(background);
        background.raycastTarget = false;

        bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;

        bannerTitle = UIBuilder.CreateText(theme, banner, string.Empty, 34f, TextAlignmentOptions.Center, theme.greyLight);
        UIBuilder.AnchorTop(bannerTitle.rectTransform, 18f, 45f);

        bannerName = UIBuilder.CreateText(theme, banner, string.Empty, 64f, TextAlignmentOptions.Center, theme.mainRedHover);
        UIBuilder.AnchorTop(bannerName.rectTransform, 62f, 85f);

        bannerRule = UIBuilder.CreateText(theme, banner, string.Empty, 28f, TextAlignmentOptions.Center, theme.mainWhite);
        UIBuilder.AnchorTop(bannerRule.rectTransform, 150f, 45f);

        banner.gameObject.SetActive(false);
    }
}
