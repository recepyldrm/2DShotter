using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Oyun sırasında bir görev tamamlanınca sol üstte kısa süre görünen bildirim:
// "GÖREV TAMAMLANDI · 100 düşman öldür". Ana menüde gösterilmez.
public class QuestToast : MonoBehaviour
{
    private const float ShowSeconds = 2.6f;

    private static QuestToast instance;

    private readonly Queue<string> queue = new Queue<string>();
    private bool showing;

    private UITheme theme;
    private CanvasGroup group;
    private TextMeshProUGUI text;

    public static void Show(QuestBoard board, QuestBoard.Quest quest)
    {
        // Editör testlerinde ve ana menüde gösterilmez.
        if (!Application.isPlaying ||
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameKeys.MainMenuScene)
            return;

        if (instance == null)
        {
            instance = new GameObject("QuestToast").AddComponent<QuestToast>();
        }

        string header = board.HasChest
            ? Loc.Pick("HAFTALIK GÖREV TAMAMLANDI", "WEEKLY QUEST COMPLETE")
            : Loc.Pick("GÖREV TAMAMLANDI", "QUEST COMPLETE");

        instance.queue.Enqueue(
            UIBuilder.ColorTag(instance.theme.mainRedHover, header) +
            "\n<size=26>" + quest.Title + "</size>"
        );

        if (!instance.showing)
        {
            instance.StartCoroutine(instance.Run());
        }
    }

    void Awake()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "QuestToastCanvas", 390);
        RectTransform safeRoot = UIBuilder.CreateSafeRoot(canvas);

        RectTransform panel = UIBuilder.CreateRect("Toast", safeRoot);
        UIBuilder.Place(panel, new Vector2(0f, 1f), new Vector2(40f, -150f), new Vector2(560f, 110f));

        Image background = panel.gameObject.AddComponent<Image>();
        theme.ApplyNotice(background);
        background.raycastTarget = false;

        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        text = UIBuilder.CreateText(theme, panel, string.Empty, 30f, TextAlignmentOptions.MidlineLeft, theme.mainWhite);
        UIBuilder.Stretch(text.rectTransform);
        text.margin = new Vector4(28f, 8f, 20f, 8f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 30f;

        panel.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    IEnumerator Run()
    {
        showing = true;

        while (queue.Count > 0)
        {
            text.text = queue.Dequeue();
            group.gameObject.SetActive(true);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayQuestComplete();

            // Soldan kayarak gelir, bekler, solar (oyun dursa da çalışır).
            RectTransform rect = (RectTransform)group.transform;
            float elapsed = 0f;

            while (elapsed < ShowSeconds)
            {
                elapsed += Time.unscaledDeltaTime;

                float slideIn = Mathf.Clamp01(elapsed / 0.25f);
                float fadeOut = Mathf.Clamp01((ShowSeconds - elapsed) / 0.35f);

                rect.anchoredPosition = new Vector2(Mathf.Lerp(-600f, 40f, 1f - Mathf.Pow(1f - slideIn, 3f)), -150f);
                group.alpha = fadeOut;
                yield return null;
            }

            group.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        showing = false;
    }
}
