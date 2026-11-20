using System;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;

// Gerçek para satın alımları (Unity IAP 5).
// Oyun açılınca kendiliğinden oluşur ve sahneler arası yaşar,
// sahneye eklemen gerekmez.
//
// - Gold paketleri: tüketilebilir, kalıcı gold'a eklenir.
// - Premium skinler: tek seferlik, her açılışta mağazadan geri yüklenir.
//
// Editor'da Unity'nin sahte mağazası çalışır; gerçek para harcanmadan test edilebilir.
public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    public bool IsReady { get; private set; }

    // Ürünler ve fiyatlar mağazadan geldi
    public event Action OnProductsReady;

    // productId
    public event Action<string> OnPurchaseSucceeded;

    // productId, oyuncuya gösterilecek mesaj
    public event Action<string, string> OnPurchaseFailed;

    private StoreController store;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
            return;

        new GameObject("IAPManager").AddComponent<IAPManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ==================================
    // BAŞLATMA
    // ==================================

    async void Initialize()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("IAP: Unity Services başlatılamadı: " + exception.Message);
        }

        store = UnityIAPServices.StoreController();

        store.OnStoreConnected += HandleStoreConnected;
        store.OnStoreDisconnected += HandleStoreDisconnected;
        store.OnProductsFetched += HandleProductsFetched;
        store.OnProductsFetchFailed += HandleProductsFetchFailed;
        store.OnPurchasesFetched += HandlePurchasesFetched;
        store.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
        store.OnPurchasePending += HandlePurchasePending;
        store.OnPurchaseConfirmed += HandlePurchaseConfirmed;
        store.OnPurchaseFailed += HandlePurchaseFailed;
        store.OnPurchaseDeferred += HandlePurchaseDeferred;

        try
        {
            await store.Connect();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("IAP: Mağazaya bağlanılamadı: " + exception.Message);
        }
    }

    List<ProductDefinition> BuildProductDefinitions()
    {
        List<ProductDefinition> definitions = new List<ProductDefinition>();

        foreach (StoreCatalog.GoldPack pack in StoreCatalog.GoldPacks)
        {
            definitions.Add(new ProductDefinition(pack.productId, ProductType.Consumable));
        }

        // Sezon bileti: her sezon yeniden alınabilir
        definitions.Add(new ProductDefinition(SeasonPass.ProductId, ProductType.Consumable));

        CompanionSkinDatabase database = CompanionSkinDatabase.Load();

        if (database != null)
        {
            foreach (CompanionSkinDatabase.Skin skin in database.skins)
            {
                if (skin != null && skin.IsPremium)
                {
                    definitions.Add(new ProductDefinition(skin.iapProductId, ProductType.NonConsumable));
                }
            }
        }

        return definitions;
    }

    // ==================================
    // MAĞAZA OLAYLARI
    // ==================================

    void HandleStoreConnected()
    {
        GameLog.Log("IAP: Mağazaya bağlandı.");
        store.FetchProducts(BuildProductDefinitions());
    }

    void HandleStoreDisconnected(StoreConnectionFailureDescription description)
    {
        IsReady = false;
        Debug.LogWarning("IAP: Mağaza bağlantısı koptu: " + description.message);
    }

    void HandleProductsFetched(List<Product> products)
    {
        GameLog.Log("IAP: " + products.Count + " ürün alındı.");

        IsReady = true;
        OnProductsReady?.Invoke();

        // Daha önce alınmış premium skinleri geri yükle (telefon değişse bile).
        store.FetchPurchases();
    }

    void HandleProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning(
            "IAP: " + failure.FailedFetchProducts.Count +
            " ürün alınamadı: " + failure.FailureReason
        );

        // Bazı ürünler geldiyse yine de satış yapılabilir.
        if (store.GetProducts().Count > 0)
        {
            IsReady = true;
            OnProductsReady?.Invoke();
        }
    }

    void HandlePurchasesFetched(Orders orders)
    {
        // Sadece tek seferlik ürünler geri yüklenir.
        // Gold paketleri burada tekrar verilmez.
        foreach (ConfirmedOrder order in orders.ConfirmedOrders)
        {
            if (!PurchaseReceiptValidator.Validate(order.Info.Receipt, out _))
                continue;

            foreach (CartItem item in order.CartOrdered.Items())
            {
                Product product = item.Product;

                if (product != null &&
                    product.definition.type == ProductType.NonConsumable)
                {
                    GrantProduct(product.definition.id);
                }
            }
        }
    }

    void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription description)
    {
        Debug.LogWarning("IAP: Önceki satın alımlar alınamadı: " + description.message);
    }

    // Ödeme alındı: önce ödülü ver, sonra mağazaya onayla.
    void HandlePurchasePending(PendingOrder order)
    {
        // Sahte makbuzla gelen satın almalar ödül vermez.
        if (!PurchaseReceiptValidator.Validate(order.Info.Receipt, out string validationError))
        {
            Debug.LogWarning("IAP: Geçersiz makbuz, ödül verilmedi: " + validationError);

            OnPurchaseFailed?.Invoke(GetFirstProductId(order), "SATIN ALMA DOĞRULANAMADI");

            // Aynı geçersiz satın alma her açılışta tekrar gelmesin.
            store.ConfirmPurchase(order);
            return;
        }

        bool firstTime = PlayerProgress.TryMarkTransactionGranted(order.Info.TransactionID);

        foreach (CartItem item in order.CartOrdered.Items())
        {
            Product product = item.Product;

            if (product == null)
                continue;

            string productId = product.definition.id;

            // Onay yarıda kalıp aynı satın alma tekrar gelirse gold iki kez verilmez.
            if (firstTime || product.definition.type == ProductType.NonConsumable)
            {
                GrantProduct(productId);
            }

            OnPurchaseSucceeded?.Invoke(productId);
            GameAnalytics.PurchaseCompleted(productId);
        }

        store.ConfirmPurchase(order);
    }

    void HandlePurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failedOrder)
        {
            Debug.LogWarning(
                "IAP: Satın alma onaylanamadı: " +
                failedOrder.FailureReason + " " + failedOrder.Details
            );
        }
    }

    void HandlePurchaseFailed(FailedOrder order)
    {
        string productId = GetFirstProductId(order);

        GameLog.Log("IAP: Satın alma başarısız: " + productId + " " + order.FailureReason + " " + order.Details);

        OnPurchaseFailed?.Invoke(productId, GetFailureMessage(order.FailureReason));
    }

    void HandlePurchaseDeferred(DeferredOrder order)
    {
        // Örn. ebeveyn onayı veya nakit ödeme bekleniyor.
        // Ödeme tamamlanınca HandlePurchasePending çağrılır.
        OnPurchaseFailed?.Invoke(GetFirstProductId(order), "ÖDEME BEKLENİYOR");
    }

    // ==================================
    // ÖDÜL
    // ==================================

    void GrantProduct(string productId)
    {
        if (productId == SeasonPass.ProductId)
        {
            SeasonPass.UnlockPremium();
            GameLog.Log("IAP: Sezon bileti açıldı.");
            return;
        }

        GrantProductInternal(productId);
    }

    void GrantProductInternal(string productId)
    {
        if (StoreCatalog.TryGetGoldPack(productId, out StoreCatalog.GoldPack pack))
        {
            PlayerProgress.AddGold(pack.gold);
            GameLog.Log("IAP: +" + pack.gold + " gold verildi.");
            return;
        }

        CompanionSkinDatabase database = CompanionSkinDatabase.Load();

        CompanionSkinDatabase.Skin skin =
            database != null ? database.FindByProductId(productId) : null;

        if (skin != null)
        {
            PlayerProgress.UnlockCompanionSkin(skin.id);
            GameLog.Log("IAP: Skin açıldı: " + skin.id);
            return;
        }

        Debug.LogWarning("IAP: Tanımsız ürün satın alındı: " + productId);
    }

    // ==================================
    // DIŞARIDAN KULLANIM
    // ==================================

    public void Purchase(string productId)
    {
        if (!IsReady || store == null)
        {
            OnPurchaseFailed?.Invoke(productId, "MAĞAZA HAZIR DEĞİL");
            return;
        }

        Product product = store.GetProductById(productId);

        if (product == null || !product.availableToPurchase)
        {
            OnPurchaseFailed?.Invoke(productId, "ÜRÜN KULLANILAMIYOR");
            return;
        }

        store.PurchaseProduct(product);
    }

    // Mağazanın yerel para birimiyle fiyatı (örn. "₺49,99"). Hazır değilse boş.
    public string GetPriceString(string productId)
    {
        if (!IsReady || store == null)
            return string.Empty;

        Product product = store.GetProductById(productId);

        return product != null && product.metadata != null
            ? product.metadata.localizedPriceString
            : string.Empty;
    }

    // iOS'ta zorunlu "Restore Purchases" butonu için.
    // Android'de satın alımlar zaten her açılışta otomatik geri yüklenir.
    public void RestorePurchases(Action<bool> onComplete)
    {
        if (store == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        store.RestoreTransactions((success, error) =>
        {
            if (!success)
            {
                Debug.LogWarning("IAP: Geri yükleme başarısız: " + error);
            }

            store.FetchPurchases();
            onComplete?.Invoke(success);
        });
    }

    // ==================================
    // YARDIMCILAR
    // ==================================

    static string GetFirstProductId(Order order)
    {
        if (order?.CartOrdered == null)
            return string.Empty;

        foreach (CartItem item in order.CartOrdered.Items())
        {
            if (item?.Product != null)
                return item.Product.definition.id;
        }

        return string.Empty;
    }

    static string GetFailureMessage(PurchaseFailureReason reason)
    {
        switch (reason)
        {
            case PurchaseFailureReason.UserCancelled:
                return "SATIN ALMA İPTAL EDİLDİ";

            case PurchaseFailureReason.PaymentDeclined:
                return "ÖDEME REDDEDİLDİ";

            case PurchaseFailureReason.ExistingPurchasePending:
                return "BEKLEYEN BİR SATIN ALMA VAR";

            case PurchaseFailureReason.StoreNotConnected:
            case PurchaseFailureReason.PurchasingUnavailable:
                return "MAĞAZA KULLANILAMIYOR";

            default:
                return "SATIN ALMA BAŞARISIZ";
        }
    }
}
