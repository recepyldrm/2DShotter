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

    // İstatistikler (başarımlar için)
    public const string StatTotalKills = "StatTotalKills";
    public const string StatTotalRuns = "StatTotalRuns";
    public const string StatBestLevel = "StatBestLevel";
    public const string StatTotalGoldEarned = "StatTotalGoldEarned";
    public const string ClaimedAchievements = "ClaimedAchievements";

    // Bulut kayıt: ilerlemenin son değiştiği zaman (UTC, Unix saniye)
    public const string ProgressModifiedAt = "ProgressModifiedAt";

    // Günlük ödül
    public const string DailyLastClaimDate = "DailyLastClaimDate";
    public const string DailyLastClaimIndex = "DailyLastClaimIndex";

    // Eğitim
    public const string TutorialSeen = "TutorialSeen";
    public const string CardHintSeen = "CardHintSeen";

    // Ayarlar
    public const string VibrationEnabled = "VibrationEnabled";

    // Kalıcı yükseltmeler: önek + yükseltme Id'si (örn. "PermUpgrade_start_health")
    public const string PermanentUpgradePrefix = "PermUpgrade_";

    // ObjectPool anahtarları (ObjectPool inspector'ındaki key'lerle aynı olmalı)
    public const string BulletPool = "Bullet";
    public const string BurnEffectPool = "BurnEffect";
    public const string ChainLightningPool = "ChainLightning";
    public const string ChainHitPool = "ChainHit";
    public const string ExplosionPool = "Explosion";
}
