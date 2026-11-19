using System.Collections.Generic;
using UnityEngine;

// Bütün dost skinlerinin listesi.
// Asset Resources klasöründe "CompanionSkinDatabase" adıyla durmalı,
// hem ana menüdeki dükkan hem oyundaki dost buradan okur.
//
// Yeni skin eklemek için: Assets/Resources/CompanionSkinDatabase'i seç,
// Skins listesine eleman ekle, benzersiz bir Id yaz, Sprite'ı sürükle, fiyat ver.
// Listedeki İLK skin varsayılandır: herkeste vardır ve ücretsizdir.
[CreateAssetMenu(
    fileName = "CompanionSkinDatabase",
    menuName = "Companion/Skin Database"
)]
public class CompanionSkinDatabase : ScriptableObject
{
    public const string ResourcePath = "CompanionSkinDatabase";

    [System.Serializable]
    public class Skin
    {
        [Tooltip("Kayıt için kullanılan benzersiz isim. Yayınladıktan sonra değiştirme, oyuncunun satın alımı kaybolur.")]
        public string id;

        public string displayName;

        [Tooltip("Sabit görsel. Animation Frames doluysa kullanılmaz. İkisi de boşsa dostun sahnedeki orijinal sprite'ı kullanılır.")]
        public Sprite sprite;

        [Tooltip("Animasyon kareleri (örn. idle). Sırayla döngüde oynatılır.")]
        public Sprite[] animationFrames;

        [Tooltip("Animasyon hızı (saniyedeki kare sayısı).")]
        [Min(0.1f)]
        public float framesPerSecond = 8f;

        [Tooltip("Dostun sahnedeki boyutuyla çarpılır. Görsel çok büyük/küçük görünürse ayarla.")]
        [Min(0.01f)]
        public float scale = 1f;

        [Tooltip("Görsel ters yöne bakıyorsa işaretle (yatay çevirir).")]
        public bool flipX;

        [Tooltip("Sprite'ın rengiyle çarpılır. Beyaz = değişiklik yok.")]
        public Color tint = Color.white;

        [Tooltip("Kalıcı gold fiyatı. Premium skinlerde kullanılmaz.")]
        [Min(0)]
        public int price = 100;

        [Tooltip("Doluysa skin gerçek parayla satılır (Google Play / App Store ürün ID'si, örn. skin_jet). Boşsa gold ile alınır.")]
        public string iapProductId;

        [Tooltip("İşaretliyse dükkanda satılmaz, sadece bir başarımın ödülü olarak açılır (AchievementCatalog).")]
        public bool unlockedByAchievement;

        public bool IsPremium =>
            !string.IsNullOrEmpty(iapProductId);

        public bool IsAnimated =>
            animationFrames != null && animationFrames.Length > 1;

        public float FramesPerSecond =>
            framesPerSecond > 0f ? framesPerSecond : 8f;

        public float Scale =>
            scale > 0f ? scale : 1f;

        // Önizleme / ilk kare: animasyonun ilk karesi, yoksa sabit sprite.
        public Sprite FirstSprite
        {
            get
            {
                if (animationFrames != null)
                {
                    foreach (Sprite frame in animationFrames)
                    {
                        if (frame != null)
                            return frame;
                    }
                }

                return sprite;
            }
        }

        // Zamana göre oynatılacak kare. Boş kareler atlanmaz, null dönebilir.
        public Sprite GetFrame(float time)
        {
            if (!IsAnimated)
                return FirstSprite;

            int index =
                Mathf.FloorToInt(time * FramesPerSecond) %
                animationFrames.Length;

            return animationFrames[index] != null
                ? animationFrames[index]
                : FirstSprite;
        }
    }

    public List<Skin> skins = new List<Skin>();

    private static CompanionSkinDatabase cached;

    public static CompanionSkinDatabase Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<CompanionSkinDatabase>(ResourcePath);

            if (cached == null)
            {
                Debug.LogWarning(
                    "CompanionSkinDatabase bulunamadı! Resources klasöründe '" +
                    ResourcePath + "' adlı asset olmalı."
                );
            }
        }

        return cached;
    }

    public Skin DefaultSkin =>
        skins.Count > 0 ? skins[0] : null;

    public bool IsDefault(Skin skin) =>
        skin != null && skin == DefaultSkin;

    public Skin Find(string skinId)
    {
        if (string.IsNullOrEmpty(skinId))
            return null;

        foreach (Skin skin in skins)
        {
            if (skin != null && skin.id == skinId)
                return skin;
        }

        return null;
    }

    public Skin FindByProductId(string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return null;

        foreach (Skin skin in skins)
        {
            if (skin != null && skin.iapProductId == productId)
                return skin;
        }

        return null;
    }

    public bool IsOwned(Skin skin)
    {
        if (skin == null)
            return false;

        return IsDefault(skin) || PlayerProgress.IsCompanionSkinOwned(skin.id);
    }

    // Seçili skin silinmiş ya da sahip olunmuyorsa varsayılana döner.
    public Skin GetSelectedSkin()
    {
        Skin selected = Find(PlayerProgress.SelectedCompanionSkin);

        return IsOwned(selected) ? selected : DefaultSkin;
    }
}
