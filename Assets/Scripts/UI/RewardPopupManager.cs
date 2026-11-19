using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FloatingText popup'larını spawn eden merkezi, sahnede tekil (singleton)
/// yönetici. Basit bir nesne havuzu (pooling) kullanarak sık ödül
/// toplamalarında Instantiate/Destroy maliyetini azaltır.
/// </summary>
public class RewardPopupManager : MonoBehaviour
{
    public static RewardPopupManager Instance { get; private set; }

    [Header("Prefab")]
    [Tooltip("TMP_Text + FloatingText içeren popup prefab'ı.")]
    [SerializeField] private FloatingText floatingTextPrefab;
    [SerializeField] private Transform poolParent;

    [Header("Havuz Ayarları")]
    [SerializeField] private int initialPoolSize = 10;
    private readonly Queue<FloatingText> _pool = new Queue<FloatingText>();

    [Header("Varsayılan Renkler")]
    [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0.1f);
    [SerializeField] private Color expColor = new Color(0.4f, 0.75f, 1f);
    [SerializeField] private Color itemColor = Color.white;

    [Header("Ekran Sınırı")]
    [Tooltip("Popup'ın kamera görüş alanının kenarından ne kadar içeride kalacağı (0-0.5 arası, viewport oranı).")]
    [SerializeField, Range(0f, 0.4f)] private float screenEdgeMargin = 0.08f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (poolParent == null) poolParent = transform;

        for (int i = 0; i < initialPoolSize; i++)
        {
            _pool.Enqueue(CreateInstance());
        }
    }

    private FloatingText CreateInstance()
    {
        FloatingText instance = Instantiate(floatingTextPrefab, poolParent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    /// <summary>
    /// Genel amaçlı popup gösterme metodu.
    /// worldPosition: ödülün toplandığı dünya konumu (örn. item.transform.position).
    /// Pozisyon, ekran dışında kalmayacak şekilde otomatik olarak kamera
    /// görüş alanına sıkıştırılır (clamp).
    /// </summary>
    public void ShowReward(string text, Vector3 worldPosition, Color color)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("[RewardPopupManager] floatingTextPrefab atanmadı.");
            return;
        }

        FloatingText popup = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();

        Vector3 safePosition = ClampToScreen(worldPosition);

        popup.transform.position = safePosition;
        popup.transform.rotation = Quaternion.identity;
        popup.gameObject.SetActive(true);

        popup.OnComplete += ReturnToPool;
        popup.Setup(text, color);
    }

    /// <summary>
    /// Verilen world pozisyonunu, aktif kameranın görüş alanının
    /// (screenEdgeMargin kadar içeride kalacak şekilde) dışına taşmayacak
    /// bir noktaya çevirir. Popup her zaman ekranda görünür kalır.
    /// </summary>
    private Vector3 ClampToScreen(Vector3 worldPosition)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return worldPosition;

        Vector3 viewportPos = cam.WorldToViewportPoint(worldPosition);

        // Kamera arkasında kalan bir pozisyon gelirse (z < 0), olduğu gibi bırak.
        if (viewportPos.z < 0f)
            return worldPosition;

        viewportPos.x = Mathf.Clamp(viewportPos.x, screenEdgeMargin, 1f - screenEdgeMargin);
        viewportPos.y = Mathf.Clamp(viewportPos.y, screenEdgeMargin, 1f - screenEdgeMargin);

        return cam.ViewportToWorldPoint(viewportPos);
    }

    private void ReturnToPool(FloatingText popup)
    {
        popup.gameObject.SetActive(false);
        _pool.Enqueue(popup);
    }

    // --- Hazır kısayollar ---

    public void ShowGold(int amount, Vector3 worldPosition)
        => ShowReward($"+{amount} Altın", worldPosition, goldColor);

    public void ShowExp(int amount, Vector3 worldPosition)
        => ShowReward($"+{amount} XP", worldPosition, expColor);

    public void ShowItem(string itemName, int amount, Vector3 worldPosition)
        => ShowReward(amount > 1 ? $"+{amount} {itemName}" : $"+{itemName}", worldPosition, itemColor);
}