using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Düşmanların üstünde uçuşan hasar sayıları.
// Kritik vuruşlar büyük ve kırmızı, yanma turuncu, zincir yıldırım mavi.
// İlk kullanımda kendiliğinden oluşur; havuz kullanır.
public class DamageNumbers : MonoBehaviour
{
    public enum Style
    {
        Normal,
        Critical,
        Burn,
        Chain,
        Explosion
    }

    private const int PoolSize = 60;
    private const float Lifetime = 0.7f;
    private const float RiseDistance = 0.9f;

    private class Number
    {
        public TextMeshPro text;
        public Vector3 start;
        public Vector3 drift;
        public float age;
        public float baseScale;
        public Color color;
        public bool critical;
    }

    private static DamageNumbers instance;

    private readonly List<Number> pool = new List<Number>();
    private readonly List<Number> active = new List<Number>();
    private UITheme theme;

    public static void Show(Vector3 worldPosition, int amount, Style style)
    {
        if (amount <= 0)
            return;

        if (instance == null)
        {
            GameObject go = new GameObject("DamageNumbers");
            instance = go.AddComponent<DamageNumbers>();
        }

        instance.Spawn(worldPosition, amount, style);
    }

    void Awake()
    {
        theme = UITheme.Load();

        for (int i = 0; i < PoolSize; i++)
        {
            pool.Add(CreateNumber());
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    Number CreateNumber()
    {
        GameObject go = new GameObject("DamageNumber");
        go.transform.SetParent(transform, false);

        TextMeshPro text = go.AddComponent<TextMeshPro>();

        if (theme.font != null)
        {
            text.font = theme.font;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        text.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

        // Sprite'ların önünde çizilsin.
        MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
        meshRenderer.sortingOrder = 500;

        go.SetActive(false);

        return new Number { text = text };
    }

    void Spawn(Vector3 position, int amount, Style style)
    {
        Number number;

        if (pool.Count > 0)
        {
            number = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        }
        else
        {
            // Havuz bittiyse en eski sayıyı yeniden kullan.
            number = active[0];
            active.RemoveAt(0);
        }

        number.critical = style == Style.Critical;
        number.color = GetColor(style);
        number.baseScale = GetSize(style);
        number.age = 0f;
        number.start = position + new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
        number.drift = new Vector3(Random.Range(-0.25f, 0.25f), RiseDistance, 0f);

        number.text.text = number.critical ? amount + "!" : amount.ToString();
        number.text.color = number.color;
        number.text.fontSize = number.baseScale;
        number.text.transform.position = number.start;
        number.text.transform.localScale = Vector3.one;
        number.text.gameObject.SetActive(true);

        active.Add(number);
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Number number = active[i];
            number.age += deltaTime;

            float t = number.age / Lifetime;

            if (t >= 1f)
            {
                number.text.gameObject.SetActive(false);
                active.RemoveAt(i);
                pool.Add(number);
                continue;
            }

            // Hızlı yüksel, sonra yavaşla
            float rise = 1f - (1f - t) * (1f - t);
            number.text.transform.position = number.start + number.drift * rise;

            // Kritik vuruşta "pop" efekti
            float scale = number.critical && t < 0.15f
                ? Mathf.Lerp(1.7f, 1f, t / 0.15f)
                : 1f;
            number.text.transform.localScale = Vector3.one * scale;

            // Son %40'ta sol
            Color color = number.color;
            color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            number.text.color = color;
        }
    }

    Color GetColor(Style style)
    {
        switch (style)
        {
            case Style.Critical:
                return theme.mainRedHover;

            case Style.Burn:
                return new Color(1f, 0.55f, 0.1f, 1f);

            case Style.Chain:
                return new Color(0.45f, 0.85f, 1f, 1f);

            case Style.Explosion:
                return new Color(1f, 0.8f, 0.3f, 1f);

            default:
                return theme.mainWhite;
        }
    }

    static float GetSize(Style style)
    {
        switch (style)
        {
            case Style.Critical:
                return 7f;

            case Style.Burn:
            case Style.Chain:
                return 3.5f;

            default:
                return 4.5f;
        }
    }
}
