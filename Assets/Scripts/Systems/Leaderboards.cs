// Liderlik tablosunu sağlayan servis (Google Play Games, Game Center...).
public interface ILeaderboardProvider
{
    // Oyuncu giriş yaptı ve tablo kullanılabilir mi?
    bool IsAvailable { get; }

    // Her zorluğun ayrı tablosu var (kolay modda yapılan skor zor tablosuna girmez).
    void ReportScore(long score, Difficulty.Level difficulty);

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

    public static void ReportScore(long score)
    {
        if (score > 0 && IsAvailable)
        {
            Provider.ReportScore(score, Difficulty.Current);
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
