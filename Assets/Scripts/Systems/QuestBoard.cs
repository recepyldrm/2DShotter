using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Dönemlik görev panosu. İki pano var:
//  - Günlük: her gün 3 küçük görev
//  - Haftalık: her pazartesi 3 büyük görev; üçü de alınınca haftalık sandık açılır
// Görevler döneme göre seçilir (aynı dönemde hep aynı görevler).
// İlerleme oyun sonunda / oyun yarıda kalınca eklenir. Telefonun saatine göre çalışır.
public class QuestBoard
{
    public enum QuestType
    {
        Kills,      // Toplam düşman öldür
        Gold,       // Toplam altın kazan
        Bosses,     // Boss yen
        Level,      // Tek oyunda şu seviyeye ulaş
        Runs        // Oyun oyna
    }

    public class Quest
    {
        public int index;
        public QuestType type;
        public int target;
        public int reward;

        public string Title
        {
            get
            {
                switch (type)
                {
                    case QuestType.Kills: return Loc.Pick(target + " düşman öldür", "Kill " + target + " enemies");
                    case QuestType.Gold: return Loc.Pick("Oyunlarda " + target + " altın kazan", "Earn " + target + " gold");
                    case QuestType.Bosses:
                        return target == 1
                            ? Loc.Pick("Bir boss yen", "Defeat a boss")
                            : Loc.Pick(target + " boss yen", "Defeat " + target + " bosses");
                    case QuestType.Level: return Loc.Pick("Tek oyunda " + target + ". seviyeye ulaş", "Reach level " + target + " in one run");
                    case QuestType.Runs: return Loc.Pick(target + " oyun oyna", "Play " + target + " runs");
                    default: return string.Empty;
                }
            }
        }
    }

    public const int QuestCount = 3;

    // ==================================
    // PANOLAR
    // ==================================

    public static readonly QuestBoard Daily = new QuestBoard(
        "daily",
        GameKeys.QuestDate,
        GameKeys.QuestProgressPrefix,
        GameKeys.QuestClaimedPrefix,
        () => DateTime.Now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        () => DateTime.Now.Date.AddDays(1) - DateTime.Now,
        new Dictionary<QuestType, (int, int)[]>
        {
            { QuestType.Kills,  new[] { (100, 60), (250, 120), (500, 200) } },
            { QuestType.Gold,   new[] { (300, 60), (750, 120), (1500, 200) } },
            { QuestType.Bosses, new[] { (1, 100), (2, 180), (3, 250) } },
            { QuestType.Level,  new[] { (8, 60), (12, 120), (16, 200) } },
            { QuestType.Runs,   new[] { (2, 50), (3, 80), (5, 120) } }
        },
        chestReward: 0
    );

    public static readonly QuestBoard Weekly = new QuestBoard(
        "weekly",
        GameKeys.WeeklyQuestWeek,
        GameKeys.WeeklyQuestProgressPrefix,
        GameKeys.WeeklyQuestClaimedPrefix,
        WeekKey,
        () => NextMonday() - DateTime.Now,
        new Dictionary<QuestType, (int, int)[]>
        {
            { QuestType.Kills,  new[] { (1500, 400), (2500, 600), (4000, 900) } },
            { QuestType.Gold,   new[] { (4000, 400), (7000, 600), (12000, 900) } },
            { QuestType.Bosses, new[] { (5, 450), (8, 650), (12, 950) } },
            { QuestType.Level,  new[] { (18, 400), (22, 600), (26, 900) } },
            { QuestType.Runs,   new[] { (10, 300), (15, 450), (25, 700) } }
        },
        chestReward: 1500
    );

    public static IEnumerable<QuestBoard> All
    {
        get
        {
            yield return Daily;
            yield return Weekly;
        }
    }

    public static event Action OnChanged;

    static string WeekKey()
    {
        DateTime today = DateTime.Now.Date;
        int week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        int year = today.Year;

        // Yılın başındaki / sonundaki hafta önceki / sonraki yıla ait olabilir.
        if (week >= 52 && today.Month == 1) year--;
        if (week == 1 && today.Month == 12) year++;

        return year.ToString(CultureInfo.InvariantCulture) + "-W" + week.ToString("00", CultureInfo.InvariantCulture);
    }

