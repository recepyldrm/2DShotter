using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Sezon bileti: 30 günlük sezonlar, 30 seviye.
// Oyun oynayarak ve görev ödüllerini alarak sezon puanı kazanılır.
// Her seviyenin ücretsiz ödülü herkese açık; premium ödüller için
// "season_pass" ürünü satın alınır (o sezon için geçerli).
// Telefonun saatine göre çalışır; ilerleme cihazda saklanır.
public static class SeasonPass
{
    public const int TierCount = 30;
    public const int PointsPerTier = 400;
    public const int SeasonDays = 30;

    // Google Play Console'da "season_pass" kimliğiyle tüketilebilir ürün oluştur.
    public const string ProductId = "season_pass";

    // Premium yolun son ödülü olan özel drone görünümü
    public const string RewardSkinId = "drone_season";

    // Ücretsiz yolun 20. seviyesindeki drone görünümü
    public const string FreeRewardSkinId = "drone_night";

    public static bool IsSeasonRewardSkin(string skinId) => skinId == RewardSkinId || skinId == FreeRewardSkinId;

    // Puan kaynakları
    public const int PointsPerLevelReached = 3;
    public const int PointsPerTenKills = 1;
    public const int PointsPerBoss = 25;
    public const int PointsPerDailyQuest = 60;
    public const int PointsPerWeeklyQuest = 200;
    public const int PointsPerWeeklyChest = 300;

    // 5 Ocak 2026 pazartesi: 1. sezonun başlangıcı
    private static readonly DateTime Epoch = new DateTime(2026, 1, 5);

    public static event Action OnChanged;

    public struct Reward
    {
        public int gold;
        public string skinId;

        // Mermi görünümü ödülü (BulletSkins)
        public string bulletSkinId;

        public bool IsBulletSkin => !string.IsNullOrEmpty(bulletSkinId);

        public bool IsSkin => !string.IsNullOrEmpty(skinId);
    }

    // ==================================
    // SEZON
    // ==================================

    public static int SeasonIndex => Mathf.Max(0, (int)Math.Floor((DateTime.Now.Date - Epoch).TotalDays / SeasonDays));

    public static int SeasonNumber => SeasonIndex + 1;

    public static DateTime SeasonEnd => Epoch.AddDays((SeasonIndex + 1) * SeasonDays);

    public static TimeSpan TimeLeft => SeasonEnd - DateTime.Now;

    // Sezon değiştiyse puanları ve alınan ödülleri sıfırlar.
    static void EnsureCurrent()
    {
        int season = SeasonIndex;

        if (PlayerPrefs.GetInt(GameKeys.SeasonIndex, -1) == season)
            return;

        PlayerPrefs.SetInt(GameKeys.SeasonIndex, season);
        PlayerPrefs.SetInt(GameKeys.SeasonPoints, 0);
        PlayerPrefs.DeleteKey(GameKeys.SeasonFreeClaimed);
        PlayerPrefs.DeleteKey(GameKeys.SeasonPremiumClaimed);
        PlayerPrefs.SetInt(GameKeys.SeasonPremium, 0);
        PlayerPrefs.Save();
    }

    // ==================================
    // PUAN / SEVİYE
    // ==================================

    public static int Points
    {
        get
        {
            EnsureCurrent();
            return PlayerPrefs.GetInt(GameKeys.SeasonPoints, 0);
        }
    }

    public static int Tier => Mathf.Min(TierCount, Points / PointsPerTier);

    // Bir sonraki seviyeye ilerleme (0-1)
    public static float TierProgress =>
        Tier >= TierCount ? 1f : (Points % PointsPerTier) / (float)PointsPerTier;

    public static void AddPoints(int amount)
    {
        if (amount <= 0)
            return;

        EnsureCurrent();

        int before = Tier;
        int max = TierCount * PointsPerTier;
        PlayerPrefs.SetInt(GameKeys.SeasonPoints, Mathf.Min(max, Points + amount));

        if (Tier != before)
        {
            PlayerPrefs.Save();
        }

        OnChanged?.Invoke();
    }

    // PlayerProgress.RecordRunProgress çağırır.
    public static void RecordRun(int kills, int levelReached, bool countRun, int bossKills)
    {
        int points = kills / 10 * PointsPerTenKills + bossKills * PointsPerBoss;

        if (countRun)
            points += levelReached * PointsPerLevelReached;

        AddPoints(points);
    }

    // ==================================
    // PREMİUM
    // ==================================

    public static bool IsPremium
    {
        get
        {
            EnsureCurrent();
            return PlayerPrefs.GetInt(GameKeys.SeasonPremium, 0) == 1;
        }
    }

