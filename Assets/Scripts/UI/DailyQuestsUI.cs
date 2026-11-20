using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki görevler ekranı: GÜNLÜK ve HAFTALIK sekmeleri.
// Haftalık görevlerin üçü de alınınca haftalık sandık açılır.
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
    private Row chestRow;

    private Button dailyTab;
    private Button weeklyTab;
    private RectTransform dailyTabBadge;
    private RectTransform weeklyTabBadge;

    private QuestBoard board = QuestBoard.Daily;
    private string shownPeriod;
    private float timerRefresh;

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "QuestsCanvas", 150);

        Button openButton = UIBuilder.CreateMenuButton(theme, canvas, "GÖREVLER", UIBuilder.MenuSlotQuests, out badge);
        openButton.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        BuildPanel(canvas);

        QuestBoard.OnChanged += OnQuestsChanged;
        UpdateBadges();
    }

    void OnDestroy()
    {
        QuestBoard.OnChanged -= OnQuestsChanged;
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        timerRefresh -= Time.unscaledDeltaTime;

        if (timerRefresh > 0f)
            return;

        timerRefresh = 1f;

        // Ekran açıkken dönem değişirse yeni görevler gelir.
        if (shownPeriod != board.Period)
        {
            Refresh();
        }

        System.TimeSpan left = board.TimeUntilReset;
        string time = left.TotalDays >= 1
            ? Loc.Pick((int)left.TotalDays + " gün " + left.Hours + " sa", (int)left.TotalDays + "d " + left.Hours + "h")
            : string.Format("{0:00}:{1:00}:{2:00}", (int)left.TotalHours, left.Minutes, left.Seconds);

        timerText.text = Loc.Pick("Yeni görevler: ", "New quests in: ") + time;
    }

    void OnQuestsChanged()
    {
        UpdateBadges();

        if (panel != null && panel.activeSelf)
        {
            Refresh();
        }
    }

    void UpdateBadges()
    {
        if (badge != null)
            badge.gameObject.SetActive(QuestBoard.TotalClaimable > 0);

        if (dailyTabBadge != null)
            dailyTabBadge.gameObject.SetActive(QuestBoard.Daily.ClaimableCount > 0);

        if (weeklyTabBadge != null)
            weeklyTabBadge.gameObject.SetActive(QuestBoard.Weekly.ClaimableCount > 0);
    }

    public void Show()
    {
        // Günlükte alınacak bir şey yoksa ama haftalıkta varsa haftalıkla aç.
        board = QuestBoard.Daily.ClaimableCount == 0 && QuestBoard.Weekly.ClaimableCount > 0
            ? QuestBoard.Weekly
            : QuestBoard.Daily;

        Refresh();
        timerRefresh = 0f;
        panel.SetActive(true);
    }

    void Close()
    {
        UIBuilder.PlayClick();
        panel.SetActive(false);
    }

    void SelectBoard(QuestBoard selected)
    {
        UIBuilder.PlayClick();
        board = selected;
        timerRefresh = 0f;
        Refresh();
    }

    void OnClaimClicked(int index)
    {
        IReadOnlyList<QuestBoard.Quest> quests = board.Current;

        if (index >= quests.Count || board.Claim(quests[index]) <= 0)
            return;

        PlayReward();
    }

    void OnChestClicked()
    {
        if (board.ClaimChest() <= 0)
            return;

        PlayReward();
        Haptics.Heavy();
    }

    void PlayReward()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradePurchase();
        }

        Haptics.Light();
        Refresh();
    }

    void Refresh()
    {
        shownPeriod = board.Period;
        IReadOnlyList<QuestBoard.Quest> quests = board.Current;

        theme.ApplyButton(dailyTab, (Image)dailyTab.targetGraphic, board == QuestBoard.Daily);
        theme.ApplyButton(weeklyTab, (Image)weeklyTab.targetGraphic, board == QuestBoard.Weekly);

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            bool exists = i < quests.Count;
            row.group.gameObject.SetActive(exists);

            if (!exists)
                continue;

            QuestBoard.Quest quest = quests[i];
            int progress = Mathf.Min(board.GetProgress(quest), quest.target);

            row.title.text = quest.Title;
            row.progressFill.anchorMax = new Vector2((float)progress / quest.target, 1f);
            row.progressText.text = progress + " / " + quest.target;

            ApplyState(row, progress >= quest.target, board.IsClaimed(quest), "+" + quest.reward + " " + Loc.T("ALTIN"));
        }

        // Haftalık sandık
        chestRow.group.gameObject.SetActive(board.HasChest);

        if (board.HasChest)
        {
            int claimed = 0;
            foreach (QuestBoard.Quest quest in quests)
            {
                if (board.IsClaimed(quest))
                    claimed++;
            }

            chestRow.title.text = Loc.Pick("HAFTALIK SANDIK", "WEEKLY CHEST") +
                UIBuilder.ColorTag(theme.greyLight, Loc.Pick("  <size=70%>(üç görevin ödülünü al)</size>", "  <size=70%>(claim all three quests)</size>"));
            chestRow.progressFill.anchorMax = new Vector2((float)claimed / QuestBoard.QuestCount, 1f);
            chestRow.progressText.text = claimed + " / " + QuestBoard.QuestCount;

            ApplyState(chestRow, board.AllClaimed, board.IsChestClaimed, "+" + board.chestReward + " " + Loc.T("ALTIN"));
        }

        UpdateBadges();
    }

    void ApplyState(Row row, bool completed, bool claimed, string reward)
    {
        Image image = (Image)row.button.targetGraphic;

        if (claimed)
        {
            theme.ApplyButton(row.button, image, false);
            row.button.interactable = false;
            row.buttonText.text = Loc.T("ALINDI");
            row.buttonText.color = theme.mainRedHover;
            row.group.alpha = 0.6f;
        }
        else if (completed)
        {
            theme.ApplyButton(row.button, image, true);
            row.button.interactable = true;
            row.buttonText.text = Loc.Pick("AL ", "CLAIM ") + reward;
            row.buttonText.color = theme.mainWhite;
            row.group.alpha = 1f;
        }
        else
        {
            theme.ApplyButton(row.button, image, false);
            row.button.interactable = false;
            row.buttonText.text = reward;
            row.buttonText.color = theme.greyLight;
            row.group.alpha = 1f;
        }
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "QuestsPanel");
        panel = dim.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.16f, 0.06f),
            new Vector2(0.84f, 0.94f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "GÖREVLER");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        // Sekmeler
        dailyTab = UIBuilder.CreateButton(theme, window, "DailyTab", "GÜNLÜK", true, out _);
        UIBuilder.Place((RectTransform)dailyTab.transform, new Vector2(0.5f, 1f), new Vector2(-165f, -195f), new Vector2(310f, 80f));
        dailyTab.onClick.AddListener(() => SelectBoard(QuestBoard.Daily));
        dailyTabBadge = UIBuilder.CreateBadge(theme, (RectTransform)dailyTab.transform);

        weeklyTab = UIBuilder.CreateButton(theme, window, "WeeklyTab", "HAFTALIK", false, out _);
        UIBuilder.Place((RectTransform)weeklyTab.transform, new Vector2(0.5f, 1f), new Vector2(165f, -195f), new Vector2(310f, 80f));
        weeklyTab.onClick.AddListener(() => SelectBoard(QuestBoard.Weekly));
        weeklyTabBadge = UIBuilder.CreateBadge(theme, (RectTransform)weeklyTab.transform);

        timerText = UIBuilder.CreateText(theme, window, string.Empty, 28f, TextAlignmentOptions.Center, theme.greyLight);
        UIBuilder.AnchorTop(timerText.rectTransform, 245f, 45f);

        RectTransform content = UIBuilder.CreateScrollList(window, new Vector2(60f, 40f), new Vector2(-60f, -300f));

        for (int i = 0; i < QuestBoard.QuestCount; i++)
        {
            int index = i;
            rows.Add(CreateRow(content, "Quest_" + i, () => OnClaimClicked(index)));
        }

        chestRow = CreateRow(content, "WeeklyChest", OnChestClicked);

        panel.SetActive(false);
    }

    Row CreateRow(RectTransform content, string name, UnityEngine.Events.UnityAction onClaim)
    {
        RectTransform rowRect = UIBuilder.CreateRect(name, content);
        theme.ApplyNotice(rowRect.gameObject.AddComponent<Image>());
        rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 140f;

        CanvasGroup group = rowRect.gameObject.AddComponent<CanvasGroup>();

        TextMeshProUGUI title = UIBuilder.CreateText(theme, rowRect, string.Empty, 38f, TextAlignmentOptions.BottomLeft, theme.mainWhite);
        title.rectTransform.anchorMin = new Vector2(0f, 0.45f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(60f, 0f);
        title.rectTransform.offsetMax = new Vector2(-380f, -16f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 22f;
        title.fontSizeMax = 38f;

        RectTransform bar = UIBuilder.CreateProgressBar(theme, rowRect, out RectTransform fill);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0f, 0f);
        bar.offsetMin = new Vector2(60f, 30f);
        bar.offsetMax = new Vector2(-580f, 46f);

        TextMeshProUGUI progressText = UIBuilder.CreateText(theme, rowRect, string.Empty, 24f, TextAlignmentOptions.MidlineLeft, theme.greyLight);
        progressText.rectTransform.anchorMin = new Vector2(1f, 0f);
        progressText.rectTransform.anchorMax = new Vector2(1f, 0f);
        progressText.rectTransform.pivot = new Vector2(0f, 0f);
        progressText.rectTransform.anchoredPosition = new Vector2(-565f, 21f);
        progressText.rectTransform.sizeDelta = new Vector2(180f, 34f);

        Button button = UIBuilder.CreateButton(theme, rowRect, "ClaimButton", string.Empty, false, out TextMeshProUGUI buttonText);
        UIBuilder.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(320f, 95f));
        button.onClick.AddListener(onClaim);

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