    static DateTime NextMonday()
    {
        DateTime today = DateTime.Now.Date;
        int days = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(days == 0 ? 7 : days);
    }

    // ==================================
    // PANO
    // ==================================

    public readonly string id;
    public readonly int chestReward;

    private readonly string periodKey;
    private readonly string progressPrefix;
    private readonly string claimedPrefix;
    private readonly Func<string> currentPeriod;
    private readonly Func<TimeSpan> timeUntilReset;
    private readonly Dictionary<QuestType, (int target, int reward)[]> tiers;

    private string cachedPeriod;
    private List<Quest> cachedQuests;

    QuestBoard(
        string id,
        string periodKey,
        string progressPrefix,
        string claimedPrefix,
        Func<string> currentPeriod,
        Func<TimeSpan> timeUntilReset,
        Dictionary<QuestType, (int, int)[]> tiers,
        int chestReward)
    {
        this.id = id;
        this.periodKey = periodKey;
        this.progressPrefix = progressPrefix;
        this.claimedPrefix = claimedPrefix;
        this.currentPeriod = currentPeriod;
        this.timeUntilReset = timeUntilReset;
        this.tiers = tiers;
        this.chestReward = chestReward;
    }

    public string Period => currentPeriod();

    public TimeSpan TimeUntilReset => timeUntilReset();

    public bool HasChest => chestReward > 0;

    public IReadOnlyList<Quest> Current
    {
        get
        {
            string period = currentPeriod();

            if (cachedQuests == null || cachedPeriod != period)
            {
                cachedPeriod = period;
                cachedQuests = Generate(period);
            }

            return cachedQuests;
        }
    }

    // Dönemden türetilen tohum: aynı dönemde her açılışta aynı görevler.
    List<Quest> Generate(string period)
    {
        System.Random random = new System.Random(StableHash(id + period));

        List<QuestType> pool = new List<QuestType>((QuestType[])Enum.GetValues(typeof(QuestType)));
        List<Quest> quests = new List<Quest>();

        for (int i = 0; i < QuestCount && pool.Count > 0; i++)
        {
            int pick = random.Next(pool.Count);
            QuestType type = pool[pick];
            pool.RemoveAt(pick);

            // 1. görev kolay, 2. kolay/orta, 3. orta/zor.
            (int target, int reward)[] typeTiers = tiers[type];
            int tier = Mathf.Clamp(i - random.Next(0, 2), 0, typeTiers.Length - 1);

            quests.Add(new Quest
            {
                index = i,
                type = type,
                target = typeTiers[tier].target,
                reward = typeTiers[tier].reward
            });
        }

        return quests;
    }

    // string.GetHashCode her çalıştırmada aynı olmak zorunda değil; kendi hash'imiz.
    static int StableHash(string text)
    {
        unchecked
        {
            int hash = 23;

            foreach (char c in text)
            {
                hash = hash * 31 + c;
            }

            return hash;
        }
    }

    // Dönem değiştiyse eski ilerlemeyi temizler.
    void EnsureCurrent()
    {
        string period = currentPeriod();

        if (PlayerPrefs.GetString(periodKey, string.Empty) == period)
            return;

        PlayerPrefs.SetString(periodKey, period);

        for (int i = 0; i < QuestCount; i++)
        {
            PlayerPrefs.DeleteKey(progressPrefix + i);
            PlayerPrefs.DeleteKey(claimedPrefix + i);
        }

        if (HasChest)
        {
            PlayerPrefs.DeleteKey(GameKeys.WeeklyChestClaimed);
        }
    }

    // ==================================
    // İLERLEME
    // ==================================

    public int GetProgress(Quest quest)
    {
        EnsureCurrent();
        return PlayerPrefs.GetInt(progressPrefix + quest.index, 0);
    }

    public bool IsCompleted(Quest quest) => GetProgress(quest) >= quest.target;

