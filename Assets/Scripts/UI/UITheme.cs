using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Oyunun arayüz teması (kırmızı-siyah gotik stil).
// Kodla oluşturulan ekranlar (reklam sayfası, Game Over reklam butonları)
// font, sprite ve renkleri buradan alır.
// Asset: Assets/Resources/UITheme
[CreateAssetMenu(fileName = "UITheme", menuName = "UI/Theme")]
public class UITheme : ScriptableObject
{
    public const string ResourcePath = "UITheme";

    [Header("Font")]
    public TMP_FontAsset font;

    [Header("Paneller")]
    [Tooltip("Kırmızı çerçeveli ana panel.")]
    public Sprite panelSprite;

    [Tooltip("Yatay bildirim şeridi.")]
    public Sprite noticeSprite;

    [Tooltip("Geniş pencerelerin dokulu zemini (esnetilir).")]
    public Sprite windowBackground;

    [Tooltip("Geniş pencerelerin kırmızı çerçevesi (9-slice, kenar payı tanımlı olmalı).")]
    public Sprite windowOutline;

    [Tooltip("Çerçeve çizgilerinin kalınlığı. Küçük değer = daha kalın.")]
    public float windowOutlinePixelsPerUnit = 0.4f;

    [Tooltip("Kapatma butonu ikonu.")]
    public Sprite closeIcon;

    [Header("Kırmızı buton")]
    public Sprite redButton;
    public Sprite redButtonHover;

    [Header("Gri buton")]
    public Sprite greyButton;
    public Sprite greyButtonHover;

    [Header("Ortak buton durumları")]
    public Sprite buttonPressed;
    public Sprite buttonDisabled;

    [Header("Renkler")]
    public Color mainRed = new Color32(0xA9, 0x0D, 0x01, 0xFF);
    public Color mainRedHover = new Color32(0xDA, 0x00, 0x00, 0xFF);
    public Color mainBlack = new Color32(0x11, 0x11, 0x11, 0xFF);
    public Color greyDark = new Color32(0x37, 0x37, 0x37, 0xFF);
    public Color greyLight = new Color32(0x66, 0x66, 0x66, 0xFF);
    public Color mainWhite = new Color32(0xD2, 0xD2, 0xD2, 0xFF);

    private static UITheme cached;

    // Asset yoksa yalnızca renkleri içeren varsayılan tema döner.
    public static UITheme Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<UITheme>(ResourcePath);

            if (cached == null)
            {
                Debug.LogWarning("UITheme bulunamadı! Resources klasöründe '" + ResourcePath + "' olmalı.");
                cached = CreateInstance<UITheme>();
            }
        }

        return cached;
    }

    // ==================================
    // UYGULAMA YARDIMCILARI
    // ==================================

    public void ApplyText(TextMeshProUGUI text, Color color)
    {
        if (font != null)
        {
            text.font = font;
        }

        text.color = color;
    }

    public void ApplyPanel(Image image)
    {
        ApplySprite(image, panelSprite, mainBlack, Image.Type.Simple);
    }

    public void ApplyNotice(Image image)
    {
        ApplySprite(image, noticeSprite, mainBlack, Image.Type.Simple);
    }

    // Her boyuta uyan pencere: dokulu zemin + üstünde 9-slice kırmızı çerçeve.
    public void ApplyWindow(Image background)
    {
        ApplySprite(background, windowBackground, mainBlack, Image.Type.Simple);

        if (windowOutline == null)
            return;

        GameObject outlineObject = new GameObject("Outline", typeof(RectTransform));
        outlineObject.transform.SetParent(background.transform, false);

        RectTransform rect = (RectTransform)outlineObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image outline = outlineObject.AddComponent<Image>();
        outline.sprite = windowOutline;
        outline.type = windowOutline.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        outline.pixelsPerUnitMultiplier = windowOutlinePixelsPerUnit;
        outline.fillCenter = false;
        outline.raycastTarget = false;
    }

    // Kapatma (X) butonu. İkon yoksa gri buton + "X" yazısı.
    public Button CreateCloseButton(Transform parent)
    {
        GameObject go = new GameObject("CloseButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        Button button = go.AddComponent<Button>();

        if (closeIcon != null)
        {
            image.sprite = closeIcon;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.highlightedColor = mainRedHover;
            colors.pressedColor = mainRed;
            colors.selectedColor = Color.white;
            button.colors = colors;
        }
        else
        {
            ApplyButton(button, image, false);

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(go.transform, false);

            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            ApplyText(text, mainWhite);
            text.text = "X";
            text.fontSize = 40f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        return button;
    }

    // Sprite'lar varsa üzerine gelince / basınca / pasifken görünüm değişir,
    // yoksa düz renkli buton olur.
    public void ApplyButton(Button button, Image image, bool red)
    {
        Sprite normal = red ? redButton : greyButton;
        Sprite hover = red ? redButtonHover : greyButtonHover;

        button.targetGraphic = image;

        if (normal == null)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = red ? mainRed : greyDark;
            button.transition = Selectable.Transition.ColorTint;
            return;
        }

        image.sprite = normal;
        image.type = normal.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;

        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = hover,
            selectedSprite = normal,
            pressedSprite = buttonPressed != null ? buttonPressed : hover,
            disabledSprite = buttonDisabled != null ? buttonDisabled : normal
        };
    }

    static void ApplySprite(Image image, Sprite sprite, Color fallbackColor, Image.Type type)
    {
        if (sprite == null)
        {
            image.sprite = null;
            image.color = fallbackColor;
            return;
        }

        image.sprite = sprite;
        image.type = type;
        image.color = Color.white;
    }
}
