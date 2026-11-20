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
        Runs,       // Oyun oyna
        ShieldedKills, // Kalkanlı düşman öldür
        GunnerKills,   // Nişancı düşman öldür
        BossStuns,     // Yıkıcı boss'u sersemlet
        HardRuns       // Zor modda oyun oyna
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
                    case QuestType.ShieldedKills: return Loc.Pick(target + " kalkanlı düşman öldür", "Kill " + target + " shielded enemies");
                    case QuestType.GunnerKills: return Loc.Pick(target + " nişancı düşman öldür", "Kill " + target + " gunners");
                    case QuestType.BossStuns:
                        return target == 1
                            ? Loc.Pick("Yıkıcı boss'u sersemlet", "Stun a Smasher boss")
                            : Loc.Pick("Yıkıcı boss'u " + target + " kez sersemlet", "Stun a Smasher boss " + target + " times");
                    case QuestType.HardRuns: return Loc.Pick("ZOR modda " + target + " oyun oyna", "Play " + target + " runs on HARD");
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
            { QuestType.Runs,   new[] { (2, 50), (3, 80), (5, 120) } },
            { QuestType.ShieldedKills, new[] { (10, 80), (20, 140), (35, 220) } },
            { QuestType.GunnerKills,   new[] { (15, 80), (30, 140), (50, 220) } },
            { QuestType.BossStuns,     new[] { (1, 150), (2, 250), (3, 350) } },
            { QuestType.HardRuns,      new[] { (1, 100), (2, 180), (3, 260) } }
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
            { QuestType.Runs,   new[] { (10, 300), (15, 450), (25, 700) } },
            { QuestType.ShieldedKills, new[] { (80, 450), (150, 650), (250, 950) } },
            { QuestType.GunnerKills,   new[] { (120, 450), (200, 650), (350, 950) } },
            { QuestType.BossStuns,     new[] { (4, 500), (7, 750), (10, 1000) } },
            { QuestType.HardRuns,      new[] { (5, 450), (8, 650), (12, 950) } }
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
                EnsureCurrent();
                cachedPeriod = period;
                cachedQuests = LoadSaved() ?? Generate(period);
            }

            return cachedQuests;
        }
    }

    string ListKey => periodKey + "_List";

    // Görev listesi dönem başında kaydedilir; oyuncu gün içinde ilerleyip yeni
    // görev türleri açılsa bile o dönemin görevleri değişmez.
    void SaveList(List<Quest> quests)
    {
        List<string> parts = new List<string>();

        foreach (Quest quest in quests)
        {
            parts.Add((int)quest.type + ":" + quest.target + ":" + quest.reward);
        }

        PlayerPrefs.SetString(ListKey, string.Join(";", parts));
    }

    List<Quest> LoadSaved()
    {
        string saved = PlayerPrefs.GetString(ListKey, string.Empty);

        if (string.IsNullOrEmpty(saved))
            return null;

        List<Quest> quests = new List<Quest>();
        string[] entries = saved.Split(';');

        for (int i = 0; i < entries.Length; i++)
        {
            string[] fields = entries[i].Split(':');

            if (fields.Length != 3 ||
                !int.TryParse(fields[0], out int type) ||
                !int.TryParse(fields[1], out int target) ||
                !int.TryParse(fields[2], out int reward) ||
                !Enum.IsDefined(typeof(QuestType), type))
            {
                return null;
            }

            quests.Add(new Quest { index = i, type = (QuestType)type, target = target, reward = reward });
        }

        return quests.Count > 0 ? quests : null;
    }

    // Yeni içerik gerektiren görevler, oyuncu o içeriği görecek kadar ilerlediyse çıkar.
    static bool IsEligible(QuestType type)
    {
        int best = PlayerProgress.BestLevel;

        switch (type)
        {
            case QuestType.GunnerKills: return best >= 4;
            case QuestType.ShieldedKills: return best >= 6;
            case QuestType.HardRuns: return best >= 10;
            case QuestType.BossStuns: return best >= 21;   // Yıkıcı 4. boss (20. seviye)
            case QuestType.Bosses: return best >= 5;
            default: return true;
        }
    }

    // Dönemden türetilen tohum: aynı dönemde her açılışta aynı görevler.
    List<Quest> Generate(string period)
    {
        System.Random random = new System.Random(StableHash(id + period));

        List<QuestType> pool = new List<QuestType>();

        foreach (QuestType type in (QuestType[])Enum.GetValues(typeof(QuestType)))
        {
            if (IsEligible(type))
                pool.Add(type);
        }

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

        SaveList(quests);
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
        PlayerPrefs.DeleteKey(ListKey);
        cachedQuests = null;

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

    // Oyun sırasında anında sayılan görevler (kalkanlı / nişancı öldürme, sersemletme)
    public static void Increment(QuestType type, int amount)
    {
        bool changed = false;

        foreach (QuestBoard board in All)
        {
            board.EnsureCurrent();

            foreach (Quest quest in board.Current)
            {
                if (quest.type != type || board.IsClaimed(quest))
                    continue;

                string key = board.progressPrefix + quest.index;
                int current = PlayerPrefs.GetInt(key, 0);
                int next = Mathf.Min(quest.target, current + amount);

                if (next != current)
                {
                    PlayerPrefs.SetInt(key, next);
                    changed = true;
                }
            }
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
                case QuestType.HardRuns: next += countRun && Difficulty.Current == Difficulty.Level.Hard ? 1 : 0; break;
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
        SeasonPass.AddPoints(HasChest ? SeasonPass.PointsPerWeeklyQuest : SeasonPass.PointsPerDailyQuest);

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
        SeasonPass.AddPoints(SeasonPass.PointsPerWeeklyChest);
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
