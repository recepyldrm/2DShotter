using UnityEngine;

// Ana menüden seçilen, oyun tarzını değiştiren silahlar.
// Yeni silah eklemek için All listesine satır ekle.
// Id'leri yayınladıktan sonra değiştirme; sahip olunan silahlar Id ile kaydediliyor.
public static class WeaponCatalog
{
    public class Weapon
    {
        public string id;
        public string title;
        public string description;

        // Kalıcı altın fiyatı (0 = başlangıçta açık)
        public int price;

        // Atışlar arası süre çarpanı (2 = iki kat yavaş ateş)
        public float fireIntervalMultiplier = 1f;

        // Her merminin hasar çarpanı
        public float damageMultiplier = 1f;

        // Tek atışta çıkan mermi sayısı ve yelpaze açısı (derece)
        public int pellets = 1;
        public float spreadAngle;

        // Her merminin yönüne eklenen rastgele sapma (± derece)
        public float randomSpread;

        // Bu silahın mermilerine eklenen kritik vuruş şansı (0.25 = +%25)
        public float bonusCritChance;

        public float speedMultiplier = 1f;

        // Menzil: mermi ömrü çarpanı
        public float lifetimeMultiplier = 1f;

        // Ek delme (çok büyük değer = her şeyi deler)
        public int extraPierce;

        // > 0 ise mermi çarptığı yerde bu yarıçapta tam hasarlı patlama yapar
        public float explosionRadius;

        // Mermi görünümü
        public Color tint = Color.white;
        public Vector2 scale = Vector2.one;

        // Ateş sesinin perdesi (1 = normal, düşük = tok, yüksek = ince)
        public float soundPitch = 1f;
    }

    public static readonly Weapon Rifle = new Weapon
    {
        id = "rifle",
        title = "TÜFEK",
        description = "Dengeli, hızlı ateş eden standart silah.",
        price = 0
    };

    public static readonly Weapon Shotgun = new Weapon
    {
        id = "shotgun",
        title = "POMPALI",
        description = "Yelpaze şeklinde 5 saçma atar. Yakında yıkıcı, menzili kısa.",
        price = 1500,
        fireIntervalMultiplier = 2.2f,
        damageMultiplier = 0.55f,
        pellets = 5,
        spreadAngle = 32f,
        speedMultiplier = 0.9f,
        lifetimeMultiplier = 0.18f,
        tint = new Color(1f, 0.85f, 0.35f, 1f),
        scale = new Vector2(0.7f, 0.7f),
        soundPitch = 0.75f
    };

    public static readonly Weapon Laser = new Weapon
    {
        id = "laser",
        title = "LAZER",
        description = "Çok hızlı ışınlar yoldaki bütün düşmanları delip geçer.",
        price = 3000,
        fireIntervalMultiplier = 1.5f,
        damageMultiplier = 0.9f,
        speedMultiplier = 2.2f,
        extraPierce = 999,
        tint = new Color(0.4f, 0.9f, 1f, 1f),
        scale = new Vector2(2.2f, 0.5f),
        soundPitch = 1.5f
    };

    public static readonly Weapon Rocket = new Weapon
    {
        id = "rocket",
        title = "ROKETATAR",
        description = "Yavaş ama güçlü roketler çarptığı yerde geniş alanda patlar.",
        price = 5000,
        fireIntervalMultiplier = 3f,
        damageMultiplier = 2.5f,
        speedMultiplier = 0.6f,
        lifetimeMultiplier = 1.5f,
        explosionRadius = 1.8f,
        tint = new Color(1f, 0.5f, 0.15f, 1f),
        scale = new Vector2(1.5f, 1.2f),
        soundPitch = 0.55f
    };

    public static readonly Weapon Minigun = new Weapon
    {
        id = "minigun",
        title = "MİNİGUN",
        description = "Mermi yağmuru: çok hızlı ama dağınık ateş eder.",
        price = 2200,
        fireIntervalMultiplier = 0.4f,
        damageMultiplier = 0.45f,
        randomSpread = 7f,
        speedMultiplier = 1.15f,
        lifetimeMultiplier = 0.8f,
        tint = new Color(1f, 0.95f, 0.6f, 1f),
        scale = new Vector2(0.8f, 0.6f),
        soundPitch = 1.25f
    };

    public static readonly Weapon Sniper = new Weapon
    {
        id = "sniper",
        title = "KESKİN NİŞANCI",
        description = "Yavaş ateş eder; tek mermisi 2 düşmanı deler ve sık sık kritik vurur.",
        price = 4000,
        fireIntervalMultiplier = 3.5f,
        damageMultiplier = 4f,
        extraPierce = 2,
        bonusCritChance = 0.3f,
        speedMultiplier = 2.6f,
        lifetimeMultiplier = 1.4f,
        tint = new Color(0.75f, 1f, 0.45f, 1f),
        scale = new Vector2(2.6f, 0.45f),
        soundPitch = 0.65f
    };

    public static readonly Weapon[] All =
    {
        Rifle,
        Shotgun,
        Minigun,
        Laser,
        Sniper,
        Rocket
    };

    public static Weapon Find(string id)
    {
        foreach (Weapon weapon in All)
        {
            if (weapon.id == id)
                return weapon;
        }

        return null;
    }

    public static bool IsOwned(Weapon weapon) =>
        weapon.price <= 0 || PlayerProgress.IsWeaponOwned(weapon.id);

    // Seçili silah (sahip olunmuyorsa ya da silindiyse tüfek)
    public static Weapon Selected
    {
        get
        {
            Weapon weapon = Find(PlayerProgress.SelectedWeapon);
            return weapon != null && IsOwned(weapon) ? weapon : Rifle;
        }
    }

    public static bool TryBuy(Weapon weapon)
    {
        if (IsOwned(weapon))
            return false;

        if (!PlayerProgress.TrySpendGold(weapon.price))
            return false;

        PlayerProgress.UnlockWeapon(weapon.id);
        return true;
    }
}
