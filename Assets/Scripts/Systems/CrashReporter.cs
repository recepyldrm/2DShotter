using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

#if FIREBASE_CRASHLYTICS
using Firebase.Crashlytics;
#endif

// Hata ve çökme raporlama.
//  - Yakalanmayan hataları (exception) ve Debug.LogError kayıtlarını toplar.
//  - Aynı hatayı bir kez kaydeder, kaç kez olduğunu sayar.
//  - Cihazda persistentDataPath/error_log.txt dosyasına yazar (son 100 hata).
//  - Her yeni hata türünü analitiğe "error" olayı olarak bildirir.
//  - Firebase Crashlytics kuruluysa (FIREBASE_CRASHLYTICS) hataları oraya da gönderir.
// Unity Cloud Diagnostics açıksa motor çökmeleri ayrıca Unity panosuna gider.
// Kendiliğinden başlar; sahneye eklemek gerekmez.
public static class CrashReporter
{
    private const int MaxEntries = 100;
    private const string FileName = "error_log.txt";

    private class Entry
    {
        public string signature;
        public string type;
        public string message;
        public string stack;
        public string context;
        public string firstSeen;
        public int count;
    }

    private static readonly object gate = new object();
    private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
    private static readonly List<Entry> order = new List<Entry>();
    private static readonly Queue<Entry> pendingAnalytics = new Queue<Entry>();

    private static bool dirty;
    private static bool initialized;

    // Son durum bilgisi (hata kaydına eklenir); ana iş parçacığında güncellenir.
    private static string contextLine = string.Empty;

    public static string LogPath => Path.Combine(Application.persistentDataPath, FileName);

    public static int SessionErrorCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (initialized)
            return;

        initialized = true;

        LoadExisting();
        Application.logMessageReceivedThreaded += OnLog;

        GameObject host = new GameObject("CrashReporter");
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.hideFlags = HideFlags.HideInHierarchy;
        host.AddComponent<CrashReporterHost>();

        InitCrashlytics();
    }

    // ==================================
    // YAKALAMA (her iş parçacığından çağrılabilir)
    // ==================================

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)
            return;

        // Kendi yazdığımız uyarı niteliğindeki "eksik ayar" hatalarını ayırmak zor;
        // hepsini kaydediyoruz ama sayıyı sınırlı tutuyoruz.
        string firstStackLine = FirstLine(stackTrace);
        string signature = type + "|" + Trim(condition, 200) + "|" + firstStackLine;

        lock (gate)
        {
            SessionErrorCount++;

            if (entries.TryGetValue(signature, out Entry existing))
            {
                existing.count++;
                dirty = true;
                return;
            }

            Entry entry = new Entry
            {
                signature = signature,
                type = type.ToString(),
                message = Trim(condition, 500),
                stack = Trim(stackTrace, 1500),
                context = contextLine,
                firstSeen = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                count = 1
            };

            entries[signature] = entry;
            order.Add(entry);

            while (order.Count > MaxEntries)
            {
                entries.Remove(order[0].signature);
                order.RemoveAt(0);
            }

            pendingAnalytics.Enqueue(entry);
            dirty = true;
        }

#if FIREBASE_CRASHLYTICS
        // Unity hatalarını Crashlytics'e "yakalanmamış hata" olarak ilet.
        if (type == LogType.Exception)
        {
            Crashlytics.LogException(new Exception(condition + "\n" + stackTrace));
        }
        else
        {
            Crashlytics.Log(type + ": " + condition);
        }
