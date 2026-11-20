using UnityEngine;

// Sonsuz mod zorluğu. Ana menüden seçilir, bir sonraki oyunda geçerli olur.
// Zor modda düşmanlar daha dayanıklı ve sert vurur, karşılığında daha çok altın düşer.
public static class Difficulty
{
    public enum Level
    {
        Easy,
        Normal,
        Hard
    }

    public static Level Current
    {
        // Günlük meydan okuma her zaman Normal zorlukta oynanır.
        get => DailyChallenge.IsActive ? Level.Normal : (Level)Mathf.Clamp(PlayerPrefs.GetInt(GameKeys.Difficulty, (int)Level.Normal), 0, 2);
        set
        {
            PlayerPrefs.SetInt(GameKeys.Difficulty, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static float EnemyHealthMultiplier => Pick(0.7f, 1f, 1.45f);
    public static float EnemyDamageMultiplier => Pick(0.65f, 1f, 1.35f);
    public static float EnemySpeedMultiplier => Pick(0.92f, 1f, 1.1f);
    public static float GoldMultiplier => Pick(0.7f, 1f, 1.4f);

    static float Pick(float easy, float normal, float hard)
    {
        switch (Current)
        {
            case Level.Easy: return easy;
            case Level.Hard: return hard;
            default: return normal;
        }
    }

    public static string Label(Level level)
    {
        switch (level)
        {
            case Level.Easy: return Loc.T("KOLAY");
            case Level.Hard: return Loc.T("ZOR");
            default: return Loc.T("NORMAL");
        }
    }

    // Ana menüde butonun altında görünen kısa açıklama
    public static string Description(Level level)
    {
        switch (level)
        {
            case Level.Easy: return Loc.Pick("Zayıf düşmanlar · %70 altın", "Weaker enemies · 70% gold");
            case Level.Hard: return Loc.Pick("Güçlü düşmanlar · %140 altın", "Stronger enemies · 140% gold");
            default: return Loc.Pick("Dengeli · %100 altın", "Balanced · 100% gold");
        }
    }
}
