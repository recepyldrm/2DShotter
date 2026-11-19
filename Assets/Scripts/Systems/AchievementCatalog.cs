// Başarımlar. Yeni başarım eklemek için All listesine satır ekle.
// Ödül ya gold ya da bir dost skini (CompanionSkinDatabase'deki skin Id'si).
// Id'leri yayınladıktan sonra değiştirme; alınan ödüller Id ile kaydediliyor.
public static class AchievementCatalog
{
    public enum Stat
    {
        TotalKills,
        BestLevel,
        TotalRuns,
        BestScore,
        TotalGoldEarned,
        TotalBossKills
    }

    public class Achievement
    {
        public readonly string id;
        public readonly string title;
        public readonly string description;
        public readonly Stat stat;
        public readonly int target;
        public readonly int goldReward;
        public readonly string skinReward;

        public Achievement(string id, string title, string description, Stat stat, int target, int goldReward, string skinReward = null)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.stat = stat;
            this.target = target;
            this.goldReward = goldReward;
            this.skinReward = skinReward;
        }

        public bool HasSkinReward => !string.IsNullOrEmpty(skinReward);
    }

    public static readonly Achievement[] All =
    {
        new Achievement("kills_100", "İLK KAN", "100 düşman öldür", Stat.TotalKills, 100, 100),
        new Achievement("kills_500", "YOK EDİCİ", "500 düşman öldür", Stat.TotalKills, 500, 300),
        new Achievement("kills_2000", "KIYIM MAKİNESİ", "2000 düşman öldür", Stat.TotalKills, 2000, 0, "drone_sentinel"),
        new Achievement("kills_5000", "ÇORAK TOPRAKLARIN EFSANESİ", "5000 düşman öldür", Stat.TotalKills, 5000, 2000),

        new Achievement("level_5", "GÜÇLENİYORSUN", "Bir oyunda 5. seviyeye ulaş", Stat.BestLevel, 5, 100),
        new Achievement("level_10", "KIDEMLİ ASKER", "Bir oyunda 10. seviyeye ulaş", Stat.BestLevel, 10, 300),
        new Achievement("level_15", "DURDURULAMAZ", "Bir oyunda 15. seviyeye ulaş", Stat.BestLevel, 15, 0, "drone_phantom"),

        new Achievement("runs_10", "AZİMLİ", "10 oyun oyna", Stat.TotalRuns, 10, 150),
        new Achievement("runs_50", "ADANMIŞ", "50 oyun oyna", Stat.TotalRuns, 50, 500),

        new Achievement("score_1000", "KESKİN NİŞANCI", "Bir oyunda 1000 skor yap", Stat.BestScore, 1000, 200),
        new Achievement("score_5000", "REKOR AVCISI", "Bir oyunda 5000 skor yap", Stat.BestScore, 5000, 600),

        new Achievement("gold_5000", "HAZİNE AVCISI", "Oyunlarda toplam 5000 altın kazan", Stat.TotalGoldEarned, 5000, 500),

        new Achievement("boss_1", "DEV AVCISI", "İlk boss'u yen", Stat.TotalBossKills, 1, 200),
        new Achievement("boss_10", "BOSS KATİLİ", "Toplam 10 boss yen", Stat.TotalBossKills, 10, 1000),
    };

    public static int GetProgress(Achievement achievement)
    {
        switch (achievement.stat)
        {
            case Stat.TotalKills:
                return PlayerProgress.TotalKills;

            case Stat.BestLevel:
                return PlayerProgress.BestLevel;

            case Stat.TotalRuns:
                return PlayerProgress.TotalRuns;

            case Stat.BestScore:
                return PlayerProgress.BestScore;

            case Stat.TotalGoldEarned:
                return PlayerProgress.TotalGoldEarned;

            case Stat.TotalBossKills:
                return PlayerProgress.TotalBossKills;

            default:
                return 0;
        }
    }

    public static bool IsCompleted(Achievement achievement) =>
        GetProgress(achievement) >= achievement.target;

    public static bool IsClaimed(Achievement achievement) =>
        PlayerProgress.IsAchievementClaimed(achievement.id);

    public static bool CanClaim(Achievement achievement) =>
        IsCompleted(achievement) && !IsClaimed(achievement);

    public static int ClaimableCount
    {
        get
        {
            int count = 0;

            foreach (Achievement achievement in All)
            {
                if (CanClaim(achievement))
                    count++;
            }

            return count;
        }
    }

    // Ödülü verir. Zaten alınmış ya da tamamlanmamışsa false döner.
    public static bool Claim(Achievement achievement)
    {
        if (!CanClaim(achievement))
            return false;

        // Önce işaretle: ödül iki kez verilmesin.
        PlayerProgress.MarkAchievementClaimed(achievement.id);
        GameAnalytics.AchievementClaimed(achievement.id);

        if (achievement.goldReward > 0)
        {
            PlayerProgress.AddGold(achievement.goldReward);
        }

        if (achievement.HasSkinReward)
        {
            PlayerProgress.UnlockCompanionSkin(achievement.skinReward);
        }

        return true;
    }

    // Ödül olarak verilen skinin dükkandaki "nasıl açılır" açıklaması için
    public static Achievement FindBySkinReward(string skinId)
    {
        foreach (Achievement achievement in All)
        {
            if (achievement.skinReward == skinId)
                return achievement;
        }

        return null;
    }
}
