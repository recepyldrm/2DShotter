using UnityEngine;

// Grafik kalitesi ayarı: Düşük / Orta / Yüksek.
// Unity kalite seviyesini ve kare hızını birlikte ayarlar. Oyun açılırken kendiliğinden uygulanır.
public static class GraphicsQuality
{
    public enum Level
    {
        Low,
        Medium,
        High
    }

    public static Level Current
    {
        get => (Level)Mathf.Clamp(PlayerPrefs.GetInt(GameKeys.GraphicsQuality, (int)Level.High), 0, 2);
        set
        {
            PlayerPrefs.SetInt(GameKeys.GraphicsQuality, (int)value);
            PlayerPrefs.Save();
            Apply();
        }
    }

    // Düşük ayarda bazı süs efektleri azaltılır (hasar sayıları, sarsıntı vb. isterse buna bakar).
    public static bool ReducedEffects => Current == Level.Low;

    public static string Label(Level level)
    {
        switch (level)
        {
            case Level.Low: return Loc.T("DÜŞÜK");
            case Level.Medium: return Loc.T("ORTA");
            default: return Loc.T("YÜKSEK");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Apply();
    }

    public static void Apply()
    {
        // Kalite seviyeleri adlarıyla seçilir. "Very Low" bilerek kullanılmaz:
        // dokuları yarı çözünürlüğe indirir, pixel art bulanıklaşır.
        string wanted;

        switch (Current)
        {
            case Level.Low: wanted = "Low"; break;
            case Level.Medium: wanted = "Medium"; break;
            default: wanted = "High"; break;
        }

        int index = System.Array.IndexOf(QualitySettings.names, wanted);

        if (index >= 0 && QualitySettings.GetQualityLevel() != index)
        {
            QualitySettings.SetQualityLevel(index, true);
        }

        // vSync açıkken targetFrameRate yok sayılır; mobilde zaten kapalıdır.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Current == Level.Low ? 30 : 60;
    }
}
