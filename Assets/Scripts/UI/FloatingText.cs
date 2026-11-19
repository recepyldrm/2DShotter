using UnityEngine;
using TMPro;

/// <summary>
/// Toplanan bir ödülü ("+10 Gold" gibi) ekranda yukarı doğru süzülüp
/// solarak kaybolan bir metin olarak gösterir. TextMeshPro (TMP_Text)
/// gerektirir; hem World Space TextMeshPro hem de bir Canvas içindeki
/// TextMeshProUGUI ile çalışır.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class FloatingText : MonoBehaviour
{
    [Header("Hareket ve Süre")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float randomXOffset = 0.4f;

    [Header("Animasyon Eğrileri")]
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0.4f),
        new Keyframe(0.15f, 1.15f),
        new Keyframe(0.3f, 1f),
        new Keyframe(1f, 1f)
    );

    [Header("2D Sıralama (Sorting)")]
    [Tooltip("2D projede popup'ın hangi Sorting Layer'da çizileceği. Sprite'ların önünde görünmesi için genelde en üstteki katman seçilir.")]
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Aynı Sorting Layer içindeki diğer nesnelere göre çizim sırası. Yüksek değer daha önde görünür.")]
    [SerializeField] private int sortingOrder = 100;

    /// <summary>
    /// Animasyon bittiğinde tetiklenir. Bu event'e abone olunursa nesne
    /// otomatik Destroy edilmez; yönetimi (örn. havuza geri koyma) çağırana bırakılır.
    /// Abone yoksa nesne kendini Destroy eder.
    /// </summary>
    public event System.Action<FloatingText> OnComplete;

    private TMP_Text _text;
    private float _elapsed;
    private Vector3 _startPos;
    private Vector3 _moveDir;
    private Color _baseColor;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();

        // 2D sahnede sprite'ların önünde çizilmesi için sorting ayarlarını uygula.
        // TextMeshProUGUI (Canvas içinde) kullanıyorsanız bu adım gerekmez;
        // sıralama Canvas/Component sırasına göre belirlenir.
        Renderer meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }

    /// <summary>
    /// Popup'ı verilen metin ve renkle sıfırlayıp animasyonu baştan başlatır.
    /// Instantiate/aktifleştirme sonrasında çağrılmalıdır.
    /// </summary>
    public void Setup(string text, Color color)
    {
        if (_text == null) _text = GetComponent<TMP_Text>();

        _text.text = text;
        _baseColor = color;
        _text.color = color;

        _startPos = transform.position;
        float xOffset = Random.Range(-randomXOffset, randomXOffset);
        _moveDir = new Vector3(xOffset, 1f, 0f).normalized;

        _elapsed = 0f;
        transform.localScale = Vector3.one * scaleCurve.Evaluate(0f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / lifetime);

        // Yukarı doğru (hafif rastgele yatay sapmalı) hareket
        transform.position = _startPos + _moveDir * (moveSpeed * _elapsed);

        // Küçük bir "pop" efekti için ölçek animasyonu
        transform.localScale = Vector3.one * scaleCurve.Evaluate(t);

        // Solma (fade out)
        Color c = _baseColor;
        c.a = alphaCurve.Evaluate(t);
        _text.color = c;

        if (_elapsed >= lifetime)
        {
            Complete();
        }
    }

    private void Complete()
    {
        if (OnComplete != null)
        {
            var callback = OnComplete;
            OnComplete = null;
            callback.Invoke(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}