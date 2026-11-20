using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameLanguage
{
    Turkish,
    English
}

// Basit çeviri: metinler Türkçe yazılır, İngilizce seçiliyse sözlükten karşılığı alınır.
// Karşılığı olmayan metin Türkçe kalır. UIBuilder.CreateText her metni buradan geçirir;
// sahnelere elle konmuş yazılar da sahne açılınca çevrilir.
public static class Loc
{
    public static GameLanguage Current
    {
        get => (GameLanguage)PlayerPrefs.GetInt(GameKeys.Language, (int)DefaultLanguage);
        set
        {
            PlayerPrefs.SetInt(GameKeys.Language, (int)value);
            PlayerPrefs.Save();
        }
    }

    // Telefon Türkçe ise Türkçe, değilse İngilizce başlar.
    static GameLanguage DefaultLanguage =>
        Application.systemLanguage == SystemLanguage.Turkish ? GameLanguage.Turkish : GameLanguage.English;

    public static bool IsEnglish => Current == GameLanguage.English;

    public static string T(string turkish)
    {
        if (!IsEnglish || string.IsNullOrEmpty(turkish))
            return turkish;

        if (LocEnglish.Table.TryGetValue(turkish, out string english))
            return english;

        // Baş / sondaki boşluklar yüzünden eşleşmeyen sahne yazıları için
        string trimmed = turkish.Trim();

        if (trimmed.Length != turkish.Length && LocEnglish.Table.TryGetValue(trimmed, out english))
            return english;

        return turkish;
    }

    // Kısa yol: Türkçe / İngilizce metni yerinde seç (sayı içeren dinamik metinler için)
    public static string Pick(string turkish, string english) => IsEnglish ? english : turkish;

    public static string LanguageName(GameLanguage language) =>
        language == GameLanguage.English ? "ENGLISH" : "TÜRKÇE";

    // Dil değişince açık sahne yeniden yüklenir; bütün arayüz yeni dille kurulur.
    public static void SetLanguage(GameLanguage language)
    {
        if (Current == language)
            return;

        Current = language;
        TimeScaleController.ResetAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ==================================
    // SAHNE YAZILARI
    // ==================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnglish)
            return;

        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text.gameObject.scene == scene)
            {
                text.text = T(text.text);
            }
        }

        foreach (UnityEngine.UI.Text text in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include))
        {
            if (text.gameObject.scene == scene)
            {
                text.text = T(text.text);
            }
        }
    }
}
