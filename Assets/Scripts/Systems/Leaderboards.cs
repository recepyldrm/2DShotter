// Hangi liderlik tablosu: her zorluğun ayrı tablosu ve günlük meydan okuma tablosu.
public enum LeaderboardKind
{
    Easy,
    Normal,
    Hard,
    DailyChallenge
}

// Liderlik tablosunu sağlayan servis (Google Play Games, Game Center...).
public interface ILeaderboardProvider
{
    // Oyuncu giriş yaptı ve tablo kullanılabilir mi?
    bool IsAvailable { get; }

    void ReportScore(long score, LeaderboardKind kind);

    // Servisin kendi liderlik ekranını açar (arkadaşlar / herkes).
    void ShowLeaderboard();
}

// Oyunun geri kalanı liderlik tablosuna sadece buradan erişir.
// Google Play Games eklentisi kurulup bir sağlayıcı Provider'a atanana kadar
// skorlar gönderilmez ve menüdeki LİDERLİK butonu görünmez.
public static class Leaderboards
{
    public static ILeaderboardProvider Provider { get; set; }

    public static bool IsAvailable =>
        Provider != null && Provider.IsAvailable;

    // Seçili zorluğun tablosu
    public static LeaderboardKind CurrentDifficultyKind
    {
        get
        {
            switch (Difficulty.Current)
            {
                case Difficulty.Level.Easy: return LeaderboardKind.Easy;
                case Difficulty.Level.Hard: return LeaderboardKind.Hard;
                default: return LeaderboardKind.Normal;
            }
        }
    }

    // Normal oyun skoru: seçili zorluğun tablosuna gider.
    public static void ReportScore(long score)
    {
        ReportScore(score, CurrentDifficultyKind);
    }

    public static void ReportScore(long score, LeaderboardKind kind)
    {
        if (score > 0 && IsAvailable)
        {
            Provider.ReportScore(score, kind);
        }
    }

    public static void Show()
    {
        if (IsAvailable)
        {
            Provider.ShowLeaderboard();
        }
    }
}
