using System.Collections.Generic;
using NUnit.Framework;

// İçerik tutarlılığı: çeviri sözlüğü, kimliklerin benzersizliği, günlük meydan okuma.
public class ContentTests
{
    [Test]
    public void Localization_TableLoads()
    {
        // Sözlükte aynı Türkçe metin iki kez varsa oyun açılırken çöker; burada yakalanır.
        Assert.Greater(LocEnglish.Table.Count, 100);
    }

    [Test]
    public void Localization_KeyTextsHaveEnglish()
    {
        string[] keys =
        {
            "OYNA", "AYARLAR", "MAĞAZA", "SİLAHLAR", "GÖREVLER", "SEZON",
            "BOSS GELİYOR!", "SEVİYE ATLADIN!", "ALTIN", "KALKAN KIRICI", "ANLADIM"
        };

        foreach (string key in keys)
        {
            Assert.IsTrue(LocEnglish.Table.ContainsKey(key), "İngilizcesi eksik: " + key);
        }
    }

    [Test]
    public void Weapons_HaveUniqueIdsAndValidStats()
    {
        HashSet<string> ids = new HashSet<string>();

        foreach (WeaponCatalog.Weapon weapon in WeaponCatalog.All)
        {
            Assert.IsTrue(ids.Add(weapon.id), "Tekrarlanan silah kimliği: " + weapon.id);
            Assert.Greater(weapon.fireIntervalMultiplier, 0f);
            Assert.Greater(weapon.damageMultiplier, 0f);
            Assert.GreaterOrEqual(weapon.pellets, 1);
        }

        Assert.AreEqual(0, WeaponCatalog.Rifle.price, "Başlangıç silahı ücretsiz olmalı");
    }

    [Test]
    public void BulletSkins_HaveUniqueIds()
    {
        HashSet<string> ids = new HashSet<string>();

        foreach (BulletSkins.Skin skin in BulletSkins.All)
        {
            Assert.IsTrue(ids.Add(skin.id), "Tekrarlanan mermi görünümü: " + skin.id);
        }

        Assert.IsNotNull(BulletSkins.Find(BulletSkins.SeasonSkinId));
    }

    [Test]
    public void CompanionSkins_HaveUniqueIds_AndSeasonSkinsExist()
    {
        CompanionSkinDatabase database = CompanionSkinDatabase.Load();
        Assert.IsNotNull(database, "Resources/CompanionSkinDatabase bulunamadı");

        HashSet<string> ids = new HashSet<string>();

        foreach (CompanionSkinDatabase.Skin skin in database.skins)
        {
            Assert.IsTrue(ids.Add(skin.id), "Tekrarlanan drone görünümü: " + skin.id);
        }

        Assert.IsNotNull(database.Find(SeasonPass.RewardSkinId), "Sezon ödülü drone eksik");
        Assert.IsNotNull(database.Find(SeasonPass.FreeRewardSkinId), "Ücretsiz sezon drone eksik");
    }

    [Test]
    public void DailyChallenge_IsSameForEveryoneToday()
    {
        Assert.AreSame(DailyChallenge.TodayWeapon, DailyChallenge.TodayWeapon);
        Assert.AreEqual(DailyChallenge.TodayStage, DailyChallenge.TodayStage);
        Assert.AreEqual(DailyChallenge.TodayModifier, DailyChallenge.TodayModifier);

        Assert.GreaterOrEqual(DailyChallenge.TodayStage, 1);
        Assert.Less(DailyChallenge.TodayStage, StageThemes.ThemeCount);
    }

    [Test]
    public void DailyChallenge_HasNoEffectOutsideChallenge()
    {
        Assert.IsFalse(DailyChallenge.IsActive);
        Assert.AreEqual(1f, DailyChallenge.GoldMultiplier);
        Assert.AreEqual(1f, DailyChallenge.EnemySpeedMultiplier);
        Assert.AreEqual(1f, DailyChallenge.PlayerDamageMultiplier);
        Assert.AreEqual(1f, DailyChallenge.PlayerHealthMultiplier);
        Assert.AreEqual(BossWaveManager.LevelsPerBoss, DailyChallenge.LevelsPerBoss);
    }

    [Test]
    public void Stages_AtLeastSix()
    {
        Assert.GreaterOrEqual(StageThemes.ThemeCount, 6);
        Assert.AreEqual(0, StageThemes.StageForLevel(1));
        Assert.AreEqual(0, StageThemes.StageForLevel(10));
        Assert.AreEqual(1, StageThemes.StageForLevel(11));
    }
}