#endif
    }

    static string FirstLine(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        int newline = text.IndexOf('\n');
        return (newline >= 0 ? text.Substring(0, newline) : text).Trim();
    }

    static string Trim(string text, int max)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= max ? text : text.Substring(0, max) + "…";
    }

    // ==================================
    // ANA İŞ PARÇACIĞI (CrashReporterHost çağırır)
    // ==================================

    internal static void Tick(bool refreshContext)
    {
        if (refreshContext)
            UpdateContext();

        Entry[] toReport = null;

        lock (gate)
        {
            if (pendingAnalytics.Count > 0)
            {
                toReport = pendingAnalytics.ToArray();
                pendingAnalytics.Clear();
            }
        }

        if (toReport != null)
        {
            foreach (Entry entry in toReport)
            {
                GameAnalytics.ErrorLogged(entry.type, Trim(entry.message, 120), FirstLine(entry.stack));
            }
        }
    }

    static void UpdateContext()
    {
        int level = LevelSystem.Instance != null ? LevelSystem.Instance.currentLevel : 0;

        contextLine =
            "scene=" + SceneManager.GetActiveScene().name +
            " level=" + level +
            " weapon=" + WeaponCatalog.Selected.id +
            " difficulty=" + Difficulty.Current +
            (DailyChallenge.IsActive ? " challenge" : string.Empty) +
            " boss=" + BossWaveManager.IsBossActive;

#if FIREBASE_CRASHLYTICS
        Crashlytics.SetCustomKey("scene", SceneManager.GetActiveScene().name);
        Crashlytics.SetCustomKey("level", level.ToString());
        Crashlytics.SetCustomKey("weapon", WeaponCatalog.Selected.id);
#endif
    }

    // Arka plana geçerken / kapanırken ve belirli aralıklarla diske yazar.
    internal static void Flush()
    {
        string text;

        lock (gate)
        {
            if (!dirty)
                return;

            dirty = false;
            text = BuildReport();
        }

        try
        {
            File.WriteAllText(LogPath, text, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            // Burada Debug.LogError kullanmıyoruz: kendi kendini tetiklerdi.
            Debug.LogWarning("CrashReporter: kayıt yazılamadı: " + exception.Message);
        }
    }

    static string BuildReport()
    {
        StringBuilder text = new StringBuilder();

        text.Append("POWER STATION hata kaydı\n");
        text.Append("Sürüm: ").Append(Application.version)
            .Append("  Unity: ").Append(Application.unityVersion)
            .Append("  Cihaz: ").Append(SystemInfo.deviceModel)
            .Append("  Sistem: ").Append(SystemInfo.operatingSystem)
            .Append("  Bellek: ").Append(SystemInfo.systemMemorySize).Append(" MB\n\n");

        for (int i = order.Count - 1; i >= 0; i--)
        {
            Entry entry = order[i];

            text.Append("=== ").Append(entry.type).Append(" ×").Append(entry.count)
                .Append("  (ilk: ").Append(entry.firstSeen).Append(")\n");
            text.Append(entry.message).Append('\n');

            if (!string.IsNullOrEmpty(entry.context))
                text.Append("[").Append(entry.context).Append("]\n");

            text.Append(entry.stack).Append("\n\n");
        }

        return text.ToString();
    }

    public static string PreviousLogPath => Path.Combine(Application.persistentDataPath, "error_log_previous.txt");

    // Önceki oturumun kaydı silinmesin: error_log_previous.txt olarak saklanır
    // (oyun çöktüyse sebebi orada görünür).
    static void LoadExisting()
    {
        try
        {
            if (File.Exists(LogPath))
            {
                if (File.Exists(PreviousLogPath))
                    File.Delete(PreviousLogPath);

                File.Move(LogPath, PreviousLogPath);
            }
        }
        catch (Exception)
        {
            // Dosya işlemi başarısızsa yeni kayıtla devam
        }
    }

    // ==================================
    // TEST MENÜSÜ / DESTEK
    // ==================================

    // Kaydı panoya kopyalar (oyuncu destek isterken gönderebilir). Kaydın kendisini döner.
    public static string CopyToClipboard()
    {
        Flush();

        string text;

        lock (gate)
        {
            text = order.Count > 0 ? BuildReport() : "Hata kaydı boş.";
        }

        GUIUtility.systemCopyBuffer = text;
        return text;
    }

    public static void Clear()
    {
        lock (gate)
        {
            entries.Clear();
            order.Clear();
            pendingAnalytics.Clear();
            dirty = false;
            SessionErrorCount = 0;
        }

        try
        {
            if (File.Exists(LogPath))
                File.Delete(LogPath);
        }
        catch (Exception)
        {
        }
    }

    // Test menüsü: raporlamanın çalıştığını görmek için bilerek hata üretir.
    public static void ThrowTestException()
    {
        throw new InvalidOperationException("CrashReporter test hatası (bilerek üretildi)");
    }

    // ==================================
    // FIREBASE CRASHLYTICS
    // ==================================

    static void InitCrashlytics()
    {
#if FIREBASE_CRASHLYTICS
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                // Varsayılan uygulama oluşturulunca Crashlytics çökmeleri kendiliğinden yakalar.
                _ = Firebase.FirebaseApp.DefaultInstance;
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            }
            else
            {
                Debug.LogWarning("Crashlytics başlatılamadı: " + task.Result);
            }
        });
#endif
    }
}

// Ana iş parçacığında kayıtları analitiğe iletir ve diske yazar.
public class CrashReporterHost : MonoBehaviour
{
    private float flushTimer;
    private float contextTimer;

    void Update()
    {
        contextTimer -= Time.unscaledDeltaTime;
        bool refreshContext = contextTimer <= 0f;
        if (refreshContext)
            contextTimer = 2f;

        CrashReporter.Tick(refreshContext);

        flushTimer -= Time.unscaledDeltaTime;

        if (flushTimer <= 0f)
        {
            flushTimer = 10f;
            CrashReporter.Flush();
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            CrashReporter.Flush();
    }

    void OnApplicationQuit()
    {
        CrashReporter.Flush();
    }
}
