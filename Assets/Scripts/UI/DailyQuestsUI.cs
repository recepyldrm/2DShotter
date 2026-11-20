using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki günlük görevler ekranı: her gün yenilenen 3 görev ve altın ödülleri.
// Alınabilir ödül varsa "GÖREVLER" butonunda "!" rozeti görünür.
public class DailyQuestsUI : MonoBehaviour
{
    private class Row
    {
        public TextMeshProUGUI title;
        public RectTransform progressFill;
        public TextMeshProUGUI progressText;
        public Button button;
        public TextMeshProUGUI buttonText;
        public CanvasGroup group;
    }

    private UITheme theme;
    private GameObject panel;
    private RectTransform badge;
    private TextMeshProUGUI timerText;
    private readonly List<Row> rows = new List<Row>();

    private string shownDate;
    private float timerRefresh;

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "DailyQuestsCanvas", 150);

        Button openButton = UIBuilder.CreateMenuButton(theme, canvas, "GÖREVLER", UIBuilder.MenuSlotQuests, out badge);
        openButton.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        BuildPanel(canvas);

        DailyQuests.OnQuestsChanged += OnQuestsChanged;
        UpdateBadge();
    }

    void OnDestroy()
    {
        DailyQuests.OnQuestsChanged -= OnQuestsChanged;
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        timerRefresh -= Time.unscaledDeltaTime;

        if (timerRefresh > 0f)
            return;

        timerRefresh = 1f;

        // Ekran açıkken gece yarısı geçerse yeni görevler gelir.
        if (shownDate != System.DateTime.Now.Date.ToString("yyyy-MM-dd"))
        {
            Refresh();
        }

        System.TimeSpan left = DailyQuests.TimeUntilReset;
        timerText.text = "Yeni görevler: " + string.Format("{0:00}:{1:00}:{2:00}", (int)left.TotalHours, left.Minutes, left.Seconds);
    }

    void OnQuestsChanged()
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
            badge.gameObject.SetActive(DailyQuests.ClaimableCount > 0);
        }
    }

    public void Show()
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

    void OnClaimClicked(int index)
    {
        IReadOnlyList<DailyQuests.Quest> quests = DailyQuests.Today;

        if (index >= quests.Count || DailyQuests.Claim(quests[index]) <= 0)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        Haptics.Light();
        Refresh();
    }

    void Refresh()
    {
        shownDate = System.DateTime.Now.Date.ToString("yyyy-MM-dd");
        IReadOnlyList<DailyQuests.Quest> quests = DailyQuests.Today;

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            bool exists = i < quests.Count;
            row.group.gameObject.SetActive(exists);

            if (!exists)
                continue;

            DailyQuests.Quest quest = quests[i];
            int progress = Mathf.Min(DailyQuests.GetProgress(quest), quest.target);
            bool completed = progress >= quest.target;
            bool claimed = DailyQuests.IsClaimed(quest);

            row.title.text = quest.Title;
            row.progressFill.anchorMax = new Vector2((float)progress / quest.target, 1f);
            row.progressText.text = progress + " / " + quest.target;

            string reward = "+" + quest.reward + " ALTIN";

            if (claimed)
            {
                theme.ApplyButton(row.button, (Image)row.button.targetGraphic, false);
                row.button.interactable = false;
                row.buttonText.text = "ALINDI";
                row.buttonText.color = theme.mainRedHover;
                row.group.alpha = 0.6f;
            }
            else if (completed)
            {
                theme.ApplyButton(row.button, (Image)row.button.targetGraphic, true);
                row.button.interactable = true;
                row.buttonText.text = "AL " + reward;
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

        UpdateBadge();
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "DailyQuestsPanel");
        panel = dim.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.18f, 0.14f),
            new Vector2(0.82f, 0.86f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "GÜNLÜK GÖREVLER");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        timerText = UIBuilder.CreateText(theme, window, string.Empty, 32f, TextAlignmentOptions.MidlineLeft, theme.greyLight);
        UIBuilder.AnchorTop(timerText.rectTransform, 25f, 100f);
        timerText.rectTransform.offsetMin = new Vector2(70f, timerText.rectTransform.offsetMin.y);

        RectTransform content = UIBuilder.CreateScrollList(window, new Vector2(60f, 45f), new Vector2(-60f, -150f));

        for (int i = 0; i < DailyQuests.QuestCount; i++)
        {
            rows.Add(CreateRow(content, i));
        }

        panel.SetActive(false);
    }

    Row CreateRow(RectTransform content, int index)
    {
        RectTransform rowRect = UIBuilder.CreateRect("Quest_" + index, content);
        theme.ApplyNotice(rowRect.gameObject.AddComponent<Image>());
        rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

        CanvasGroup group = rowRect.gameObject.AddComponent<CanvasGroup>();

        TextMeshProUGUI title = UIBuilder.CreateText(theme, rowRect, string.Empty, 40f, TextAlignmentOptions.BottomLeft, theme.mainWhite);
        title.rectTransform.anchorMin = new Vector2(0f, 0.45f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(60f, 0f);
        title.rectTransform.offsetMax = new Vector2(-380f, -18f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 24f;
        title.fontSizeMax = 40f;

        RectTransform bar = UIBuilder.CreateProgressBar(theme, rowRect, out RectTransform fill);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0f, 0f);
        bar.offsetMin = new Vector2(60f, 32f);
        bar.offsetMax = new Vector2(-560f, 48f);

        TextMeshProUGUI progressText = UIBuilder.CreateText(theme, rowRect, string.Empty, 24f, TextAlignmentOptions.MidlineLeft, theme.greyLight);
        progressText.rectTransform.anchorMin = new Vector2(1f, 0f);
        progressText.rectTransform.anchorMax = new Vector2(1f, 0f);
        progressText.rectTransform.pivot = new Vector2(0f, 0f);
        progressText.rectTransform.anchoredPosition = new Vector2(-545f, 23f);
        progressText.rectTransform.sizeDelta = new Vector2(170f, 34f);

        Button button = UIBuilder.CreateButton(theme, rowRect, "ClaimButton", string.Empty, false, out TextMeshProUGUI buttonText);
        UIBuilder.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(320f, 100f));
        button.onClick.AddListener(() => OnClaimClicked(index));

        return new Row
        {
            title = title,
            progressFill = fill,
            progressText = progressText,
            button = button,
            buttonText = buttonText,
            group = group
        };
    }
}
