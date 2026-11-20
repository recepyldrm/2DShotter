using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki 7 günlük giriş ödülü ekranı.
// Bugün alınabilir ödül varsa menü açılınca otomatik gösterilir
// (ilk açılışta eğitim bittikten sonra). "GÜNLÜK" butonu ile tekrar açılır.
public class DailyRewardUI : MonoBehaviour
{
    private class DayTile
    {
        public CanvasGroup group;
        public RectTransform rect;
        public GameObject claimedMark;
    }

    private UITheme theme;
    private GameObject panel;
    private RectTransform badge;
    private readonly List<DayTile> tiles = new List<DayTile>();
    private Button claimButton;
    private TextMeshProUGUI claimText;
    private TextMeshProUGUI rewardText;

    private bool autoShowPending;
    private float nextTimerUpdate;

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "DailyRewardCanvas", 200);

        Button openButton = UIBuilder.CreateMenuButton(theme, canvas, "GÜNLÜK", UIBuilder.MenuSlotDaily, out badge);
        openButton.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Show();
        });

        BuildPanel(canvas);

        autoShowPending = DailyReward.CanClaim;

        PlayerProgress.OnProgressChanged += UpdateBadge;
        UpdateBadge();
    }

    void OnDestroy()
    {
        PlayerProgress.OnProgressChanged -= UpdateBadge;
    }

    void Update()
    {
        // İlk açılışta önce eğitim gösterilir.
        if (autoShowPending && !TutorialUI.IsShowing)
        {
            autoShowPending = false;

            if (DailyReward.CanClaim)
            {
                Show();
            }
        }

        // Bekleme sayacı saniyede bir güncellenir.
        if (panel.activeSelf && !DailyReward.CanClaim && Time.unscaledTime >= nextTimerUpdate)
        {
            nextTimerUpdate = Time.unscaledTime + 1f;
            UpdateClaimButton();
        }
    }

    void UpdateBadge()
    {
        if (badge != null)
        {
            badge.gameObject.SetActive(DailyReward.CanClaim);
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

    void Claim()
    {
        int gold = DailyReward.Claim();

        if (gold <= 0)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayReward();
        }

        rewardText.text = "+" + gold + " " + UIBuilder.ColorTag(theme.mainRedHover, Loc.T("ALTIN"));
        rewardText.gameObject.SetActive(true);

        Refresh();
    }

    void Refresh()
    {
        int current = DailyReward.CurrentDayIndex;
        int claimedCount = DailyReward.ClaimedDaysInStreak;
        bool canClaim = DailyReward.CanClaim;

        for (int i = 0; i < tiles.Count; i++)
        {
            DayTile tile = tiles[i];

            bool claimed = i < claimedCount;
            bool isToday = i == current && canClaim;

            tile.claimedMark.SetActive(claimed);
            tile.group.alpha = isToday ? 1f : claimed ? 0.45f : 0.7f;
            tile.rect.localScale = isToday ? Vector3.one * 1.08f : Vector3.one;
        }

        if (canClaim)
        {
            rewardText.gameObject.SetActive(false);
        }

        UpdateClaimButton();
        UpdateBadge();
    }

    void UpdateClaimButton()
    {
        bool canClaim = DailyReward.CanClaim;

        claimButton.interactable = canClaim;

        if (canClaim)
        {
            claimText.text = Loc.T("ÖDÜLÜ AL");
            claimText.color = theme.mainWhite;
            return;
        }

        TimeSpan wait = DailyReward.TimeUntilNextClaim;

        claimText.text = string.Format(
            Loc.Pick("YENİ ÖDÜL {0:00}:{1:00}:{2:00}", "NEXT REWARD {0:00}:{1:00}:{2:00}"),
            (int)wait.TotalHours,
            wait.Minutes,
            wait.Seconds
        );
        claimText.color = theme.greyLight;
    }

    // ==================================
    // ARAYÜZ
    // ==================================

    void BuildPanel(Transform canvas)
    {
        RectTransform dim = UIBuilder.CreateDim(canvas, "DailyRewardPanel");
        panel = dim.gameObject;

        RectTransform safeRoot = UIBuilder.CreateSafeRoot(dim);

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            safeRoot,
            new Vector2(0.1f, 0.12f),
            new Vector2(0.9f, 0.88f)
        );

        UIBuilder.CreateWindowTitle(theme, window, "GÜNLÜK ÖDÜL");
        UIBuilder.CreateWindowCloseButton(theme, window).onClick.AddListener(Close);

        TextMeshProUGUI subtitle = UIBuilder.CreateText(
            theme,
            window,
            "Her gün gel, daha fazla altın kazan. Bir gün kaçırırsan seri sıfırlanır!",
            30f,
            TextAlignmentOptions.Center,
            theme.greyLight
        );
        UIBuilder.AnchorTop(subtitle.rectTransform, 150f, 50f);

        // Günler
        RectTransform row = UIBuilder.CreateRect("Days", window);
        row.anchorMin = new Vector2(0f, 0.5f);
        row.anchorMax = new Vector2(1f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, 10f);
        row.sizeDelta = new Vector2(-120f, 260f);

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < DailyReward.DayCount; i++)
        {
            tiles.Add(CreateDayTile(row, i));
        }

        // Alınan ödül yazısı
        rewardText = UIBuilder.CreateText(theme, window, string.Empty, 48f, TextAlignmentOptions.Center, theme.mainWhite);
        UIBuilder.Place(rewardText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(600f, 70f));
        rewardText.gameObject.SetActive(false);

        // Al
        claimButton = UIBuilder.CreateButton(theme, window, "ClaimButton", "ÖDÜLÜ AL", true, out claimText);
        UIBuilder.Place((RectTransform)claimButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(480f, 110f));
        claimButton.onClick.AddListener(Claim);

        panel.SetActive(false);
    }

    DayTile CreateDayTile(RectTransform row, int index)
    {
        bool isBigDay = index == DailyReward.DayCount - 1;

        RectTransform tileRect = UIBuilder.CreateRect("Day" + (index + 1), row);
        tileRect.sizeDelta = new Vector2(0f, isBigDay ? 250f : 220f);

        LayoutElement element = tileRect.gameObject.AddComponent<LayoutElement>();
        element.flexibleWidth = isBigDay ? 1.35f : 1f;

        CanvasGroup group = tileRect.gameObject.AddComponent<CanvasGroup>();

        theme.ApplyPanel(tileRect.gameObject.AddComponent<Image>());

        TextMeshProUGUI dayText = UIBuilder.CreateText(
            theme,
            tileRect,
            isBigDay
                ? Loc.Pick("GÜN 7\n<size=22>BÜYÜK ÖDÜL</size>", "DAY 7\n<size=22>BIG REWARD</size>")
                : Loc.Pick("GÜN ", "DAY ") + (index + 1),
            30f,
            TextAlignmentOptions.Center,
            isBigDay ? theme.mainRedHover : theme.greyLight
        );
        UIBuilder.AnchorTop(dayText.rectTransform, 18f, 70f);

        TextMeshProUGUI amountText = UIBuilder.CreateText(
            theme,
            tileRect,
            "+" + DailyReward.Rewards[index],
            isBigDay ? 54f : 44f,
            TextAlignmentOptions.Center,
            theme.mainWhite
        );
        UIBuilder.Stretch(amountText.rectTransform);
        amountText.rectTransform.offsetMin = new Vector2(0f, 40f);
        amountText.rectTransform.offsetMax = new Vector2(0f, -80f);

        TextMeshProUGUI goldText = UIBuilder.CreateText(theme, tileRect, "ALTIN", 26f, TextAlignmentOptions.Center, theme.mainRedHover);
        goldText.rectTransform.anchorMin = new Vector2(0f, 0f);
        goldText.rectTransform.anchorMax = new Vector2(1f, 0f);
        goldText.rectTransform.pivot = new Vector2(0.5f, 0f);
        goldText.rectTransform.offsetMin = new Vector2(0f, 25f);
        goldText.rectTransform.offsetMax = new Vector2(0f, 60f);

        // "ALINDI" damgası
        RectTransform mark = UIBuilder.CreateRect("Claimed", tileRect);
        UIBuilder.Stretch(mark);
        mark.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        TextMeshProUGUI markText = UIBuilder.CreateText(theme, mark, "ALINDI", 28f, TextAlignmentOptions.Center, theme.mainRedHover);
        UIBuilder.Stretch(markText.rectTransform);
        markText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 20f);

        return new DayTile
        {
            group = group,
            rect = tileRect,
            claimedMark = mark.gameObject
        };
    }
}
