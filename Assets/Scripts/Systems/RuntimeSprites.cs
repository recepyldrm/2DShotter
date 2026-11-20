using UnityEngine;

// Kodla üretilen basit efekt sprite'ları (ek görsel dosyası gerekmez).
public static class RuntimeSprites
{
    private static Sprite softCircle;
    private static Sprite solidCircle;
    private static Sprite shieldArc;

    // Merkezden kenara yumuşakça solan daire (hale / parıltı)
    public static Sprite SoftCircle =>
        softCircle != null ? softCircle : softCircle = Create(64, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - d);
            return a * a;
        });

    // Kenarı hafif yumuşatılmış dolu daire (mermi)
    public static Sprite SolidCircle =>
        solidCircle != null ? solidCircle : solidCircle = Create(32, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01((1f - d) * 8f);
        });

    // Dikey, kenarları parlak kalkan yayı
    public static Sprite ShieldArc =>
        shieldArc != null ? shieldArc : shieldArc = Create(64, (x, y) =>
        {
            // Elips: içi yarı saydam, kenarı belirgin
            float d = Mathf.Sqrt(x * x + y * y);

            if (d > 1f)
                return 0f;

            float edge = Mathf.Clamp01((d - 0.7f) / 0.3f);
            float fade = Mathf.Clamp01((1f - d) * 10f);
            return Mathf.Lerp(0.35f, 1f, edge) * fade;
        });

    // alpha(x, y): x ve y -1..1 aralığında
    static Sprite Create(int size, System.Func<float, float, float> alpha)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "RuntimeSprite"
        };

        Color32[] pixels = new Color32[size * size];

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2f - 1f;
                float y = (py + 0.5f) / size * 2f - 1f;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f);
                pixels[py * size + px] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        // pixelsPerUnit = size: sprite 1x1 dünya birimi
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
