using System;

// Ödüllü reklamı gösteren taraf. Gerçek reklam ağı (LevelPlay, AdMob...)
// eklendiğinde bu arayüzü uygulayan yeni bir sınıf yazılır ve
// AdsManager.Provider'da FakeRewardedAd yerine o kullanılır.
public interface IRewardedAdProvider
{
    bool IsReady { get; }

    // rewardLabel: oyuncuya gösterilecek ödül adı (örn. "CONTINUE").
    // Gerçek reklam ağları bunu kullanmayabilir.
    // onFinished(true) = reklam sonuna kadar izlendi, ödül verilmeli.
    void Show(string rewardLabel, Action<bool> onFinished);
}

// Oyunun geri kalanı reklamlara sadece buradan erişir.
public static class AdsManager
{
    private static IRewardedAdProvider provider;

    public static IRewardedAdProvider Provider
    {
        get
        {
            // Unity objesi yok edildiyse (örn. Play mode yeniden başladı) yeniden oluştur.
            if (provider == null ||
                (provider is UnityEngine.Object unityObject && unityObject == null))
            {
                provider = FakeRewardedAd.Create();
            }

            return provider;
        }
    }

    public static bool IsShowing { get; private set; }

    public static bool IsRewardedReady =>
        !IsShowing && Provider.IsReady;

    public static void ShowRewarded(string rewardLabel, Action<bool> onFinished)
    {
        if (!IsRewardedReady)
        {
            onFinished?.Invoke(false);
            return;
        }

        IsShowing = true;

        Provider.Show(rewardLabel, rewarded =>
        {
            IsShowing = false;
            onFinished?.Invoke(rewarded);
        });
    }
}
