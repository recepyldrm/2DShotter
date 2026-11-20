#if GPGS_INSTALLED
using System.Collections;
using System.Text;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using UnityEngine;
#endif

// Google Play Games: giriş, liderlik tablosu ve bulut kayıt (Saved Games).
//
// Bu dosya Google Play Games eklentisi kurulunca otomatik derlenir
// (Editor/PluginDefines.cs, eklentiyi görünce GPGS_INSTALLED tanımını ekler).
// Kurulum adımları için StoreListing/kurulum_rehberi.md.

/// <summary>Play Console'dan alınacak kimlikler.</summary>
public static class GooglePlayGamesConfig
{
    // Play Console > Play Games hizmetleri > Liderlik tabloları > tablonun kimliği
    // (örn. "CgkI1a2b3c4dEAIQAQ"). Her zorluğun kendi tablosu var; boş bırakılan
    // zorluğun skorları gönderilmez. Üçü de boşsa liderlik tablosu kapalı kalır.
    public const string LeaderboardIdEasy = "";
    public const string LeaderboardIdNormal = "";
    public const string LeaderboardIdHard = "";

    public static string LeaderboardIdFor(Difficulty.Level difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Level.Easy: return LeaderboardIdEasy;
            case Difficulty.Level.Hard: return LeaderboardIdHard;
            default: return LeaderboardIdNormal;
        }
    }

    public static bool AnyLeaderboard =>
        !string.IsNullOrEmpty(LeaderboardIdEasy) ||
        !string.IsNullOrEmpty(LeaderboardIdNormal) ||
        !string.IsNullOrEmpty(LeaderboardIdHard);

    // Bulut kayıt dosyasının adı (değiştirme; oyuncuların kayıtları bu adla saklanır).
    public const string SaveFileName = "progress";
}

#if GPGS_INSTALLED
public class GooglePlayGamesService : MonoBehaviour, ILeaderboardProvider
{
    // Art arda değişikliklerde her seferinde yüklemesin.
    private const float UploadDelay = 5f;

    private static GooglePlayGamesService instance;

    private bool signedIn;
    private bool syncing;
    private bool uploadPending;
    private Coroutine uploadRoutine;

    public bool IsAvailable =>
        signedIn && GooglePlayGamesConfig.AnyLeaderboard;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (instance != null)
            return;

        GameObject go = new GameObject("GooglePlayGamesService");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GooglePlayGamesService>();
#endif
    }

    void Start()
    {
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(OnAuthenticated);
    }

    void OnDestroy()
    {
        PlayerProgress.OnSaveDataChanged -= ScheduleUpload;
    }

    void OnAuthenticated(SignInStatus status)
    {
        signedIn = status == SignInStatus.Success;

        if (!signedIn)
        {
            Debug.Log("Google Play Games: giriş yapılmadı (" + status + ")");
            return;
        }

        Leaderboards.Provider = this;

        // Önce buluttaki kaydı al ve birleştir, sonra değişiklikleri yükle.
        SyncWithCloud();
        PlayerProgress.OnSaveDataChanged += ScheduleUpload;
    }

    // ==================================
    // LİDERLİK TABLOSU
    // ==================================

    public void ReportScore(long score, Difficulty.Level difficulty)
    {
        string leaderboardId = GooglePlayGamesConfig.LeaderboardIdFor(difficulty);

        if (string.IsNullOrEmpty(leaderboardId))
            return;

        PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, success =>
        {
            if (!success)
            {
                Debug.LogWarning("Google Play Games: skor gönderilemedi.");
            }
        });
    }

    public void ShowLeaderboard()
    {
        // Seçili zorluğun tablosu varsa onu, yoksa bütün tabloların listesini aç.
        string leaderboardId = GooglePlayGamesConfig.LeaderboardIdFor(Difficulty.Current);

        if (string.IsNullOrEmpty(leaderboardId))
            PlayGamesPlatform.Instance.ShowLeaderboardUI();
        else
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
    }

    // ==================================
    // BULUT KAYIT
    // ==================================

    // Buluttaki ve cihazdaki ilerlemeyi birleştirir (SaveSnapshot.Merge),
    // sonucu hem cihaza hem buluta yazar.
    void SyncWithCloud()
    {
        if (syncing)
        {
            uploadPending = true;
            return;
        }

        syncing = true;

        ISavedGameClient savedGames = PlayGamesPlatform.Instance.SavedGame;

        savedGames.OpenWithAutomaticConflictResolution(
            GooglePlayGamesConfig.SaveFileName,
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            (openStatus, metadata) =>
            {
                if (openStatus != SavedGameRequestStatus.Success)
                {
                    Debug.LogWarning("Google Play Games: bulut kaydı açılamadı (" + openStatus + ")");
                    FinishSync();
                    return;
                }

                savedGames.ReadBinaryData(metadata, (readStatus, data) =>
                {
                    SaveSnapshot cloud = readStatus == SavedGameRequestStatus.Success && data != null && data.Length > 0
                        ? SaveSnapshot.FromJson(Encoding.UTF8.GetString(data))
                        : null;

                    SaveSnapshot local = SaveSnapshot.CaptureLocal();
                    SaveSnapshot merged = SaveSnapshot.Merge(local, cloud);

                    if (!merged.ContentEquals(local))
                    {
                        // Uygulama sırasında OnSaveDataChanged tetiklenmesin.
                        PlayerProgress.OnSaveDataChanged -= ScheduleUpload;
                        merged.ApplyToLocal();
                        PlayerProgress.OnSaveDataChanged += ScheduleUpload;
                    }

                    if (merged.ContentEquals(cloud))
                    {
                        FinishSync();
                        return;
                    }

                    SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder()
                        .WithUpdatedDescription("İlerleme")
                        .Build();

                    savedGames.CommitUpdate(metadata, update, Encoding.UTF8.GetBytes(merged.ToJson()), (commitStatus, _) =>
                    {
                        if (commitStatus != SavedGameRequestStatus.Success)
                        {
                            Debug.LogWarning("Google Play Games: bulut kaydı yazılamadı (" + commitStatus + ")");
                        }

                        FinishSync();
                    });
                });
            });
    }

    void FinishSync()
    {
        syncing = false;

        if (uploadPending)
        {
            uploadPending = false;
            ScheduleUpload();
        }
    }

    void ScheduleUpload()
    {
        if (!signedIn)
            return;

        if (uploadRoutine != null)
        {
            StopCoroutine(uploadRoutine);
        }

        uploadRoutine = StartCoroutine(UploadAfterDelay());
    }

    IEnumerator UploadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(UploadDelay);

        uploadRoutine = null;
        SyncWithCloud();
    }

    // Uygulama arka plana alınırken bekleyen değişiklikleri hemen yükle.
    void OnApplicationPause(bool paused)
    {
        if (paused && signedIn && uploadRoutine != null)
        {
            StopCoroutine(uploadRoutine);
            uploadRoutine = null;
            SyncWithCloud();
        }
    }
}
#endif
