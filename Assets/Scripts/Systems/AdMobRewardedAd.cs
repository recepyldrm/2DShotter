#if ADMOB_INSTALLED
using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
#endif

// AdMob ödüllü reklam kimlikleri.
public static class AdMobConfig
{
    // Google'ın herkese açık TEST reklam birimi: gerçek para kazandırmaz ama
    // reklamın çalıştığını görmek için kullanılır. Yayından önce AdMob panelindeki
    // kendi "Ödüllü" reklam biriminin kimliğiyle değiştir.
    public const string TestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    // Kendi reklam birimin (örn. "ca-app-pub-1234567890123456/1234567890"). Boşsa test kimliği kullanılır.
    public const string RewardedAdUnitId = "";

    public static string ActiveRewardedAdUnitId =>
        string.IsNullOrEmpty(RewardedAdUnitId) ? TestRewardedAdUnitId : RewardedAdUnitId;
}

// AdMob ödüllü reklam sağlayıcısı (AdsManager bunu kullanır).
// Bu dosya Google Mobile Ads eklentisi kurulunca otomatik derlenir
// (Editor/PluginDefines.cs, eklentiyi görünce ADMOB_INSTALLED tanımını ekler).
#if ADMOB_INSTALLED
public class AdMobRewardedAd : MonoBehaviour, IRewardedAdProvider
{
    private const float RetryDelay = 15f;

    private RewardedAd ad;
    private bool loading;
    private bool rewardEarned;
    private Action<bool> onFinished;

    public bool IsReady => ad != null && ad.CanShowAd();

    public static AdMobRewardedAd Create()
    {
        GameObject go = new GameObject("AdMobRewardedAd");
        DontDestroyOnLoad(go);

        AdMobRewardedAd provider = go.AddComponent<AdMobRewardedAd>();
        provider.RequestConsentAndInitialize();

        return provider;
    }

    // AB / Birleşik Krallık kullanıcıları için zorunlu gizlilik izni (UMP).
    // İzin alınabiliyorsa ya da gerekmiyorsa reklamlar başlatılır.
    void RequestConsentAndInitialize()
    {
        ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
        {
            if (updateError != null)
            {
                Debug.LogWarning("AdMob: izin bilgisi alınamadı: " + updateError.Message);
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null)
                {
                    Debug.LogWarning("AdMob: izin formu gösterilemedi: " + formError.Message);
                }

                if (ConsentInformation.CanRequestAds())
                {
                    InitializeAds();
                }
            });
        });
    }

    void InitializeAds()
    {
        // Reklam olayları Unity ana iş parçacığında gelsin (arayüz güvenle güncellenebilsin).
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(_ => LoadAd());
    }

    void LoadAd()
    {
        if (loading || ad != null)
            return;

        loading = true;

        RewardedAd.Load(AdMobConfig.ActiveRewardedAdUnitId, new AdRequest(), (loadedAd, error) =>
        {
            loading = false;

            if (error != null || loadedAd == null)
            {
                Debug.LogWarning("AdMob: reklam yüklenemedi: " + (error != null ? error.GetMessage() : "boş"));
                StartCoroutine(RetryLater());
                return;
            }

            ad = loadedAd;
            ad.OnAdFullScreenContentClosed += OnAdClosed;
            ad.OnAdFullScreenContentFailed += _ => OnAdClosed();
        });
    }

    IEnumerator RetryLater()
    {
        yield return new WaitForSecondsRealtime(RetryDelay);
        LoadAd();
    }

    public void Show(string rewardLabel, Action<bool> onFinished)
    {
        if (!IsReady)
        {
            onFinished?.Invoke(false);
            return;
        }

        this.onFinished = onFinished;
        rewardEarned = false;

        ad.Show(_ => rewardEarned = true);
    }

    void OnAdClosed()
    {
        Action<bool> callback = onFinished;
        onFinished = null;

        if (ad != null)
        {
            ad.Destroy();
            ad = null;
        }

        // Bir sonraki reklamı hazırla.
        LoadAd();

        callback?.Invoke(rewardEarned);
    }
}
#endif
