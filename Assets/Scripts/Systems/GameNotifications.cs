using System;
using UnityEngine;

#if NOTIFICATIONS_INSTALLED && UNITY_ANDROID
using Unity.Notifications.Android;
#endif

// Telefon bildirimleri: oyuncuyu geri getiren hatırlatmalar.
//  - "Günlük ödülün hazır!"
//  - "Sezon 2 gün sonra bitiyor"
//  - 3 gün oynanmazsa "Askerlerin seni bekliyor"
// Uygulama arka plana geçince planlanır, açılınca hepsi iptal edilir.
// Unity "Mobile Notifications" paketi kurulunca çalışır
// (Editor/PluginDefines NOTIFICATIONS_INSTALLED tanımını kendiliğinden ekler).
// Ayarlardan kapatılabilir.
public static class GameNotifications
{
    private const string ChannelId = "reminders";

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(GameKeys.NotificationsEnabled, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(GameKeys.NotificationsEnabled, value ? 1 : 0);
            PlayerPrefs.Save();

            if (!value)
                CancelAll();
        }
    }

    // Bildirim paketi kurulu mu? (Ayarlarda satırı göstermek için)
    public static bool IsSupported
    {
        get
        {
#if NOTIFICATIONS_INSTALLED && UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
#if NOTIFICATIONS_INSTALLED && UNITY_ANDROID && !UNITY_EDITOR
        AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(
            ChannelId,
            Loc.Pick("Hatırlatmalar", "Reminders"),
            Loc.Pick("Günlük ödül ve sezon hatırlatmaları", "Daily reward and season reminders"),
            Importance.Default
        ));

        // Android 13+: bildirim izni (ilk açılışta bir kez sorulur)
        if (Enabled && PlayerPrefs.GetInt(GameKeys.NotificationPermissionAsked, 0) == 0)
        {
            PlayerPrefs.SetInt(GameKeys.NotificationPermissionAsked, 1);
            PlayerPrefs.Save();
            new PermissionRequest();
        }

        CancelAll();

        GameObject host = new GameObject("GameNotifications");
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.AddComponent<NotificationScheduler>();
#endif
    }

    // ==================================
    // PLANLAMA
    // ==================================

    // Arka plana geçerken çağrılır.
    public static void ScheduleAll()
    {
        CancelAll();

        if (!Enabled)
            return;

        DateTime now = DateTime.Now;

        // Günlük ödül: alınmadıysa bu akşam 19:00, alındıysa yarın öğlen
        DateTime rewardTime = DailyReward.CanClaim
            ? (now.Hour < 19 ? now.Date.AddHours(19) : now.Date.AddDays(1).AddHours(12))
            : now.Date.AddDays(1).AddHours(12);

        Send(
            Loc.Pick("Günlük ödülün hazır!", "Your daily reward is ready!"),
            Loc.Pick("Gel, altınını al ve serini bozma.", "Come grab your gold and keep your streak."),
            rewardTime
        );

        // Sezon bitmeden 2 gün önce (sezon tamamlanmadıysa)
        DateTime seasonWarning = SeasonPass.SeasonEnd.AddDays(-2).Date.AddHours(18);

        if (seasonWarning > now && SeasonPass.Tier < SeasonPass.TierCount)
        {
            Send(
                Loc.Pick("Sezon bitmek üzere!", "The season is almost over!"),
                Loc.Pick("2 gün kaldı. Ödüllerini almayı unutma.", "2 days left. Don't miss your rewards."),
                seasonWarning
            );
        }

        // 3 gün oynanmazsa
        Send(
            Loc.Pick("Askerlerin seni bekliyor", "Your soldiers are waiting"),
            Loc.Pick("Yeni görevler ve günlük meydan okuma seni bekliyor.", "New quests and a daily challenge are waiting for you."),
            now.AddDays(3)
        );
    }

    static void Send(string title, string text, DateTime fireTime)
    {
#if NOTIFICATIONS_INSTALLED && UNITY_ANDROID && !UNITY_EDITOR
        AndroidNotification notification = new AndroidNotification
        {
            Title = title,
            Text = text,
            FireTime = fireTime
        };

        AndroidNotificationCenter.SendNotification(notification, ChannelId);
#endif
    }

    public static void CancelAll()
    {
#if NOTIFICATIONS_INSTALLED && UNITY_ANDROID && !UNITY_EDITOR
        AndroidNotificationCenter.CancelAllNotifications();
#endif
    }
}

// Uygulama arka plana geçince bildirimleri planlar, öne gelince iptal eder.
public class NotificationScheduler : MonoBehaviour
{
    void OnApplicationPause(bool paused)
    {
        if (paused)
            GameNotifications.ScheduleAll();
        else
            GameNotifications.CancelAll();
    }

    void OnApplicationQuit()
    {
        GameNotifications.ScheduleAll();
    }
}
