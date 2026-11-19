using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Oyun başında, ana menüden alınmış kalıcı geliştirmeleri birkaç saniye gösterir
// (örn. "KALICI BONUSLAR: +20 maksimum can · +%10 hasar"). Bonus yoksa görünmez.
public class ActiveBonusBanner : MonoBehaviour
{
    private const float VisibleSeconds = 3.5f;
    private const float FadeSeconds = 0.6f;

    public static void ShowIfAny()
    {
        List<string> bonuses = PermanentUpgrades.GetActiveBonusTexts();

        if (bonuses.Count == 0)
            return;

        new GameObject("ActiveBonusBanner").AddComponent<ActiveBonusBanner>().Show(bonuses);
    }

    void Show(List<string> bonuses)
    {
        UITheme theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "ActiveBonusCanvas", 350);
        RectTransform safeRoot = UIBuilder.CreateSafeRoot(canvas);

        RectTransform banner = UIBuilder.CreateRect("Banner", safeRoot);
        UIBuilder.Place(banner, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1300f, 120f));

        Image background = banner.gameObject.AddComponent<Image>();
        theme.ApplyNotice(background);
        background.raycastTarget = false;

        TextMeshProUGUI text = UIBuilder.CreateText(
            theme,
            banner,
            UIBuilder.ColorTag(theme.mainRedHover, "KALICI BONUSLAR") + "\n<size=28>" +
            string.Join("   ·   ", bonuses) + "</size>",
            34f,
            TextAlignmentOptions.Center,
            theme.mainWhite
        );
        UIBuilder.Stretch(text.rectTransform);
        text.margin = new Vector4(50f, 8f, 50f, 8f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 34f;

        CanvasGroup group = banner.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        StartCoroutine(HideAfterDelay(group));
    }

    IEnumerator HideAfterDelay(CanvasGroup group)
    {
        yield return new WaitForSecondsRealtime(VisibleSeconds);

        float elapsed = 0f;

        while (elapsed < FadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - elapsed / FadeSeconds;
            yield return null;
        }

        Destroy(gameObject);
    }
}