    // IAPManager satın alma onaylanınca çağırır.
    public static void UnlockPremium()
    {
        EnsureCurrent();
        PlayerPrefs.SetInt(GameKeys.SeasonPremium, 1);

        // Bulut kayda hemen yüklensin (parayla alınan ürün kaybolmasın).
        PlayerProgress.NotifySaveDataChanged();
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    // ==================================
    // ÖDÜLLER
    // ==================================

    // tier: 1..30
    public static Reward FreeReward(int tier)
    {
        if (tier == TierCount)
            return new Reward { gold = 1000 };

        // 20. seviye: ücretsiz yolun drone görünümü (zaten varsa altın)
        if (tier == 20)
        {
            return PlayerProgress.IsCompanionSkinOwned(FreeRewardSkinId)
                ? new Reward { gold = 400 }
                : new Reward { skinId = FreeRewardSkinId };
        }

        if (tier % 5 == 0)
            return new Reward { gold = 300 };

        return new Reward { gold = Mathf.RoundToInt((60 + tier * 8) / 10f) * 10 };
    }

    public static Reward PremiumReward(int tier)
    {
        if (tier == TierCount)
        {
            // Görünüm zaten varsa onun yerine altın
            return PlayerProgress.IsCompanionSkinOwned(RewardSkinId)
                ? new Reward { gold = 3000 }
                : new Reward { skinId = RewardSkinId, gold = 1000 };
        }

        // 15. seviye: SEZON ALEVİ mermi görünümü (zaten varsa altın)
        if (tier == 15)
        {
            return BulletSkins.IsOwned(BulletSkins.Find(BulletSkins.SeasonSkinId))
                ? new Reward { gold = 800 }
                : new Reward { bulletSkinId = BulletSkins.SeasonSkinId, gold = 300 };
        }

        if (tier % 5 == 0)
            return new Reward { gold = 800 };

        return new Reward { gold = Mathf.RoundToInt((150 + tier * 15) / 10f) * 10 };
    }

    public static bool IsFreeClaimed(int tier) => ReadClaimed(GameKeys.SeasonFreeClaimed).Contains(tier);

    public static bool IsPremiumClaimed(int tier) => ReadClaimed(GameKeys.SeasonPremiumClaimed).Contains(tier);

    public static bool CanClaimFree(int tier) => tier <= Tier && !IsFreeClaimed(tier);

    public static bool CanClaimPremium(int tier) => IsPremium && tier <= Tier && !IsPremiumClaimed(tier);

    public static int ClaimableCount
    {
        get
        {
            int count = 0;

            for (int tier = 1; tier <= Tier; tier++)
            {
                if (CanClaimFree(tier)) count++;
                if (CanClaimPremium(tier)) count++;
            }

            return count;
        }
    }

    public static bool ClaimFree(int tier)
    {
        if (!CanClaimFree(tier))
            return false;

        MarkClaimed(GameKeys.SeasonFreeClaimed, tier);
        Grant(FreeReward(tier));
        return true;
    }

    public static bool ClaimPremium(int tier)
    {
        if (!CanClaimPremium(tier))
            return false;

        Reward reward = PremiumReward(tier);
        MarkClaimed(GameKeys.SeasonPremiumClaimed, tier);
        Grant(reward);
        return true;
    }

    static void Grant(Reward reward)
    {
        if (reward.gold > 0)
            PlayerProgress.AddGold(reward.gold);

        if (reward.IsSkin)
            PlayerProgress.UnlockCompanionSkin(reward.skinId);

        if (reward.IsBulletSkin)
            BulletSkins.Unlock(reward.bulletSkinId);

        OnChanged?.Invoke();
    }

    static HashSet<int> ReadClaimed(string key)
    {
        EnsureCurrent();

        HashSet<int> claimed = new HashSet<int>();

        foreach (string part in PlayerPrefs.GetString(key, string.Empty).Split(','))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tier))
                claimed.Add(tier);
        }

        return claimed;
    }

    static void MarkClaimed(string key, int tier)
    {
        HashSet<int> claimed = ReadClaimed(key);
        claimed.Add(tier);

        List<string> parts = new List<string>();
        foreach (int value in claimed)
            parts.Add(value.ToString(CultureInfo.InvariantCulture));

        PlayerPrefs.SetString(key, string.Join(",", parts));
        PlayerPrefs.Save();
    }

    // Test menüsü için
    public static void AddTierForDebug()
    {
        AddPoints(PointsPerTier);
    }

    public static void ResetForDebug()
    {
        PlayerPrefs.DeleteKey(GameKeys.SeasonIndex);
        EnsureCurrent();
        OnChanged?.Invoke();
    }
}
