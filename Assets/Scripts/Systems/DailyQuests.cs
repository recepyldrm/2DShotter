using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Her gün yenilenen 3 küçük görev. Görevler tarihe göre seçilir (aynı gün
// hep aynı 3 görev), ilerleme oyun sonunda / oyun yarıda kalınca eklenir.
// Not: Telefonun saatine göre çalışır.
public static class DailyQuests
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
                    case QuestType.Kills: return target + " düşman öldür";
                    case QuestType.Gold: return "Oyunlarda " + target + " altın kazan";
                    case QuestType.Bosses: return target == 1 ? "Bir boss yen" : target + " boss yen";
                    case QuestType.Level: return "Tek oyunda " + target + ". seviyeye ulaş";
                    case QuestType.Runs: return target + " oyun oyna";
                    default: return string.Empty;
                }
            }
        }
    }

    public const int QuestCount = 3;

    private const string DateFormat = "yyyy-MM-dd";

    // Her görev türü için zorluk kademeleri: hedef ve altın ödülü
    private static readonly Dictionary<QuestType, (int target, int reward)[]> Tiers =
        new Dictionary<QuestType, (int, int)[]>
        {
            { QuestType.Kills,  new[] { (100, 60), (250, 120), (500, 200) } },
            { QuestType.Gold,   new[] { (300, 60), (750, 120), (1500, 200) } },
            { QuestType.Bosses, new[] { (1, 100), (2, 180), (3, 250) } },
            { QuestType.Level,  new[] { (8, 60), (12, 120), (16, 200) } },
            { QuestType.Runs,   new[] { (2, 50), (3, 80), (5, 120) } }
        };

    static string TodayKey => DateTime.Now.Date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static event Action OnQuestsChanged;

    // ==================================
    // BUGÜNÜN GÖREVLERİ
    // ==================================

    private static string cachedDate;
    private static List<Quest> cachedQuests;

    public static IReadOnlyList<Quest> Today
    {
        get
        {
            string today = TodayKey;

            if (cachedQuests == null || cachedDate != today)
            {
                cachedDate = today;
                cachedQuests = Generate(today);
            }

            return cachedQuests;
        }
    }

    // Tarihten türetilen tohum: aynı gün her açılışta aynı görevler.
    static List<Quest> Generate(string date)
    {
        int seed = int.Parse(date.Replace("-", string.Empty), CultureInfo.InvariantCulture);
        System.Random random = new System.Random(seed);

        List<QuestType> pool = new List<QuestType>((QuestType[])Enum.GetValues(typeof(QuestType)));
        List<Quest> quests = new List<Quest>();

        for (int i = 0; i < QuestCount && pool.Count > 0; i++)
        {
            int pick = random.Next(pool.Count);
            QuestType type = pool[pick];
            pool.RemoveAt(pick);

            // 1. görev kolay, 2. kolay/orta, 3. orta/zor.
            (int target, int reward)[] tiers = Tiers[type];
            int tier = Mathf.Clamp(i - random.Next(0, 2), 0, tiers.Length - 1);

            quests.Add(new Quest
            {
                index = i,
                type = type,
                target = tiers[tier].target,
                reward = tiers[tier].reward
            });
        }

        return quests;
    }

    // Gün değiştiyse eski ilerlemeyi temizler.
    static void EnsureToday()
    {
        string today = TodayKey;

        if (PlayerPrefs.GetString(GameKeys.QuestDate, string.Empty) == today)
            return;

        PlayerPrefs.SetString(GameKeys.QuestDate, today);

        for (int i = 0; i < QuestCount; i++)
        {
            PlayerPrefs.DeleteKey(GameKeys.QuestProgressPrefix + i);
            PlayerPrefs.DeleteKey(GameKeys.QuestClaimedPrefix + i);
        }
    }

    // ==================================
    // İLERLEME
    // ==================================

    public static int GetProgress(Quest quest)
    {
        EnsureToday();
        return PlayerPrefs.GetInt(GameKeys.QuestProgressPrefix + quest.index, 0);
    }

    public static bool IsCompleted(Quest quest) => GetProgress(quest) >= quest.target;

    public static bool IsClaimed(Quest quest)
    {
        EnsureToday();
        return PlayerPrefs.GetInt(GameKeys.QuestClaimedPrefix + quest.index, 0) == 1;
    }

    public static int ClaimableCount
    {
        get
        {
            int count = 0;

            foreach (Quest quest in Today)
            {
                if (IsCompleted(quest) && !IsClaimed(quest))
                    count++;
            }

            return count;
        }
    }

    // PlayerProgress.RecordRunProgress her kayıtta çağırır.
    public static void Record(int kills, int goldEarned, int levelReached, bool countRun, int bossKills)
    {
        EnsureToday();

        bool changed = false;

        foreach (Quest quest in Today)
        {
            if (IsClaimed(quest))
                continue;

            string key = GameKeys.QuestProgressPrefix + quest.index;
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

        if (changed)
        {
            OnQuestsChanged?.Invoke();
        }
    }

    // Ödülü verir; verilen altını döner (alınamıyorsa 0).
    public static int Claim(Quest quest)
    {
        if (!IsCompleted(quest) || IsClaimed(quest))
            return 0;

        PlayerPrefs.SetInt(GameKeys.QuestClaimedPrefix + quest.index, 1);
        PlayerPrefs.Save();

        PlayerProgress.AddGold(quest.reward);
        GameAnalytics.DailyQuestClaimed(quest.type.ToString(), quest.target, quest.reward);

        OnQuestsChanged?.Invoke();
        return quest.reward;
    }

    public static TimeSpan TimeUntilReset => DateTime.Now.Date.AddDays(1) - DateTime.Now;

    // Test menüsü için
    public static void ResetToday()
    {
        PlayerPrefs.DeleteKey(GameKeys.QuestDate);
        EnsureToday();
        PlayerPrefs.Save();
        OnQuestsChanged?.Invoke();
    }

    public static void CompleteAllForDebug()
    {
        EnsureToday();

        foreach (Quest quest in Today)
        {
            PlayerPrefs.SetInt(GameKeys.QuestProgressPrefix + quest.index, quest.target);
        }

        PlayerPrefs.Save();
        OnQuestsChanged?.Invoke();
    }
}
