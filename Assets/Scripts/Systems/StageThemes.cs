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

    private struct Theme
    {
        public string name;
        public Color tint;

        public Theme(string name, Color tint)
        {
            this.name = name;
            this.tint = tint;
        }
    }

    private static readonly Theme[] Themes =
    {
        new Theme("ÇORAK TOPRAKLAR", Color.white),
        new Theme("KIZIL ÇÖL", new Color(1f, 0.7f, 0.58f, 1f)),
        new Theme("ZEHİRLİ BATAKLIK", new Color(0.68f, 0.95f, 0.62f, 1f)),
        new Theme("BUZUL SINIR", new Color(0.68f, 0.86f, 1f, 1f)),
        new Theme("KARANLIK BÖLGE", new Color(0.6f, 0.52f, 0.8f, 1f))
    };

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

    public static int StageForLevel(int level) => Mathf.Max(0, (level - 1) / LevelsPerStage);

    // 1. bölüm ilk temadır; sonrakiler 2-5 arasında döner.
    static Theme ThemeForStage(int stage)
    {
        if (stage < Themes.Length)
            return Themes[stage];

        return Themes[1 + (stage - 1) % (Themes.Length - 1)];
    }

    public static void NotifyLevelUp(int level)
    {
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

        transition = StartCoroutine(Transition(target.tint));
        StartCoroutine(ShowBanner(stage, target.name));
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

    IEnumerator ShowBanner(int stage, string stageName)
    {
        // Seviye kartı kapanana kadar bekle
        while (TimeScaleController.IsPaused)
            yield return null;

        bannerTitle.text = Loc.T("BÖLÜM") + " " + (stage + 1);
        bannerName.text = Loc.T(stageName);
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
        UIBuilder.Place(banner, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(900f, 170f));

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

        banner.gameObject.SetActive(false);
    }
}
