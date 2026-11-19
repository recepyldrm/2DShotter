using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Google Play ekran görüntüleri için: Play modunda F12'ye bas.
// Görüntüler proje klasöründeki StoreListing/Screenshots içine kaydedilir.
// Game penceresini 1920x1080 (yatay) olarak ayarla; kısa kenarı 1080'den
// küçükse görüntü otomatik büyütülür (Google Play önerisi en az 1080 px).
public static class StoreScreenshotTool
{
    private const int MinShortSide = 1080;

    [MenuItem("Tools/Mağaza/Ekran Görüntüsü Al _F12")]
    static void Capture()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Ekran görüntüsü için önce Play moduna gir.");
            return;
        }

        string folder = Path.Combine(Directory.GetCurrentDirectory(), "StoreListing", "Screenshots");
        Directory.CreateDirectory(folder);

        int shortSide = Mathf.Max(1, Mathf.Min(Screen.width, Screen.height));
        int superSize = Mathf.Max(1, Mathf.CeilToInt((float)MinShortSide / shortSide));

        string path = Path.Combine(folder, "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");

        ScreenCapture.CaptureScreenshot(path, superSize);

        Debug.Log("Ekran görüntüsü kaydedildi: " + path + " (x" + superSize + ")");
    }

    [MenuItem("Tools/Mağaza/Ekran Görüntüsü Klasörünü Aç")]
    static void OpenFolder()
    {
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "StoreListing", "Screenshots");
        Directory.CreateDirectory(folder);
        EditorUtility.RevealInFinder(folder);
    }
}
