using UnityEngine;
using UnityEngine.SceneManagement;

// Ana menü sahnesi açılınca geliştirmeler, başarımlar, günlük ödül, eğitim
// ekranlarını ve liderlik butonunu otomatik ekler. Sahneye elle
// eklenmişlerse tekrar eklemez.
public static class MainMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameKeys.MainMenuScene)
            return;

        GameObject host = null;

        AddIfMissing<WeaponsUI>(ref host);
        AddIfMissing<PermanentUpgradesUI>(ref host);
        AddIfMissing<AchievementsUI>(ref host);
        AddIfMissing<DailyRewardUI>(ref host);
        AddIfMissing<DailyQuestsUI>(ref host);
        AddIfMissing<TutorialUI>(ref host);
        AddIfMissing<LeaderboardButton>(ref host);
    }

    static void AddIfMissing<T>(ref GameObject host) where T : Component
    {
        if (Object.FindAnyObjectByType<T>() != null)
            return;

        if (host == null)
        {
            host = new GameObject("MainMenuFeatures");
        }

        host.AddComponent<T>();
    }
}
