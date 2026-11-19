using System.Diagnostics;

// Oyun içi bilgi logları (hasar, spawn, XP...).
// Sadece Player Settings > Scripting Define Symbols'a ENABLE_GAME_LOGS
// eklenirse derlenir. Aksi hâlde çağrılar argümanlarıyla birlikte tamamen
// silinir, yani build'de hiçbir maliyeti yoktur.
// Uyarı ve hatalar için Debug.LogWarning / Debug.LogError kullanılmaya devam eder.
public static class GameLog
{
    [Conditional("ENABLE_GAME_LOGS")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }
}