    public bool IsClaimed(Quest quest)
    {
        EnsureCurrent();
        return PlayerPrefs.GetInt(claimedPrefix + quest.index, 0) == 1;
    }

    public bool AllClaimed
    {
        get
        {
            foreach (Quest quest in Current)
            {
                if (!IsClaimed(quest))
                    return false;
            }

            return true;
        }
    }

    public bool IsChestClaimed
    {
        get
        {
            EnsureCurrent();
            return PlayerPrefs.GetInt(GameKeys.WeeklyChestClaimed, 0) == 1;
        }
    }

    public bool CanClaimChest => HasChest && AllClaimed && !IsChestClaimed;

    public int ClaimableCount
    {
        get
        {
            int count = 0;

            foreach (Quest quest in Current)
            {
                if (IsCompleted(quest) && !IsClaimed(quest))
                    count++;
            }

            if (CanClaimChest)
                count++;

            return count;
        }
    }

    public static int TotalClaimable
    {
        get
        {
            int count = 0;

            foreach (QuestBoard board in All)
                count += board.ClaimableCount;

            return count;
        }
    }

    // PlayerProgress.RecordRunProgress her kayıtta çağırır.
    public static void RecordAll(int kills, int goldEarned, int levelReached, bool countRun, int bossKills)
    {
        bool changed = false;

        foreach (QuestBoard board in All)
        {
            changed |= board.Record(kills, goldEarned, levelReached, countRun, bossKills);
        }

        if (changed)
        {
            OnChanged?.Invoke();
        }
    }

    bool Record(int kills, int goldEarned, int levelReached, bool countRun, int bossKills)
    {
        EnsureCurrent();

        bool changed = false;

        foreach (Quest quest in Current)
        {
            if (IsClaimed(quest))
                continue;

            string key = progressPrefix + quest.index;
            int current = PlayerPrefs.GetInt(key, 0);
            int next = current;

            switch (quest.type)
            {
                case QuestType.Kills: next += kills; break;
                case QuestType.Gold: next += goldEarned; break;
                case QuestType.Bosses: next += bossKills; break;
                case QuestType.Level: next = Mathf.Max(current, levelReached); break;
                case QuestType.Runs: next += countRun ? 1 : 0; break;
            }

            next = Mathf.Min(next, quest.target);

            if (next != current)
            {
                PlayerPrefs.SetInt(key, next);
                changed = true;
            }
        }

        return changed;
    }

    // Ödülü verir; verilen altını döner (alınamıyorsa 0).
    public int Claim(Quest quest)
    {
        if (!IsCompleted(quest) || IsClaimed(quest))
            return 0;

        PlayerPrefs.SetInt(claimedPrefix + quest.index, 1);
        PlayerPrefs.Save();

        PlayerProgress.AddGold(quest.reward);

        if (HasChest)
            GameAnalytics.WeeklyQuestClaimed(quest.type.ToString(), quest.target, quest.reward);
        else
            GameAnalytics.DailyQuestClaimed(quest.type.ToString(), quest.target, quest.reward);

        OnChanged?.Invoke();
        return quest.reward;
    }

    public int ClaimChest()
    {
        if (!CanClaimChest)
            return 0;

        PlayerPrefs.SetInt(GameKeys.WeeklyChestClaimed, 1);
        PlayerPrefs.Save();

        PlayerProgress.AddGold(chestReward);
        GameAnalytics.WeeklyQuestClaimed("chest", 0, chestReward);

        OnChanged?.Invoke();
        return chestReward;
    }

    // ==================================
    // TEST MENÜSÜ
    // ==================================

    public static void ResetAllForDebug()
    {
        foreach (QuestBoard board in All)
        {
            PlayerPrefs.DeleteKey(board.periodKey);
            board.EnsureCurrent();
        }

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public static void CompleteAllForDebug()
    {
        foreach (QuestBoard board in All)
        {
            board.EnsureCurrent();

            foreach (Quest quest in board.Current)
            {
                PlayerPrefs.SetInt(board.progressPrefix + quest.index, quest.target);
            }
        }

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
