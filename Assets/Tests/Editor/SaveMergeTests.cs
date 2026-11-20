using System.Collections.Generic;
using NUnit.Framework;

// Bulut kayıt birleştirme: iki cihazın ilerlemesi birleşirken hiçbir kazanım
// (özellikle parayla alınanlar) kaybolmamalı.
public class SaveMergeTests
{
    static SaveSnapshot Snapshot(long modifiedAt)
    {
        return new SaveSnapshot { modifiedAtUtc = modifiedAt };
    }

    [Test]
    public void Merge_OwnedSkinsAreUnion()
    {
        SaveSnapshot a = Snapshot(100);
        a.ownedSkins = new List<string> { "drone_jet" };

        SaveSnapshot b = Snapshot(200);
        b.ownedSkins = new List<string> { "drone_chopper" };

        SaveSnapshot merged = SaveSnapshot.Merge(a, b);

        CollectionAssert.IsSupersetOf(merged.ownedSkins, new[] { "drone_jet", "drone_chopper" });
    }

    [Test]
    public void Merge_WeaponsAndBulletSkinsAreUnion()
    {
        SaveSnapshot a = Snapshot(100);
        a.ownedWeapons = new List<string> { "laser" };
        a.ownedBulletSkins = new List<string> { "gold" };

        SaveSnapshot b = Snapshot(50);
        b.ownedWeapons = new List<string> { "rocket" };
        b.ownedBulletSkins = new List<string> { "season_flame" };

        SaveSnapshot merged = SaveSnapshot.Merge(a, b);

        CollectionAssert.IsSupersetOf(merged.ownedWeapons, new[] { "laser", "rocket" });
        CollectionAssert.IsSupersetOf(merged.ownedBulletSkins, new[] { "gold", "season_flame" });
    }

    [Test]
    public void Merge_UpgradeAndWeaponLevelsTakeHighest()
    {
        SaveSnapshot a = Snapshot(100);
        a.permanentUpgrades = new List<string> { "damage:3", "fire_rate:1" };
        a.weaponLevels = new List<string> { "laser:4" };

        SaveSnapshot b = Snapshot(200);
        b.permanentUpgrades = new List<string> { "damage:2", "fire_rate:5" };
        b.weaponLevels = new List<string> { "laser:2", "rifle:3" };

        SaveSnapshot merged = SaveSnapshot.Merge(a, b);

        CollectionAssert.Contains(merged.permanentUpgrades, "damage:3");
        CollectionAssert.Contains(merged.permanentUpgrades, "fire_rate:5");
        CollectionAssert.Contains(merged.weaponLevels, "laser:4");
        CollectionAssert.Contains(merged.weaponLevels, "rifle:3");
    }

    [Test]
    public void Merge_StatsTakeHighest_GoldTakesNewest()
    {
        SaveSnapshot a = Snapshot(100);
        a.highScore = 9000;
        a.totalKills = 50;
        a.totalGold = 999;

        SaveSnapshot b = Snapshot(200);
        b.highScore = 100;
        b.totalKills = 70;
        b.totalGold = 10;

        SaveSnapshot merged = SaveSnapshot.Merge(a, b);

        Assert.AreEqual(9000, merged.highScore);
        Assert.AreEqual(70, merged.totalKills);
        Assert.AreEqual(10, merged.totalGold, "Altın en son değişen cihazdan gelmeli");
        Assert.AreEqual(200, merged.modifiedAtUtc);
    }

    [Test]
    public void Merge_SameSeason_PremiumIsKept_ClaimsAreUnion()
    {
        SaveSnapshot phone = Snapshot(100);
        phone.seasonIndex = 10;
        phone.seasonPoints = 2000;
        phone.seasonPremium = 1;
        phone.seasonPremiumClaimed = "1,2";

        SaveSnapshot tablet = Snapshot(500);
        tablet.seasonIndex = 10;
        tablet.seasonPoints = 800;
        tablet.seasonPremium = 0;
        tablet.seasonFreeClaimed = "1";
        tablet.seasonPremiumClaimed = "3";

        SaveSnapshot merged = SaveSnapshot.Merge(phone, tablet);

        Assert.AreEqual(1, merged.seasonPremium, "Parayla alınan premium kaybolmamalı");
        Assert.AreEqual(2000, merged.seasonPoints);
        Assert.AreEqual("1,2,3", merged.seasonPremiumClaimed);
        Assert.AreEqual("1", merged.seasonFreeClaimed);
    }

    [Test]
    public void Merge_DifferentSeasons_NewestSeasonWins()
    {
        SaveSnapshot old = Snapshot(900);
        old.seasonIndex = 9;
        old.seasonPremium = 1;
        old.seasonPoints = 9000;

        SaveSnapshot current = Snapshot(100);
        current.seasonIndex = 10;
        current.seasonPoints = 400;

        SaveSnapshot merged = SaveSnapshot.Merge(old, current);

        Assert.AreEqual(10, merged.seasonIndex);
        Assert.AreEqual(400, merged.seasonPoints);
        Assert.AreEqual(0, merged.seasonPremium, "Eski sezonun premiumu yeni sezona taşınmamalı");
    }

    [Test]
    public void Merge_IsOrderIndependentForOwnership()
    {
        SaveSnapshot a = Snapshot(100);
        a.ownedSkins = new List<string> { "x" };
        a.seasonIndex = 3;
        a.seasonPremium = 1;

        SaveSnapshot b = Snapshot(200);
        b.ownedSkins = new List<string> { "y" };
        b.seasonIndex = 3;

        SaveSnapshot ab = SaveSnapshot.Merge(a, b);
        SaveSnapshot ba = SaveSnapshot.Merge(b, a);

        CollectionAssert.AreEquivalent(ab.ownedSkins, ba.ownedSkins);
        Assert.AreEqual(ab.seasonPremium, ba.seasonPremium);
    }

    [Test]
    public void Merge_NullSide_ReturnsOther()
    {
        SaveSnapshot a = Snapshot(5);
        Assert.AreSame(a, SaveSnapshot.Merge(a, null));
        Assert.AreSame(a, SaveSnapshot.Merge(null, a));
    }

    [Test]
    public void Json_RoundTripKeepsEverything()
    {
        SaveSnapshot original = Snapshot(123);
        original.totalGold = 4567;
        original.ownedSkins = new List<string> { "drone_season" };
        original.weaponLevels = new List<string> { "sniper:5" };
        original.seasonIndex = 10;
        original.seasonPremium = 1;
        original.seasonPremiumClaimed = "5,10,15";
        original.ownedBulletSkins = new List<string> { "season_flame" };

        SaveSnapshot copy = SaveSnapshot.FromJson(original.ToJson());

        Assert.IsTrue(original.ContentEquals(copy));
    }

    [Test]
    public void Json_CorruptDataReturnsNull()
    {
        Assert.IsNull(SaveSnapshot.FromJson("{bozuk"));
        Assert.IsNull(SaveSnapshot.FromJson(string.Empty));
    }
}
