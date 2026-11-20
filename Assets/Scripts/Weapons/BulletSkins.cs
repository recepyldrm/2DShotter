using System.Collections.Generic;
using UnityEngine;

// Mermi görünümleri: bütün silahların mermi rengini değiştirir.
// "Klasik" her silahın kendi rengidir. Bazıları altınla alınır, biri sezon ödülüdür.
// Id'leri yayınladıktan sonra değiştirme; sahip olunanlar Id ile kaydediliyor.
public static class BulletSkins
{
    public class Skin
    {
        public string id;
        public string title;
        public Color color;
        public int price;

        // true ise satılmaz, sadece sezon biletinden gelir
        public bool seasonReward;

        public bool IsClassic => id == ClassicId;
    }

    public const string ClassicId = "classic";
    public const string SeasonSkinId = "season_flame";

    public static readonly Skin[] All =
    {
        new Skin { id = ClassicId, title = "KLASİK", color = Color.white, price = 0 },
        new Skin { id = "emerald", title = "ZÜMRÜT", color = new Color(0.35f, 1f, 0.5f, 1f), price = 600 },
        new Skin { id = "ice", title = "BUZ", color = new Color(0.6f, 0.95f, 1f, 1f), price = 800 },
        new Skin { id = "plasma", title = "MOR PLAZMA", color = new Color(0.78f, 0.4f, 1f, 1f), price = 800 },
        new Skin { id = "crimson", title = "KIZIL", color = new Color(1f, 0.3f, 0.3f, 1f), price = 1000 },
        new Skin { id = "gold", title = "ALTIN", color = new Color(1f, 0.85f, 0.25f, 1f), price = 1500 },
        new Skin { id = SeasonSkinId, title = "SEZON ALEVİ", color = new Color(1f, 0.5f, 0.1f, 1f), price = 0, seasonReward = true }
    };

    public static Skin Find(string id)
    {
        foreach (Skin skin in All)
        {
            if (skin.id == id)
                return skin;
        }

        return null;
    }

    // ==================================
    // SAHİPLİK / SEÇİM
    // ==================================

    static HashSet<string> Owned()
    {
        HashSet<string> owned = new HashSet<string>();

        foreach (string id in PlayerPrefs.GetString(GameKeys.OwnedBulletSkins, string.Empty).Split(','))
        {
            if (!string.IsNullOrEmpty(id))
                owned.Add(id);
        }

        return owned;
    }

    public static List<string> OwnedIds
    {
        get
        {
            List<string> list = new List<string>(Owned());
            list.Sort(System.StringComparer.Ordinal);
            return list;
        }
    }

    public static bool IsOwned(Skin skin) =>
        skin != null && ((skin.price <= 0 && !skin.seasonReward) || Owned().Contains(skin.id));

    public static void Unlock(string id)
    {
        HashSet<string> owned = Owned();

        if (!owned.Add(id))
            return;

        PlayerPrefs.SetString(GameKeys.OwnedBulletSkins, string.Join(",", owned));
        PlayerProgress.NotifySaveDataChanged();
    }

    public static Skin Selected
    {
        get
        {
            Skin skin = Find(PlayerPrefs.GetString(GameKeys.SelectedBulletSkin, ClassicId));
            return skin != null && IsOwned(skin) ? skin : All[0];
        }
    }

    public static void Select(Skin skin)
    {
        if (!IsOwned(skin))
            return;

        PlayerPrefs.SetString(GameKeys.SelectedBulletSkin, skin.id);
        PlayerProgress.NotifySaveDataChanged();
    }

    public static bool TryBuy(Skin skin)
    {
        if (skin == null || skin.seasonReward || IsOwned(skin))
            return false;

        if (!PlayerProgress.TrySpendGold(skin.price))
            return false;

        Unlock(skin.id);
        return true;
    }

    // Merminin rengi: klasikse silahın rengi, değilse görünümün rengi
    public static Color TintFor(WeaponCatalog.Weapon weapon)
    {
        Skin skin = Selected;
        return skin.IsClassic ? weapon.tint : skin.color;
    }
}
