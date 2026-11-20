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
        int count = QualitySettings.names.Length;

        if (count > 0)
        {
            int index;

            switch (Current)
            {
                case Level.Low: index = 0; break;
                case Level.Medium: index = count / 2; break;
                default: index = count - 1; break;
            }

            if (QualitySettings.GetQualityLevel() != index)
            {
                QualitySettings.SetQualityLevel(index, true);
            }
        }

        // vSync açıkken targetFrameRate yok sayılır; mobilde zaten kapalıdır.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Current == Level.Low ? 30 : 60;
    }
}
