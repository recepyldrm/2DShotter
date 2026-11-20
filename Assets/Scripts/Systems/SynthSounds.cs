using System.Collections.Generic;
using UnityEngine;

// Kodla üretilen kısa ses efektleri (ek ses dosyası gerekmez).
// AudioManager'daki ilgili alana gerçek bir ses dosyası atanırsa o kullanılır.
public static class SynthSounds
{
    public enum Sound
    {
        Laser,
        Shotgun,
        Minigun,
        Sniper,
        Rocket,
        ShieldHit,
        ShieldBreak,
        RewardChime,
        QuestComplete
    }

    private const int SampleRate = 22050;

    private static readonly Dictionary<Sound, AudioClip> cache = new Dictionary<Sound, AudioClip>();

    // Gürültü her seferinde aynı olsun (sesler her açılışta birebir aynı duyulsun)
    private static System.Random noise;

    public static AudioClip Get(Sound sound)
    {
        if (cache.TryGetValue(sound, out AudioClip clip) && clip != null)
            return clip;

        noise = new System.Random(1234 + (int)sound);
        clip = Create(sound);
        cache[sound] = clip;
        return clip;
    }

    static AudioClip Create(Sound sound)
    {
        switch (sound)
        {
            case Sound.Laser: return Build("Laser", 0.14f, Laser);
            case Sound.Shotgun: return Build("Shotgun", 0.26f, Shotgun);
            case Sound.Minigun: return Build("Minigun", 0.06f, Minigun);
            case Sound.Sniper: return Build("Sniper", 0.32f, Sniper);
            case Sound.Rocket: return Build("Rocket", 0.34f, Rocket);
            case Sound.ShieldHit: return Build("ShieldHit", 0.16f, ShieldHit);
            case Sound.ShieldBreak: return Build("ShieldBreak", 0.4f, ShieldBreak);
            case Sound.RewardChime: return Build("RewardChime", 0.55f, RewardChime);
            default: return Build("QuestComplete", 0.42f, QuestComplete);
        }
    }

    static AudioClip Build(string name, float seconds, System.Func<float, float, float> generator)
    {
        int length = Mathf.CeilToInt(seconds * SampleRate);
        float[] data = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / SampleRate;
            data[i] = Mathf.Clamp(generator(t, t / seconds), -1f, 1f);
        }

        // Başta / sonda tık sesi olmasın
        int fade = Mathf.Min(64, length / 4);
        for (int i = 0; i < fade; i++)
        {
            float k = (float)i / fade;
            data[i] *= k;
            data[length - 1 - i] *= k;
        }

        AudioClip clip = AudioClip.Create("Synth_" + name, length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==================================
    // ÜRETİCİLER (t: saniye, p: 0-1 ilerleme)
    // ==================================

    static float Noise() => (float)(noise.NextDouble() * 2.0 - 1.0);

    static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);

    // Perdesi hızla düşen ince "zap"
    static float Laser(float t, float p)
    {
        float frequency = Mathf.Lerp(1900f, 380f, Mathf.Sqrt(p));
        float square = Mathf.Sign(Sine(frequency, t)) * 0.35f + Sine(frequency * 2f, t) * 0.25f;
        return square * Mathf.Exp(-p * 3.5f) * 0.6f;
    }

    // Tok patlama: gürültü + alçak darbe
    static float Shotgun(float t, float p)
    {
        float body = Noise() * Mathf.Exp(-p * 7f) * 0.7f;
        float thump = Sine(Mathf.Lerp(120f, 45f, p), t) * Mathf.Exp(-p * 5f) * 0.8f;
        return body + thump;
    }

    // Çok kısa, kuru "tık"
    static float Minigun(float t, float p)
    {
        return (Noise() * 0.6f + Sine(900f, t) * 0.3f) * Mathf.Exp(-p * 9f);
    }

    // Keskin çatlama + derin yankı
    static float Sniper(float t, float p)
    {
        float crack = Noise() * Mathf.Exp(-t * 60f) * 1.1f;
        float boom = Sine(Mathf.Lerp(90f, 40f, p), t) * Mathf.Exp(-p * 4f) * 0.75f;
        float tail = Noise() * 0.12f * Mathf.Exp(-p * 3f);
        return crack + boom + tail;
    }

    // Yükselen ıslık + gürültü (fırlatma)
    static float Rocket(float t, float p)
    {
        float whoosh = Noise() * 0.45f * Mathf.Sin(Mathf.PI * p);
        float whistle = Sine(Mathf.Lerp(300f, 700f, p), t) * 0.25f * (1f - p);
        float thump = Sine(70f, t) * Mathf.Exp(-t * 25f) * 0.7f;
        return whoosh + whistle + thump;
    }

    // Metalik tın (uyumsuz frekanslar)
    static float ShieldHit(float t, float p)
    {
        float ring = Sine(1250f, t) * 0.4f + Sine(1870f, t) * 0.3f + Sine(2640f, t) * 0.2f;
        return (ring + Noise() * 0.15f * Mathf.Exp(-t * 80f)) * Mathf.Exp(-p * 6f) * 0.7f;
    }

    // Cam kırılması: yüksek tınılar + gürültü
    static float ShieldBreak(float t, float p)
    {
        float shards = Sine(2200f + 300f * Mathf.Sin(t * 90f), t) * 0.3f + Sine(3300f, t) * 0.2f;
        float crash = Noise() * Mathf.Exp(-p * 5f) * 0.6f;
        return (shards * Mathf.Exp(-p * 4f)) + crash;
    }

    // Ödül: yükselen dört nota (Do-Mi-Sol-Do)
    static float RewardChime(float t, float p)
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        float step = 0.11f;
        float value = 0f;

        for (int i = 0; i < notes.Length; i++)
        {
            float local = t - i * step;
            if (local < 0f) continue;

            float envelope = Mathf.Exp(-local * 7f);
            value += (Sine(notes[i], local) * 0.5f + Sine(notes[i] * 2f, local) * 0.15f) * envelope;
        }

        return value * 0.45f;
    }

    // Görev tamamlandı: iki notalı kısa "ding-dong"
    static float QuestComplete(float t, float p)
    {
        float first = t < 0.14f ? Sine(880f, t) * Mathf.Exp(-t * 12f) : 0f;
        float local = t - 0.12f;
        float second = local > 0f ? Sine(1318.5f, local) * Mathf.Exp(-local * 8f) : 0f;
        return (first + second) * 0.5f;
    }
}
