using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Tek bir analitik olayı. Her olay alanların sadece bir kısmını kullanır.
// Dil değişse de veriler bozulmasın diye kartlar UpgradeType adıyla kaydedilir.
[Serializable]
public class AnalyticsRecord
{
    public long t;          // UTC Unix saniye
    public string e;        // olay adı
    public int level;       // oyuncu seviyesi
    public int score;
    public int kills;
    public int value;       // olaya göre: kart seviyesi, gün, yükseltme seviyesi...
    public float seconds;   // oyun süresi (duraklatmalar hariç)
    public string card;     // seçilen / değiştirilen kart
    public string rarity;
    public string detail;   // sunulan kartlar, ürün Id'si, reklam yeri...
    public bool flag;       // olaya göre: devam hakkı kullanıldı mı, ücretsiz mi, ödül alındı mı
}

// Olayları alan taraf (yerel dosya, Unity Analytics, Firebase...)
public interface IAnalyticsSink
{
    void Record(AnalyticsRecord record);
}

// Oyunun geri kalanı analitiğe sadece buradan erişir.
// Editor'da ve Development Build'de olaylar yerel dosyaya yazılır
// (Tools > Analitik Özeti ile incelenir). Yayın build'inde gerçek bir
// analitik servisi AddSink ile eklenene kadar hiçbir şey kaydedilmez.
public static class GameAnalytics
{
    public const string Run_Start = "run_start";
    public const string Death = "death";
    public const string Run_End = "run_end";
    public const string Level_Up = "level_up";
    public const string Cards_Offered = "cards_offered";
    public const string Card_Chosen = "card_chosen";
    public const string Card_Rerolled = "card_rerolled";
    public const string Purchase = "purchase";
    public const string Ad_Watched = "ad_watched";
    public const string Permanent_Upgrade = "permanent_upgrade";
    public const string Daily_Reward = "daily_reward";
    public const string Achievement = "achievement";
    public const string Boss_Spawned = "boss_spawned";
    public const string Boss_Defeated = "boss_defeated";

    private static readonly List<IAnalyticsSink> sinks = new List<IAnalyticsSink>();

    static GameAnalytics()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        sinks.Add(new LocalFileAnalyticsSink());
#endif
    }

    public static void AddSink(IAnalyticsSink sink)
    {
        if (sink != null && !sinks.Contains(sink))
        {
            sinks.Add(sink);
        }
    }

    static void Send(AnalyticsRecord record)
    {
        if (sinks.Count == 0)
            return;

        record.t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (IAnalyticsSink sink in sinks)
        {
            try
            {
                sink.Record(record);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("GameAnalytics: " + exception.Message);
            }
        }
    }

    // ==================================
    // OLAYLAR
    // ==================================

    public static void RunStarted() =>
        Send(new AnalyticsRecord { e = Run_Start });

    // Oyuncu öldü (devam hakkı kullanılırsa aynı oyunda tekrar ölebilir).
    public static void PlayerDied(int level, int score, int kills, float seconds, bool reviveUsed) =>
        Send(new AnalyticsRecord { e = Death, level = level, score = score, kills = kills, seconds = seconds, flag = reviveUsed });

    // Oyun bitti ya da yarıda bırakıldı: oyuncunun bıraktığı son durum.
    public static void RunEnded(int level, int score, int kills, float seconds, bool died) =>
        Send(new AnalyticsRecord { e = Run_End, level = level, score = score, kills = kills, seconds = seconds, flag = died });

    public static void LevelReached(int level, float seconds) =>
        Send(new AnalyticsRecord { e = Level_Up, level = level, seconds = seconds });

    public static void CardsOffered(int playerLevel, IEnumerable<string> cards) =>
        Send(new AnalyticsRecord { e = Cards_Offered, level = playerLevel, detail = string.Join(",", cards) });

    public static void CardChosen(int playerLevel, string card, string rarity, int cardLevel) =>
        Send(new AnalyticsRecord { e = Card_Chosen, level = playerLevel, card = card, rarity = rarity, value = cardLevel });

    public static void CardRerolled(int playerLevel, string oldCard, string newCard, bool free) =>
        Send(new AnalyticsRecord { e = Card_Rerolled, level = playerLevel, card = oldCard, detail = newCard, flag = free });

    public static void PurchaseCompleted(string productId) =>
        Send(new AnalyticsRecord { e = Purchase, detail = productId });

    public static void AdWatched(string placement, bool rewarded) =>
        Send(new AnalyticsRecord { e = Ad_Watched, detail = placement, flag = rewarded });

    public static void PermanentUpgradeBought(string upgradeId, int newLevel) =>
        Send(new AnalyticsRecord { e = Permanent_Upgrade, detail = upgradeId, value = newLevel });

    public static void DailyRewardClaimed(int day) =>
        Send(new AnalyticsRecord { e = Daily_Reward, value = day });

    public static void BossSpawned(int level, int bossNumber, string bossType) =>
        Send(new AnalyticsRecord { e = Boss_Spawned, level = level, value = bossNumber, detail = bossType });

    // seconds: boss'un çıkışından ölümüne kadar geçen süre
    public static void BossDefeated(int level, int bossNumber, float seconds) =>
        Send(new AnalyticsRecord { e = Boss_Defeated, level = level, value = bossNumber, seconds = seconds });

    public static void AchievementClaimed(string achievementId) =>
        Send(new AnalyticsRecord { e = Achievement, detail = achievementId });
}

// Olayları satır satır JSON olarak dosyaya yazar (geliştirme / test için).
public class LocalFileAnalyticsSink : IAnalyticsSink
{
    public const string FileName = "analytics.jsonl";

    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public void Record(AnalyticsRecord record)
    {
        File.AppendAllText(FilePath, JsonUtility.ToJson(record) + "\n");
    }
}
