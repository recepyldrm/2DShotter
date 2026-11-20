// Birden fazla dosyada kullanılan sabit isimler tek yerde.
// Birini değiştirirsen her yerde birlikte değişir.
public static class GameKeys
{
    // Sahneler
    public const string MainMenuScene = "MainMenu";
    public const string FirstLevelScene = "Level_01";

    // PlayerPrefs
    public const string HighScore = "HighScore";
    public const string MusicVolume = "MusicVolume";
    public const string SfxVolume = "SfxVolume";
    public const string TotalGold = "TotalGold";
    public const string OwnedCompanionSkins = "OwnedCompanionSkins";
    public const string SelectedCompanionSkin = "SelectedCompanionSkin";
    public const string GrantedTransactions = "GrantedTransactions";
    public const string OwnedWeapons = "OwnedWeapons";
    public const string SelectedWeapon = "SelectedWeapon";

    // İstatistikler (başarımlar için)
    public const string StatTotalKills = "StatTotalKills";
    public const string StatTotalRuns = "StatTotalRuns";
    public const string StatBestLevel = "StatBestLevel";
    public const string StatTotalGoldEarned = "StatTotalGoldEarned";
    public const string StatTotalBossKills = "StatTotalBossKills";
    public const string ClaimedAchievements = "ClaimedAchievements";

    // Bulut kayıt: ilerlemenin son değiştiği zaman (UTC, Unix saniye)
    public const string ProgressModifiedAt = "ProgressModifiedAt";

    // Günlük ödül
    public const string DailyLastClaimDate = "DailyLastClaimDate";
    public const string DailyLastClaimIndex = "DailyLastClaimIndex";
    public const string QuestDate = "QuestDate";
    public const string QuestProgressPrefix = "QuestProgress_";
    public const string QuestClaimedPrefix = "QuestClaimed_";

    // Eğitim
    public const string TutorialSeen = "TutorialSeen";
    public const string CardHintSeen = "CardHintSeen";

    // Ayarlar
    public const string VibrationEnabled = "VibrationEnabled";
    public const string Language = "Language";
    public const string GraphicsQuality = "GraphicsQuality";
    public const string Difficulty = "Difficulty";
    public const string IntroSeenPrefix = "IntroSeen_";
    public const string WeaponLevelPrefix = "WeaponLevel_";
    public const string WeeklyQuestWeek = "WeeklyQuestWeek";
    public const string WeeklyQuestProgressPrefix = "WeeklyQuestProgress_";
    public const string WeeklyQuestClaimedPrefix = "WeeklyQuestClaimed_";
    public const string WeeklyChestClaimed = "WeeklyChestClaimed";
    public const string SeasonIndex = "SeasonIndex";
    public const string SeasonPoints = "SeasonPoints";
    public const string SeasonPremium = "SeasonPremium";
    public const string SeasonFreeClaimed = "SeasonFreeClaimed";
    public const string SeasonPremiumClaimed = "SeasonPremiumClaimed";
    public const string OwnedBulletSkins = "OwnedBulletSkins";
    public const string SelectedBulletSkin = "SelectedBulletSkin";
    public const string ChallengeDate = "ChallengeDate";
    public const string ChallengeBest = "ChallengeBest";
    public const string NotificationsEnabled = "NotificationsEnabled";
    public const string NotificationPermissionAsked = "NotificationPermissionAsked";

    // Kalıcı yükseltmeler: önek + yükseltme Id'si (örn. "PermUpgrade_start_health")
    public const string PermanentUpgradePrefix = "PermUpgrade_";

    // ObjectPool anahtarları (ObjectPool inspector'ındaki key'lerle aynı olmalı)
    public const string BulletPool = "Bullet";
    public const string BurnEffectPool = "BurnEffect";
    public const string ChainLightningPool = "ChainLightning";
    public const string ChainHitPool = "ChainHit";
    public const string ExplosionPool = "Explosion";
}
