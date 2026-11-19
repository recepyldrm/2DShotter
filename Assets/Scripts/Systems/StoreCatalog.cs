// Gerçek parayla satılan ürünler.
// Buradaki productId'ler Google Play Console'da (ve sonra App Store Connect'te)
// oluşturulan ürün ID'leriyle BİREBİR aynı olmalı.
// Premium skinlerin ID'leri CompanionSkinDatabase'deki "Iap Product Id" alanından gelir.
public static class StoreCatalog
{
    public readonly struct GoldPack
    {
        public readonly string productId;
        public readonly int gold;

        public GoldPack(string productId, int gold)
        {
            this.productId = productId;
            this.gold = gold;
        }
    }

    // Tüketilebilir (tekrar tekrar alınabilir) gold paketleri
    public static readonly GoldPack[] GoldPacks =
    {
        new GoldPack("gold_500", 500),
        new GoldPack("gold_1500", 1500),
        new GoldPack("gold_5000", 5000),
    };

    public static bool TryGetGoldPack(string productId, out GoldPack pack)
    {
        foreach (GoldPack candidate in GoldPacks)
        {
            if (candidate.productId == productId)
            {
                pack = candidate;
                return true;
            }
        }

        pack = default;
        return false;
    }
}
