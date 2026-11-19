using System;
using System.Globalization;
using UnityEngine;

// 7 günlük giriş ödülü. Her gün bir sonraki günün ödülü alınır,
// 7. gün büyük ödül. Bir gün kaçırılırsa seri 1. günden başlar,
// 7. günden sonra yine 1. güne döner.
// Not: Telefonun saatine göre çalışır; saat ileri alınarak hile yapılabilir.
public static class DailyReward
{
    public static readonly int[] Rewards = { 50, 75, 100, 150, 200, 300, 1000 };

    public static int DayCount => Rewards.Length;

    private const string DateFormat = "yyyy-MM-dd";

    static DateTime Today => DateTime.Now.Date;

    static bool TryGetLastClaim(out DateTime date, out int index)
    {
        string saved = PlayerPrefs.GetString(GameKeys.DailyLastClaimDate, string.Empty);
        index = PlayerPrefs.GetInt(GameKeys.DailyLastClaimIndex, -1);

        return DateTime.TryParseExact(
            saved,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date
        ) && index >= 0;
    }

    // Bugün alınabilir mi?
    public static bool CanClaim
    {
        get
        {
            if (!TryGetLastClaim(out DateTime last, out _))
                return true;

            // Saat geri alındıysa (son alım gelecekte) bekle.
            return last < Today;
        }
    }

    // Bugün alınacak (veya bugün alınmış) ödülün gün sırası (0 = 1. gün).
    public static int CurrentDayIndex
    {
        get
        {
            if (!TryGetLastClaim(out DateTime last, out int lastIndex))
                return 0;

            if (last >= Today)
                return lastIndex;

            // Dün alındıysa seri devam eder.
            if (last == Today.AddDays(-1))
                return (lastIndex + 1) % DayCount;

            return 0;
        }
    }

    // Ekrandaki günlerden kaçı bu seride alınmış.
    public static int ClaimedDaysInStreak =>
        CanClaim ? CurrentDayIndex : CurrentDayIndex + 1;

    public static TimeSpan TimeUntilNextClaim =>
        CanClaim ? TimeSpan.Zero : Today.AddDays(1) - DateTime.Now;

    // Ödülü verir, verilen gold miktarını döner (alınamıyorsa 0).
    public static int Claim()
    {
        if (!CanClaim)
            return 0;

        int index = CurrentDayIndex;
        int gold = Rewards[index];

        PlayerPrefs.SetString(GameKeys.DailyLastClaimDate, Today.ToString(DateFormat, CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt(GameKeys.DailyLastClaimIndex, index);
        PlayerPrefs.Save();

        PlayerProgress.AddGold(gold);
        GameAnalytics.DailyRewardClaimed(index + 1);
        PlayerProgress.NotifyProgressChanged();

        return gold;
    }
}
