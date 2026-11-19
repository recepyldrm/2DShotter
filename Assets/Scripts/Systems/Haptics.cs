using UnityEngine;

// Mobilde kısa titreşimler. Ayarlardan kapatılabilir.
// Android'de süre ve şiddet ayarlanabilir kısa titreşim kullanılır;
// iOS'ta eklentisiz kısa titreşim olmadığı için sadece güçlü olaylarda titrer.
public static class Haptics
{
    // Çok sık olaylarda (öldürme) telefon sürekli titremesin.
    private const float MinInterval = 0.12f;

    private static float lastVibrateTime = -1f;

    // Unity, VIBRATE iznini sadece Handheld.Vibrate koddan çağrılabiliyorsa ekler.
    // Bu alan hiçbir zaman true olmaz; sadece iznin eklenmesini sağlar.
    private static readonly bool neverTrue = System.DateTime.Now.Year < 2000;

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(GameKeys.VibrationEnabled, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(GameKeys.VibrationEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void Light() => Vibrate(15, 60, false);
    public static void Medium() => Vibrate(35, 140, false);
    public static void Heavy() => Vibrate(70, 255, true);

    static void Vibrate(long milliseconds, int amplitude, bool strong)
    {
        if (neverTrue)
        {
            Handheld.Vibrate();
        }

        if (!Enabled)
            return;

        // Güçlü titreşimler (ölüm, patlama) sınıra takılmaz.
        if (!strong && Time.unscaledTime - lastVibrateTime < MinInterval)
            return;

        lastVibrateTime = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(milliseconds, amplitude);
#elif UNITY_IOS && !UNITY_EDITOR
        if (strong)
        {
            Handheld.Vibrate();
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int sdkVersion = -1;

    static void VibrateAndroid(long milliseconds, int amplitude)
    {
        try
        {
            if (vibrator == null)
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    sdkVersion = version.GetStatic<int>("SDK_INT");
                }
            }

            if (vibrator == null)
                return;

            // Android 8.0+ : süre + şiddet
            if (sdkVersion >= 26)
            {
                using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                    "createOneShot",
                    milliseconds,
                    Mathf.Clamp(amplitude, 1, 255)))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("Haptics: Titreşim başarısız: " + exception.Message);
        }
    }
#endif
}
