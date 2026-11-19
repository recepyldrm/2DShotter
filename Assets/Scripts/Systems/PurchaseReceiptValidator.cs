using System;
using UnityEngine;
using UnityEngine.Purchasing.Security;

// Google Play satın alma makbuzlarını cihazda doğrular (sahte satın almalara karşı).
//
// Gereken Google lisans anahtarı, Unity'nin ürettiği GooglePlayTangle sınıfındadır:
// Services > In-App Purchasing > Receipt Validation Obfuscator ekranına Play Console'daki
// lisans anahtarını yapıştırıp "Obfuscate" de. Sınıf yansıma (reflection) ile bulunduğu için
// bu dosya o adımdan önce de derlenir; anahtar yokken doğrulama atlanır ve uyarı verilir.
public static class PurchaseReceiptValidator
{
    private const string TangleTypeName = "UnityEngine.Purchasing.Security.GooglePlayTangle";

    // Sadece Android build'inde atanır.
#pragma warning disable 0649
    private static CrossPlatformValidator validator;
#pragma warning restore 0649
    private static bool initialized;
    private static bool warnedMissingKey;

    public static bool IsConfigured
    {
        get
        {
            Initialize();
            return validator != null;
        }
    }

    // Makbuz geçerliyse (ya da doğrulama yapılamıyorsa) true döner.
    public static bool Validate(string receipt, out string error)
    {
        error = null;
        Initialize();

        if (validator == null)
        {
            if (!warnedMissingKey)
            {
                warnedMissingKey = true;
                Debug.LogWarning(
                    "IAP: Makbuz doğrulama kapalı. Receipt Validation Obfuscator ile Google lisans anahtarını ekle."
                );
            }

            return true;
        }

        try
        {
            validator.Validate(receipt);
            return true;
        }
        catch (IAPSecurityException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    static void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        // Doğrulama sadece gerçek Android cihazda anlamlıdır; Editor'da sahte mağaza kullanılır.
#if UNITY_ANDROID && !UNITY_EDITOR
        byte[] googleKey = LoadGooglePlayKey();

        if (googleKey == null)
            return;

        try
        {
            validator = new CrossPlatformValidator(googleKey, Application.identifier);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("IAP: Makbuz doğrulayıcı başlatılamadı: " + exception.Message);
        }
#endif
    }

    static byte[] LoadGooglePlayKey()
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type tangle = assembly.GetType(TangleTypeName);

            if (tangle == null)
                continue;

            System.Reflection.MethodInfo data = tangle.GetMethod("Data");
            return data != null ? data.Invoke(null, null) as byte[] : null;
        }

        return null;
    }
}
