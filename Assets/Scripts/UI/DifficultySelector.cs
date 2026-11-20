using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ana menüde OYNA / AYARLAR / ÇIKIŞ butonlarının altında zorluk seçici.
// Basınca KOLAY > NORMAL > ZOR arasında döner; seçim bir sonraki oyunda geçerli olur.
public class DifficultySelector : MonoBehaviour
{
    private UITheme theme;
    private TextMeshProUGUI buttonText;
    private TextMeshProUGUI descriptionText;

    void Start()
    {
        theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "DifficultyCanvas", 140);
        RectTransform root = UIBuilder.CreateSafeRoot(canvas);

        Button button = UIBuilder.CreateButton(theme, root, "DifficultyButton", string.Empty, false, out buttonText);
        UIBuilder.Center((RectTransform)button.transform, new Vector2(0f, -300f), new Vector2(420f, 85f));
        button.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Difficulty.Current = (Difficulty.Level)(((int)Difficulty.Current + 1) % 3);
            Refresh();
        });

        descriptionText = UIBuilder.CreateText(theme, root, string.Empty, 26f, TextAlignmentOptions.Center, theme.greyLight);
        UIBuilder.Center(descriptionText.rectTransform, new Vector2(0f, -370f), new Vector2(700f, 40f));

        Refresh();
    }

    void Refresh()
    {
        Difficulty.Level level = Difficulty.Current;

        Color color = level == Difficulty.Level.Hard
            ? theme.mainRedHover
            : level == Difficulty.Level.Easy ? new Color(0.55f, 0.9f, 0.55f, 1f) : theme.mainWhite;

        buttonText.text = Loc.T("ZORLUK") + ": " + UIBuilder.ColorTag(color, Difficulty.Label(level));
        descriptionText.text = Difficulty.Description(level);
    }
}
