using UnityEngine;

// Ana menüden kalıcı altınla alınan, her oyunun başında uygulanan küçük bonuslar.
// Yeni yükseltme eklemek için All listesine satır ekle ve etkisini
// ilgili sisteme (PlayerHealth, PlayerStats...) bağla.
// Id'leri yayınladıktan sonra değiştirme; seviyeler Id ile kaydediliyor.
public static class PermanentUpgrades
{
    public class Upgrade
    {
        public readonly string id;
        public readonly string title;

        // {0} = mevcut toplam bonus, örn. "+20 başlangıç canı"
        public readonly string bonusFormat;

        public readonly int maxLevel;
        public readonly int baseCost;
        public readonly float costGrowth;
        public readonly float valuePerLevel;

        public Upgrade(string id, string title, string bonusFormat, int maxLevel, int baseCost, float costGrowth, float valuePerLevel)
        {
            this.id = id;
            this.title = title;
            this.bonusFormat = bonusFormat;
            this.maxLevel = maxLevel;
            this.baseCost = baseCost;
            this.costGrowth = costGrowth;
            this.valuePerLevel = valuePerLevel;
        }
    }

    public static readonly Upgrade StartHealth =
        new Upgrade("start_health", "BAŞLANGIÇ CANI", "+{0} maksimum can", 10, 100, 1.45f, 10f);

    public static readonly Upgrade Damage =
        new Upgrade("damage", "GÜÇLÜ MERMİLER", "+%{0} hasar", 10, 150, 1.5f, 5f);

    public static readonly Upgrade FireRate =
        new Upgrade("fire_rate", "HIZLI TETİK", "+%{0} ateş hızı", 5, 200, 1.6f, 4f);

    public static readonly Upgrade GoldBonus =
        new Upgrade("gold_bonus", "ALTIN AVCISI", "+%{0} oyun içi altın", 5, 250, 1.6f, 10f);

    public static readonly Upgrade XpBonus =
        new Upgrade("xp_bonus", "HIZLI ÖĞRENME", "+%{0} tecrübe puanı", 5, 200, 1.6f, 8f);

    public static readonly Upgrade ExtraReroll =
        new Upgrade("extra_reroll", "ŞANSLI EL", "Kart başına +{0} ücretsiz yenileme", 2, 600, 2.5f, 1f);

    public static readonly Upgrade[] All =
    {
        StartHealth,
        Damage,
        FireRate,
        GoldBonus,
        XpBonus,
        ExtraReroll
    };

    public static Upgrade Find(string id)
    {
        foreach (Upgrade upgrade in All)
        {
            if (upgrade.id == id)
                return upgrade;
        }

        return null;
    }

    public static int GetLevel(Upgrade upgrade) =>
        PlayerProgress.GetPermanentUpgradeLevel(upgrade.id);

    public static bool IsMaxed(Upgrade upgrade) =>
        GetLevel(upgrade) >= upgrade.maxLevel;

    // Bir sonraki seviyenin fiyatı
    public static int GetCost(Upgrade upgrade) =>
        Mathf.RoundToInt(upgrade.baseCost * Mathf.Pow(upgrade.costGrowth, GetLevel(upgrade)));

    // Toplam bonus (örn. 3. seviye can = 30)
    public static float GetValue(Upgrade upgrade) =>
        GetLevel(upgrade) * upgrade.valuePerLevel;

    public static float GetValueAtLevel(Upgrade upgrade, int level) =>
        level * upgrade.valuePerLevel;

    public static bool TryBuy(Upgrade upgrade)
    {
        if (IsMaxed(upgrade))
            return false;

        if (!PlayerProgress.TrySpendGold(GetCost(upgrade)))
            return false;

        PlayerProgress.SetPermanentUpgradeLevel(upgrade.id, GetLevel(upgrade) + 1);
        return true;
    }

    // ==================================
    // OYUN İÇİ ETKİLER
    // ==================================

    public static int BonusMaxHealth => Mathf.RoundToInt(GetValue(StartHealth));

    // Çarpana eklenecek oran (0.15 = +%15)
    public static float BonusDamage => GetValue(Damage) / 100f;
    public static float BonusFireRate => GetValue(FireRate) / 100f;

    public static float GoldMultiplier => 1f + GetValue(GoldBonus) / 100f;
    public static float XpMultiplier => 1f + GetValue(XpBonus) / 100f;

    public static int BonusFreeRerolls => Mathf.RoundToInt(GetValue(ExtraReroll));

    // Oyun başında gösterilecek aktif bonuslar (örn. "+20 maksimum can")
    public static System.Collections.Generic.List<string> GetActiveBonusTexts()
    {
        System.Collections.Generic.List<string> texts = new System.Collections.Generic.List<string>();

        foreach (Upgrade upgrade in All)
        {
            if (GetLevel(upgrade) > 0)
            {
                texts.Add(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    upgrade.bonusFormat,
                    Mathf.RoundToInt(GetValue(upgrade))
                ));
            }
        }

        return texts;
    }
}
