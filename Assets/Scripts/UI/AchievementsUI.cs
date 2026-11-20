using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki başarımlar ekranı: ilerleme çubukları ve ödül alma.
// Alınabilir ödül varsa "BAŞARIMLAR" butonunda "!" rozeti görünür.
public class AchievementsUI : MonoBehaviour
{
    private class Row
    {
        public AchievementCatalog.Achievement achievement;
        public RectTransform progressFill;
        public TextMeshProUGUI progressText;
        public Button button;
        public TextMeshProUGUI buttonText;
        public CanvasGroup group;
    }

    private UITheme theme;
    private GameObject panel;
    private RectTransform badge;
    private TextMeshProUGUI summaryText;
    private readonly List<Row> rows = new List<Row>();

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "AchievementsCanvas", 150);

        Button openButton = UIBuilder.CreateMenuButton(theme, canvas, "BAŞARIMLAR", UIBuilder.MenuSlotAchievements, out badge);
        openButton.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        BuildPanel(canvas);

        PlayerProgress.OnProgressChanged += OnProgressChanged;
        UpdateBadge();
    }

    void OnDestroy()
    {
        PlayerProgress.OnProgressChanged -= OnProgressChanged;
    }

    void OnProgressChanged()
    {
        UpdateBadge();

        if (panel != null && panel.activeSelf)
        {
            Refresh();
        }
    }

    void UpdateBadge()
    {
        if (badge != null)
        {
            badge.gameObject.SetActive(AchievementCatalog.ClaimableCount > 0);
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

    void OnClaimClicked(AchievementCatalog.Achievement achievement)
    {
        if (!AchievementCatalog.Claim(achievement))
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayReward();
        }

        // Ödül skin ise otomatik seç.
        if (achievement.HasSkinReward)
        {
            PlayerProgress.SelectedCompanionSkin = achievement.skinReward;
        }

        Refresh();
    }

    void Refresh()
    {
        int completed = 0;

        foreach (Row row in rows)
        {
            AchievementCatalog.Achievement achievement = row.achievement;

            int progress = Mathf.Min(AchievementCatalog.GetProgress(achievement), achievement.target);
            bool isCompleted = progress >= achievement.target;
            bool isClaimed = AchievementCatalog.IsClaimed(achievement);

            if (isCompleted)
                completed++;

            row.progressFill.anchorMax = new Vector2((float)progress / achievement.target, 1f);
            row.progressText.text = progress + " / " + achievement.target;

            string reward = GetRewardLabel(achievement);

            if (isClaimed)
            {
                theme.ApplyButton(row.button, (Image)row.button.targetGraphic, false);
                row.button.interactable = false;
                row.buttonText.text = Loc.T("ALINDI");
                row.buttonText.color = theme.mainRedHover;
                row.group.alpha = 0.6f;
            }
            else if (isCompleted)
            {
                theme.ApplyButton(row.button, (Image)row.button.targetGraphic, true);
                row.button.interactable = true;
                row.buttonText.text = Loc.Pick("AL ", "CLAIM ") + reward;
                row.buttonText.color = theme.mainWhite;
                row.group.alpha = 1f;
            }
            else
            {
                theme.ApplyButton(row.button, (Image)row.button.targetGraphic, false);
                row.button.interactable = false;
                row.buttonText.text = reward;
                row.buttonText.color = theme.greyLight;
                row.group.alpha = 1f;
            }
        }

        summaryText.text =
            UIBuilder.ColorTag(theme.mainRedHover, completed.ToString()) +
            " / " + rows.Count + Loc.Pick(" TAMAMLANDI", " COMPLETED");

        UpdateBadge();
    }

    static string GetRewardLabel(AchievementCatalog.Achievement achievement)
    {
        if (achievement.HasSkinReward)
        {
            CompanionSkinDatabase database = CompanionSkinDatabase.Load();
            CompanionSkinDatabase.Skin skin = database != null ? database.Find(achievement.skinReward) : null;

            string skinName = skin != null && !string.IsNullOrEmpty(skin.displayName)
                ? skin.displayName
                : achievement.skinReward;

            return Loc.Pick("GÖRÜNÜM: ", "SKIN: ") + skinName;
        }

        return "+" + achievement.goldReward + " " + Loc.T("ALTIN");
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "AchievementsPanel");
        panel = dim.gameObject;

        RectTransform safeRoot = UIBuilder.CreateSafeRoot(dim);

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            safeRoot,
            new Vector2(0.15f, 0.06f),
            new Vector2(0.85f, 0.94f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "BAŞARIMLAR");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        summaryText = UIBuilder.CreateText(theme, window, string.Empty, 36f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        UIBuilder.AnchorTop(summaryText.rectTransform, 25f, 100f);
        summaryText.rectTransform.offsetMin = new Vector2(70f, summaryText.rectTransform.offsetMin.y);

        RectTransform content = UIBuilder.CreateScrollList(window, new Vector2(60f, 45f), new Vector2(-60f, -150f));

        foreach (AchievementCatalog.Achievement achievement in AchievementCatalog.All)
        {
            rows.Add(CreateRow(content, achievement));
        }

        panel.SetActive(false);
    }

    Row CreateRow(RectTransform content, AchievementCatalog.Achievement achievement)
    {
        RectTransform rowRect = UIBuilder.CreateRect("Achievement_" + achievement.id, content);
        theme.ApplyNotice(rowRect.gameObject.AddComponent<Image>());
        rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;

        CanvasGroup group = rowRect.gameObject.AddComponent<CanvasGroup>();

        // Başlık
        TextMeshProUGUI title = UIBuilder.CreateText(theme, rowRect, achievement.title, 40f, TextAlignmentOptions.BottomLeft, theme.mainWhite);
        title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(60f, 10f);
        title.rectTransform.offsetMax = new Vector2(-380f, -18f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 24f;
        title.fontSizeMax = 40f;

        // Açıklama
        TextMeshProUGUI description = UIBuilder.CreateText(theme, rowRect, achievement.description, 28f, TextAlignmentOptions.TopLeft, theme.greyLight);
        description.rectTransform.anchorMin = new Vector2(0f, 0f);
        description.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        description.rectTransform.offsetMin = new Vector2(60f, 50f);
        description.rectTransform.offsetMax = new Vector2(-380f, -4f);

        // İlerleme çubuğu
        RectTransform bar = UIBuilder.CreateProgressBar(theme, rowRect, out RectTransform fill);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0f, 0f);
        bar.offsetMin = new Vector2(60f, 30f);
        bar.offsetMax = new Vector2(-560f, 44f);

        TextMeshProUGUI progressText = UIBuilder.CreateText(theme, rowRect, string.Empty, 24f, TextAlignmentOptions.MidlineLeft, theme.greyLight);
        progressText.rectTransform.anchorMin = new Vector2(1f, 0f);
        progressText.rectTransform.anchorMax = new Vector2(1f, 0f);
        progressText.rectTransform.pivot = new Vector2(0f, 0f);
        progressText.rectTransform.anchoredPosition = new Vector2(-545f, 20f);
        progressText.rectTransform.sizeDelta = new Vector2(170f, 34f);

        // Ödül / al butonu
        Button button = UIBuilder.CreateButton(theme, rowRect, "ClaimButton", string.Empty, false, out TextMeshProUGUI buttonText);
        UIBuilder.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(320f, 100f));
        button.onClick.AddListener(() => OnClaimClicked(achievement));

        return new Row
        {
            achievement = achievement,
            progressFill = fill,
            progressText = progressText,
            button = button,
            buttonText = buttonText,
            group = group
        };
    }
}
