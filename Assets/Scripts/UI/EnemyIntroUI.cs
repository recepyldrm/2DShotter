using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Yeni bir düşman türü ya da boss tipi ilk kez ekrana girdiğinde oyunu durdurup
// kısa bir tanıtım gösterir. Her tanıtım bir kez gösterilir (PlayerPrefs).
// Kendiliğinden oluşur, sahneye eklemek gerekmez.
public class EnemyIntroUI : MonoBehaviour
{
    private class Request
    {
        public string id;
        public string title;
        public string body;
        public Color color;
        public Transform target;
        public bool isBoss;
    }

    private static EnemyIntroUI instance;

    private readonly Queue<Request> queue = new Queue<Request>();
    private readonly HashSet<string> queuedIds = new HashSet<string>();
    private bool processing;

    private UITheme theme;
    private GameObject panel;
    private Image colorStripe;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private bool waitingForClose;

    public static bool IsShowing => instance != null && instance.panel != null && instance.panel.activeSelf;

    static string Key(string id) => GameKeys.IntroSeenPrefix + id;

    public static bool HasSeen(string id) => PlayerPrefs.GetInt(Key(id), 0) == 1;

    // target ekrana girince gösterilir; target null ise hemen.
    public static void ShowOnce(string id, string title, string body, Color color, Transform target = null, bool isBoss = false)
    {
        if (HasSeen(id))
            return;

        if (instance == null)
        {
            instance = new GameObject("EnemyIntroUI").AddComponent<EnemyIntroUI>();
        }

        if (!instance.queuedIds.Add(id))
            return;

        instance.queue.Enqueue(new Request { id = id, title = title, body = body, color = color, target = target, isBoss = isBoss });

        if (!instance.processing)
        {
            instance.StartCoroutine(instance.Process());
        }
    }

    // Test menüsü için
    public static void ResetAll(IEnumerable<string> ids)
    {
        foreach (string id in ids)
        {
            PlayerPrefs.DeleteKey(Key(id));
        }

        PlayerPrefs.Save();
    }

    void Awake()
    {
        theme = UITheme.Load();
        Build();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        TimeScaleController.Resume(this);
    }

    IEnumerator Process()
    {
        processing = true;

        while (queue.Count > 0)
        {
            Request request = queue.Peek();

            // Hedef ekrana girene kadar bekle (yok olduysa sonraki sefere kalsın).
            bool targetLost = false;

            while (request.target != null && !IsOnScreen(request.target))
            {
                yield return null;

                if (request.target == null || !request.target.gameObject.activeInHierarchy)
                {
                    targetLost = true;
                    break;
                }
            }

            if (targetLost)
            {
                queue.Dequeue();
                queuedIds.Remove(request.id);
                continue;
            }

            // Kart ekranı, duraklatma, eğitim vb. açıkken bekle.
            while (TimeScaleController.IsPaused || TutorialUI.IsShowing)
                yield return null;

            queue.Dequeue();

            if (HasSeen(request.id))
                continue;

            PlayerPrefs.SetInt(Key(request.id), 1);
            PlayerPrefs.Save();

            yield return ShowRoutine(request);
        }

        processing = false;
    }

    static bool IsOnScreen(Transform target)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return true;

        Vector3 viewport = cam.WorldToViewportPoint(target.position);
        return viewport.x > 0.05f && viewport.x < 0.9f && viewport.y > 0f && viewport.y < 1f;
    }

    IEnumerator ShowRoutine(Request request)
    {
        colorStripe.color = request.color;
        headerText.text = Loc.T(request.isBoss ? "YENİ BOSS" : "YENİ DÜŞMAN");
        titleText.text = request.title;
        titleText.color = request.color;
        bodyText.text = request.body;

        TimeScaleController.Pause(this);
        panel.SetActive(true);
        waitingForClose = true;

        // Yanlışlıkla hemen kapanmasın.
        float elapsed = 0f;
        while (waitingForClose || elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        panel.SetActive(false);
        TimeScaleController.Resume(this);
    }

    void Build()
    {
        Transform canvas = UIBuilder.CreateCanvas(transform, "EnemyIntroCanvas", 450);
        RectTransform dim = UIBuilder.CreateDim(canvas, "EnemyIntroPanel", 0.7f);
        panel = dim.gameObject;

        RectTransform window = UIBuilder.CreateWindow(
            theme,
            UIBuilder.CreateSafeRoot(dim),
            new Vector2(0.25f, 0.25f),
            new Vector2(0.75f, 0.75f)
        );

        // Türün rengini gösteren şerit
        RectTransform stripe = UIBuilder.CreateRect("Stripe", window);
        stripe.anchorMin = new Vector2(0f, 1f);
        stripe.anchorMax = new Vector2(1f, 1f);
        stripe.pivot = new Vector2(0.5f, 1f);
        stripe.offsetMin = new Vector2(50f, -40f);
        stripe.offsetMax = new Vector2(-50f, -28f);
        colorStripe = stripe.gameObject.AddComponent<Image>();
        colorStripe.raycastTarget = false;

        headerText = UIBuilder.CreateText(theme, window, string.Empty, 30f, TextAlignmentOptions.Center, theme.greyLight);
        UIBuilder.AnchorTop(headerText.rectTransform, 55f, 45f);

        titleText = UIBuilder.CreateText(theme, window, string.Empty, 64f, TextAlignmentOptions.Center, theme.mainWhite);
        UIBuilder.AnchorTop(titleText.rectTransform, 100f, 80f);

        bodyText = UIBuilder.CreateText(theme, window, string.Empty, 34f, TextAlignmentOptions.Top, theme.mainWhite);
        bodyText.rectTransform.anchorMin = new Vector2(0f, 0f);
        bodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
        bodyText.rectTransform.offsetMin = new Vector2(70f, 150f);
        bodyText.rectTransform.offsetMax = new Vector2(-70f, -200f);
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 22f;
        bodyText.fontSizeMax = 34f;

        Button ok = UIBuilder.CreateButton(theme, window, "OkButton", Loc.T("ANLADIM"), true, out _);
        UIBuilder.Place((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(320f, 95f));
        ok.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            waitingForClose = false;
        });

        panel.SetActive(false);
    }
}
