using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

// Google Play Games ve Google Mobile Ads (AdMob) eklentileri projeye kurulunca
// onlara bağlı kodları (GooglePlayGamesService, AdMobRewardedAd) açan
// tanımları (GPGS_INSTALLED, ADMOB_INSTALLED) otomatik ekler; eklenti silinirse kaldırır.
[InitializeOnLoad]
static class PluginDefines
{
    private static readonly (string define, string typeName)[] Plugins =
    {
        ("GPGS_INSTALLED", "GooglePlayGames.PlayGamesPlatform"),
        ("ADMOB_INSTALLED", "GoogleMobileAds.Api.MobileAds"),
        ("NOTIFICATIONS_INSTALLED", "Unity.Notifications.Android.AndroidNotificationCenter"),
        ("FIREBASE_CRASHLYTICS", "Firebase.Crashlytics.Crashlytics"),
    };

    private static readonly NamedBuildTarget[] Targets =
    {
        NamedBuildTarget.Android,
        NamedBuildTarget.iOS,
        NamedBuildTarget.Standalone,
    };

    static PluginDefines()
    {
        EditorApplication.delayCall += Refresh;
    }

    static void Refresh()
    {
        foreach (NamedBuildTarget target in Targets)
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(target);
            HashSet<string> defines = new HashSet<string>(
                current.Split(';').Where(d => !string.IsNullOrWhiteSpace(d))
            );

            bool changed = false;

            foreach ((string define, string typeName) in Plugins)
            {
                bool installed = TypeExists(typeName);

                if (installed && defines.Add(define))
                {
                    changed = true;
                }
                else if (!installed && defines.Remove(define))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            }
        }
    }

    static bool TypeExists(string fullName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(assembly => assembly.GetType(fullName, false) != null);
    }
}
