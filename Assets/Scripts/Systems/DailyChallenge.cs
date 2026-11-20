using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

// Günlük meydan okuma: herkes o gün aynı silah, aynı bölüm kuralı ve aynı
// ek kuralla oynar. Kalıcı geliştirmeler, silah seviyeleri, drone yetenekleri
// ve zorluk seçimi devre dışıdır (adil olsun). Skor ayrı liderlik tablosuna gider.
// Günün ilk oyunu katılım ödülü verir.
public static class DailyChallenge
{
    public enum Modifier
    {
        DoubleGold,   // Çift altın
        FastEnemies,  // Düşmanlar %30 hızlı
        GlassCannon,  // Yarım can, %50 fazla hasar
        BossRush      // Her 3 seviyede boss
    }

    public const int ParticipationReward = 200;

    private const string DateFormat = "yyyy-MM-dd";

    // Şu an meydan okuma oyunu mu oynanıyor? (Ana menüye dönünce kapanır.)
    public static bool IsActive { get; private set; }

    // Son oyunda verilen katılım ödülü (oyun sonu özetinde gösterilir)
    public static int LastReward { get; private set; }

    static string TodayKey => DateTime.Now.Date.ToString(DateFormat, CultureInfo.InvariantCulture);

    // ==================================
    // GÜNÜN KURALLARI (tarihten türetilir: herkes için aynı)
    // ==================================

    static System.Random TodayRandom()
    {
        int seed = int.Parse(TodayKey.Replace("-", string.Empty), CultureInfo.InvariantCulture);
        return new System.Random(seed * 7919);
    }

    public static WeaponCatalog.Weapon TodayWeapon
    {
        get
        {
            System.Random random = TodayRandom();
            return WeaponCatalog.All[random.Next(WeaponCatalog.All.Length)];
        }
    }

    // Bölüm 1 kuralsız olduğu için 2. bölümden itibaren seçilir.
    public static int TodayStage
    {
        get
        {
            System.Random random = TodayRandom();
            random.Next();
            return 1 + random.Next(StageThemes.ThemeCount - 1);
        }
    }

    public static Modifier TodayModifier
    {
        get
        {
            System.Random random = TodayRandom();
            random.Next();
            random.Next();
            return (Modifier)random.Next(Enum.GetValues(typeof(Modifier)).Length);
        }
    }

    public static string ModifierName(Modifier modifier)
    {
        switch (modifier)
        {
            case Modifier.DoubleGold: return Loc.Pick("ÇİFT ALTIN", "DOUBLE GOLD");
            case Modifier.FastEnemies: return Loc.Pick("HIZLI DÜŞMANLAR", "FAST ENEMIES");
            case Modifier.GlassCannon: return Loc.Pick("CAM TOP", "GLASS CANNON");
            default: return Loc.Pick("BOSS FIRTINASI", "BOSS RUSH");
        }
    }

    public static string ModifierDescription(Modifier modifier)
    {
        switch (modifier)
        {
            case Modifier.DoubleGold: return Loc.Pick("Düşmanlar iki kat altın düşürür.", "Enemies drop double gold.");
            case Modifier.FastEnemies: return Loc.Pick("Düşmanlar %30 daha hızlı.", "Enemies are 30% faster.");
            case Modifier.GlassCannon: return Loc.Pick("Canın yarıya iner ama %50 fazla hasar verirsin.", "Half health, but 50% more damage.");
            default: return Loc.Pick("Boss her 3 seviyede bir gelir.", "A boss arrives every 3 levels.");
        }
    }

    // ==================================
    // OYUNA ETKİLER (sadece meydan okuma sırasında)
    // ==================================

    static bool Is(Modifier modifier) => IsActive && TodayModifier == modifier;

    public static float GoldMultiplier => Is(Modifier.DoubleGold) ? 2f : 1f;
    public static float EnemySpeedMultiplier => Is(Modifier.FastEnemies) ? 1.3f : 1f;
    public static float PlayerDamageMultiplier => Is(Modifier.GlassCannon) ? 1.5f : 1f;
    public static float PlayerHealthMultiplier => Is(Modifier.GlassCannon) ? 0.5f : 1f;
    public static int LevelsPerBoss => Is(Modifier.BossRush) ? 3 : BossWaveManager.LevelsPerBoss;

    // ==================================
    // BAŞLAT / BİTİR
    // ==================================

    public static void Begin()
    {
        IsActive = true;
        TimeScaleController.ResetAll();
        SceneManager.LoadScene(GameKeys.FirstLevelScene);
    }

    // Ana menü açılınca çağrılır.
    public static void End()
    {
        IsActive = false;
    }

    // ==================================
    // SKOR
    // ==================================

    public static bool PlayedToday => PlayerPrefs.GetString(GameKeys.ChallengeDate, string.Empty) == TodayKey;

    public static int BestToday => PlayedToday ? PlayerPrefs.GetInt(GameKeys.ChallengeBest, 0) : 0;

    public static TimeSpan TimeUntilNext => DateTime.Now.Date.AddDays(1) - DateTime.Now;

    // Oyun bitince çağrılır. Günün ilk oyunuysa katılım ödülü verilir (verilen altını döner).
    public static int SubmitScore(int score)
    {
        int reward = 0;

        if (!PlayedToday)
        {
            PlayerPrefs.SetString(GameKeys.ChallengeDate, TodayKey);
            PlayerPrefs.SetInt(GameKeys.ChallengeBest, 0);
            PlayerProgress.AddGold(ParticipationReward);
            reward = ParticipationReward;
        }

        if (score > PlayerPrefs.GetInt(GameKeys.ChallengeBest, 0))
        {
            PlayerPrefs.SetInt(GameKeys.ChallengeBest, score);
        }

        PlayerPrefs.Save();
        LastReward = reward;
        Leaderboards.ReportScore(score, LeaderboardKind.DailyChallenge);
        GameAnalytics.ChallengePlayed(TodayKey, score);

        return reward;
    }
}
