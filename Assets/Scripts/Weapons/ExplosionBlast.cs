using System.Collections.Generic;
using UnityEngine;

// Patlamanın görünen kısmı: ortada hızla büyüyüp sönen bir parlama ve
// dışa doğru yayılan bir şok dalgası halkası. Halka tam olarak hasar
// yarıçapına kadar büyür, böylece oyuncu patlamanın nereye ulaştığını görür.
// İlk kullanımda kendiliğinden oluşur; havuz kullanır.
public class ExplosionBlast : MonoBehaviour
{
    private const float Lifetime = 0.35f;
    private const int RingSegments = 48;
    private const int PoolSize = 12;
    private const int SortingOrder = 60;

    private static ExplosionBlast manager;
    private static Sprite circleSprite;

    private class Blast
    {
        public GameObject root;
        public SpriteRenderer flash;
        public LineRenderer ring;
        public float radius;
        public float age;
        public Color coreColor;
        public Color edgeColor;
    }

    private readonly List<Blast> pool = new List<Blast>();
    private readonly List<Blast> active = new List<Blast>();
    private Material material;

    // Patlayıcı mermi: sarı-turuncu
    public static void Play(Vector3 position, float radius)
    {
        Play(position, radius, new Color(1f, 0.85f, 0.4f, 1f), new Color(1f, 0.45f, 0.05f, 1f));
    }

    public static void Play(Vector3 position, float radius, Color coreColor, Color edgeColor)
    {
        // Düşük grafik ayarında küçük süs patlamaları (isabet, drone atışı) atlanır.
        if (radius <= 0f || (radius < 0.6f && GraphicsQuality.ReducedEffects))
            return;

        if (manager == null)
        {
            manager = new GameObject("ExplosionBlasts").AddComponent<ExplosionBlast>();
        }

        manager.Spawn(position, radius, coreColor, edgeColor);
    }

    void Awake()
    {
        UITheme theme = UITheme.Load();
        material = theme.unlitEffectMaterial;

        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite();
        }

        for (int i = 0; i < PoolSize; i++)
        {
            pool.Add(CreateBlast());
        }
    }

    void OnDestroy()
    {
        if (manager == this)
        {
            manager = null;
        }
    }

    void Spawn(Vector3 position, float radius, Color coreColor, Color edgeColor)
    {
        Blast blast;

        if (pool.Count > 0)
        {
            blast = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        }
        else
        {
            // Çok sayıda patlama aynı anda: en eskisini yeniden kullan.
            blast = active[0];
            active.RemoveAt(0);
        }

        position.z = 0f;
        blast.root.transform.position = position;
        blast.radius = radius;
        blast.age = 0f;
        blast.coreColor = coreColor;
        blast.edgeColor = edgeColor;
        blast.root.SetActive(true);

        UpdateBlast(blast, 0f);
        active.Add(blast);
    }

    void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Blast blast = active[i];
            blast.age += Time.deltaTime;

            float t = blast.age / Lifetime;

            if (t >= 1f)
            {
                blast.root.SetActive(false);
                active.RemoveAt(i);
                pool.Add(blast);
                continue;
            }

            UpdateBlast(blast, t);
        }
    }

    void UpdateBlast(Blast blast, float t)
    {
        // Hızlı başlayıp yavaşlayan büyüme
        float grow = 1f - (1f - t) * (1f - t) * (1f - t);

        // Parlama: yarıçapın biraz altında kalır, hızla söner
        float flashSize = blast.radius * 1.6f * Mathf.Lerp(0.35f, 1f, grow);
        blast.flash.transform.localScale = new Vector3(flashSize, flashSize, 1f);

        Color flashColor = Color.Lerp(Color.white, blast.coreColor, t * 2f);
        flashColor.a = Mathf.Clamp01(1f - t * 1.6f) * 0.85f;
        blast.flash.color = flashColor;

        // Şok dalgası: hasar yarıçapına kadar büyür, incelerek söner
        float ringRadius = blast.radius * grow;

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i / (float)RingSegments * Mathf.PI * 2f;
            blast.ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0f));
        }

        float width = Mathf.Lerp(0.22f, 0.02f, t) * Mathf.Clamp(blast.radius / 1.5f, 0.6f, 1.6f);
        blast.ring.startWidth = width;
        blast.ring.endWidth = width;

        Color ringColor = Color.Lerp(blast.coreColor, blast.edgeColor, t);
        ringColor.a = 1f - t;
        blast.ring.startColor = ringColor;
        blast.ring.endColor = ringColor;
    }

    Blast CreateBlast()
    {
        GameObject root = new GameObject("Blast");
        root.transform.SetParent(transform, false);

        GameObject flashObject = new GameObject("Flash");
        flashObject.transform.SetParent(root.transform, false);

        SpriteRenderer flash = flashObject.AddComponent<SpriteRenderer>();
        flash.sprite = circleSprite;
        flash.sortingOrder = SortingOrder - 1;

        if (material != null)
        {
            flash.sharedMaterial = material;
        }

        GameObject ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(root.transform, false);

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = RingSegments;
        ring.numCapVertices = 2;
        ring.sortingOrder = SortingOrder + 1;

        if (material != null)
        {
            ring.sharedMaterial = material;
        }

        root.SetActive(false);

        return new Blast
        {
            root = root,
            flash = flash,
            ring = ring
        };
    }

    // Ortası dolu, kenarları yumuşakça sönen 1 birim çaplı daire
    static Sprite CreateCircleSprite()
    {
        const int size = 128;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
