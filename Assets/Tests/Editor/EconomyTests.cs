using NUnit.Framework;
using UnityEngine;

// Altın, silah güçlendirme, mermi görünümü ve satın alma hesapları.
public class EconomyTests
{
    private PrefsSandbox sandbox;

    [SetUp]
    public void SetUp()
    {
        sandbox = new PrefsSandbox(PrefsSandbox.ProgressKeys());
    }

    [TearDown]
    public void TearDown()
    {
        sandbox.Restore();
    }

    // ---------- Altın ----------

    [Test]
    public void Gold_AddAndSpend()
    {
        PlayerProgress.AddGold(500);
        Assert.AreEqual(500, PlayerProgress.TotalGold);

        Assert.IsTrue(PlayerProgress.TrySpendGold(200));
        Assert.AreEqual(300, PlayerProgress.TotalGold);
    }

    [Test]
    public void Gold_CannotGoNegative()
    {
        PlayerProgress.AddGold(100);

        Assert.IsFalse(PlayerProgress.TrySpendGold(101));
        Assert.AreEqual(100, PlayerProgress.TotalGold);

        PlayerProgress.AddGold(-50);
        Assert.AreEqual(100, PlayerProgress.TotalGold, "Negatif altın eklenmemeli");
    }

    [Test]
    public void GoldBonus_IsCapped()
    {
        Assert.AreEqual(1f, EnemyHealth.CombinedGoldBonus(1f, 1f, 1f), 0.0001f);
        Assert.AreEqual(1.4f, EnemyHealth.CombinedGoldBonus(1f, 1.4f, 1f), 0.0001f);

        // Hepsi en yüksekte: üst sınırı geçmemeli
        float maxed = EnemyHealth.CombinedGoldBonus(1.5f, 1.4f, 1.15f);
        Assert.AreEqual(EnemyHealth.MaxGoldBonusMultiplier, maxed, 0.0001f);
    }

    [Test]
    public void Difficulty_GoldOrdering()
    {
        Difficulty.Current = Difficulty.Level.Easy;
        float easy = Difficulty.GoldMultiplier;

        Difficulty.Current = Difficulty.Level.Normal;
        float normal = Difficulty.GoldMultiplier;

        Difficulty.Current = Difficulty.Level.Hard;
        float hard = Difficulty.GoldMultiplier;

        Assert.Less(easy, normal);
        Assert.Less(normal, hard);
        Assert.AreEqual(1f, normal, 0.0001f);
    }

    // ---------- Silah güçlendirme ----------

    [Test]
    public void WeaponUpgrade_CostRisesThenStopsAtMax()
    {
        WeaponCatalog.Weapon rifle = WeaponCatalog.Rifle;
        int previous = 0;

        for (int level = 1; level < WeaponCatalog.MaxLevel; level++)
        {
            int cost = WeaponCatalog.UpgradeCost(rifle);
            Assert.Greater(cost, previous, "Her seviye öncekinden pahalı olmalı");
            previous = cost;

            PlayerProgress.AddGold(cost);
            Assert.IsTrue(WeaponCatalog.TryUpgrade(rifle));
        }

        Assert.AreEqual(WeaponCatalog.MaxLevel, WeaponCatalog.GetLevel(rifle));
        Assert.AreEqual(0, WeaponCatalog.UpgradeCost(rifle));

        PlayerProgress.AddGold(100000);
        Assert.IsFalse(WeaponCatalog.TryUpgrade(rifle), "Maks. seviyeden sonra güçlenmemeli");
    }

    [Test]
    public void WeaponUpgrade_NeedsGoldAndOwnership()
    {
        WeaponCatalog.Weapon laser = WeaponCatalog.Laser;

        // Sahip olunmayan silah güçlenmez
        PlayerProgress.AddGold(100000);
        Assert.IsFalse(WeaponCatalog.TryUpgrade(laser));

        PlayerProgress.UnlockWeapon(laser.id);
        PlayerProgress.TrySpendGold(PlayerProgress.TotalGold);

        // Altın yoksa güçlenmez ve seviye değişmez
        Assert.IsFalse(WeaponCatalog.TryUpgrade(laser));
        Assert.AreEqual(1, WeaponCatalog.GetLevel(laser));
    }

    [Test]
    public void WeaponUpgrade_SpendsExactCost()
    {
        int cost = WeaponCatalog.UpgradeCost(WeaponCatalog.Rifle);
        PlayerProgress.AddGold(cost + 7);

        Assert.IsTrue(WeaponCatalog.TryUpgrade(WeaponCatalog.Rifle));
        Assert.AreEqual(7, PlayerProgress.TotalGold);
        Assert.Greater(WeaponCatalog.DamageBonus(WeaponCatalog.Rifle), 1f);
    }

    [Test]
    public void WeaponBuy_SpendsPriceOnce()
    {
        WeaponCatalog.Weapon shotgun = WeaponCatalog.Shotgun;
        PlayerProgress.AddGold(shotgun.price * 2);

        Assert.IsTrue(WeaponCatalog.TryBuy(shotgun));
        Assert.IsFalse(WeaponCatalog.TryBuy(shotgun), "Aynı silah iki kez satın alınmamalı");
        Assert.AreEqual(shotgun.price, PlayerProgress.TotalGold);
    }

    // ---------- Mermi görünümleri ----------

    [Test]
    public void BulletSkin_BuySelectAndSeasonOnly()
    {
        BulletSkins.Skin gold = BulletSkins.Find("gold");
        BulletSkins.Skin season = BulletSkins.Find(BulletSkins.SeasonSkinId);

        Assert.IsTrue(BulletSkins.IsOwned(BulletSkins.Find(BulletSkins.ClassicId)));
        Assert.IsFalse(BulletSkins.IsOwned(gold));

        PlayerProgress.AddGold(gold.price);
        Assert.IsTrue(BulletSkins.TryBuy(gold));
        BulletSkins.Select(gold);
        Assert.AreSame(gold, BulletSkins.Selected);

        // Sezon görünümü altınla alınamaz
        PlayerProgress.AddGold(100000);
        Assert.IsFalse(BulletSkins.TryBuy(season));
        Assert.IsFalse(BulletSkins.IsOwned(season));
    }

    // ---------- Gerçek para satın almaları ----------

    [Test]
    public void Transaction_IsGrantedOnlyOnce()
    {
        Assert.IsTrue(PlayerProgress.TryMarkTransactionGranted("GPA.1234-TEST"));
        Assert.IsFalse(PlayerProgress.TryMarkTransactionGranted("GPA.1234-TEST"), "Aynı satın alma iki kez ödül vermemeli");
        Assert.IsTrue(PlayerProgress.TryMarkTransactionGranted("GPA.5678-TEST"));
    }
}
