using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Oyunu durduran her sistem (pause, kart ekranı, shop, game over) kendi
// isteğini ekler / kaldırır. En az bir istek varsa oyun durur.
// Böylece örn. kart ekranı açıkken Pause -> Resume yapmak oyunu başlatmaz.
//
// Ayrıca vuruş hissi için çok kısa "hit-stop" (zamanı bir anlığına yavaşlatma)
// destekler; oyun durmuşsa hit-stop etkisizdir.
public static class TimeScaleController
{
    private static readonly HashSet<object> pauseRequests = new HashSet<object>();

    private static float hitStopUntil;
    private static float hitStopScale = 1f;
    private static float lastHitStopTime = -10f;

    public static bool IsPaused => pauseRequests.Count > 0;

    public static void Pause(object requester)
    {
        if (requester == null)
            return;

        pauseRequests.Add(requester);
        Apply();
    }

    public static void Resume(object requester)
    {
        if (requester == null)
            return;

        pauseRequests.Remove(requester);
        Apply();
    }

    public static void ResetAll()
    {
        pauseRequests.Clear();
        hitStopUntil = 0f;
        Apply();
    }

    // Zamanı kısa süre yavaşlatır. minInterval: art arda öldürmelerde
    // oyunun sürekli takılmaması için iki hit-stop arası en az süre.
    public static void HitStop(float duration, float scale = 0.05f, float minInterval = 0.25f)
    {
        if (IsPaused)
            return;

        float now = Time.unscaledTime;

        if (now - lastHitStopTime < minInterval)
            return;

        lastHitStopTime = now;
        hitStopUntil = now + duration;
        hitStopScale = Mathf.Clamp01(scale);

        HitStopRunner.Ensure();
        Apply();
    }

    static void Apply()
    {
        if (IsPaused)
        {
            Time.timeScale = 0f;
            return;
        }

        Time.timeScale = Time.unscaledTime < hitStopUntil ? hitStopScale : 1f;
    }

    // Hit-stop süresi dolunca zamanı geri açar.
    private class HitStopRunner : MonoBehaviour
    {
        private static HitStopRunner instance;

        public static void Ensure()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("TimeScaleHitStop");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStopRunner>();
        }

        void Update()
        {
            if (hitStopUntil > 0f && Time.unscaledTime >= hitStopUntil)
            {
                hitStopUntil = 0f;
                Apply();
            }
        }
    }

    // Her sahne yüklemesinde (restart, ana menü) bütün istekler temizlenir.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        ResetAll();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            ResetAll();
        }
    }
}
