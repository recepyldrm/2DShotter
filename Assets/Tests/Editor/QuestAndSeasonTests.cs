using NUnit.Framework;
using UnityEngine;

// Görev panosu (günlük / haftalık) ve sezon bileti kuralları.
public class QuestAndSeasonTests
{
    private PrefsSandbox sandbox;

    [SetUp]
    public void SetUp()
    {
        sandbox = new PrefsSandbox(PrefsSandbox.ProgressKeys());
        QuestBoard.ResetAllForDebug();
    }

    [TearDown]
    public void TearDown()
    {
        sandbox.Restore();
    }

    // ---------- Görevler ----------

    [Test]
    public void Quests_ThreeDistinctPerBoard()
    {
        foreach (QuestBoard board in QuestBoard.All)
        {
            Assert.AreEqual(QuestBoard.QuestCount, board.Current.Count);

            System.Collections.Generic.HashSet<QuestBoard.QuestType> types = new System.Collections.Generic.HashSet<QuestBoard.QuestType>();
            foreach (QuestBoard.Quest quest in board.Current)
            {
                Assert.IsTrue(types.Add(quest.type), "Aynı panoda aynı görev türü iki kez olmamalı");
                Assert.Greater(quest.target, 0);
                Assert.Greater(quest.reward, 0);
            }
        }
    }

    [Test]
    public void Quests_ListIsStableWithinPeriod()
    {
        string first = Describe(QuestBoard.Daily);

        // Oyuncu ilerleyip yeni görev türleri açılsa da aynı gün liste değişmemeli
        PlayerPrefs.SetInt(GameKeys.StatBestLevel, 50);

        Assert.AreEqual(first, Describe(QuestBoard.Daily));
    }

    [Test]
    public void Quests_NewPlayerGetsOnlyReachableQuests()
    {
        PlayerPrefs.SetInt(GameKeys.StatBestLevel, 0);
        QuestBoard.ResetAllForDebug();

        foreach (QuestBoard.Quest quest in QuestBoard.Daily.Current)
        {
            Assert.AreNotEqual(QuestBoard.QuestType.BossStuns, quest.type);
            Assert.AreNotEqual(QuestBoard.QuestType.ShieldedKills, quest.type);
            Assert.AreNotEqual(QuestBoard.QuestType.HardRuns, quest.type);
        }
    }

    [Test]
    public void Quests_ClaimOnlyWhenComplete_AndOnce()
    {
        QuestBoard board = QuestBoard.Daily;
        QuestBoard.Quest quest = board.Current[0];

        Assert.AreEqual(0, board.Claim(quest), "Tamamlanmamış görev ödül vermemeli");

        QuestBoard.CompleteAllForDebug();
        int goldBefore = PlayerProgress.TotalGold;

        Assert.AreEqual(quest.reward, board.Claim(quest));
        Assert.AreEqual(goldBefore + quest.reward, PlayerProgress.TotalGold);
        Assert.AreEqual(0, board.Claim(quest), "Ödül iki kez alınmamalı");
    }

    [Test]
    public void Quests_ProgressIsCappedAtTarget()
    {
        QuestBoard.RecordAll(100000, 100000, 100, true, 100);

        foreach (QuestBoard board in QuestBoard.All)
        {
            foreach (QuestBoard.Quest quest in board.Current)
            {
                Assert.LessOrEqual(board.GetProgress(quest), quest.target);
            }
        }
    }

    [Test]
    public void WeeklyChest_NeedsAllThreeClaimed()
    {
        QuestBoard weekly = QuestBoard.Weekly;
        QuestBoard.CompleteAllForDebug();

        Assert.IsFalse(weekly.CanClaimChest);

        foreach (QuestBoard.Quest quest in weekly.Current)
        {
            weekly.Claim(quest);
        }

        Assert.IsTrue(weekly.CanClaimChest);
        Assert.AreEqual(weekly.chestReward, weekly.ClaimChest());
        Assert.AreEqual(0, weekly.ClaimChest(), "Sandık iki kez açılmamalı");
    }

    static string Describe(QuestBoard board)
    {
        string text = string.Empty;
        foreach (QuestBoard.Quest quest in board.Current)
            text += quest.type + ":" + quest.target + ":" + quest.reward + ";";
        return text;
    }

    // ---------- Sezon bileti ----------

    [Test]
    public void Season_TierFollowsPoints()
    {
        SeasonPass.ResetForDebug();
        Assert.AreEqual(0, SeasonPass.Tier);

        SeasonPass.AddPoints(SeasonPass.PointsPerTier * 3 + 10);
        Assert.AreEqual(3, SeasonPass.Tier);

        SeasonPass.AddPoints(SeasonPass.PointsPerTier * 1000);
        Assert.AreEqual(SeasonPass.TierCount, SeasonPass.Tier, "Seviye 30'u geçmemeli");
    }

    [Test]
    public void Season_PremiumRewardsNeedPremium()
    {
        SeasonPass.ResetForDebug();
        SeasonPass.AddPoints(SeasonPass.PointsPerTier * 2);

        Assert.IsTrue(SeasonPass.CanClaimFree(1));
        Assert.IsFalse(SeasonPass.CanClaimPremium(1), "Premium alınmadan premium ödül verilmemeli");
        Assert.IsFalse(SeasonPass.CanClaimFree(3), "Ulaşılmamış seviye alınmamalı");

        SeasonPass.UnlockPremium();
        Assert.IsTrue(SeasonPass.CanClaimPremium(1));

        Assert.IsTrue(SeasonPass.ClaimPremium(1));
        Assert.IsFalse(SeasonPass.ClaimPremium(1), "Ödül iki kez alınmamalı");
    }

    [Test]
    public void Season_PremiumIsSavedToCloudSnapshot()
    {
        SeasonPass.ResetForDebug();
        SeasonPass.UnlockPremium();

        SaveSnapshot snapshot = SaveSnapshot.CaptureLocal();

        Assert.AreEqual(1, snapshot.seasonPremium, "Premium bulut kayda girmeli");
        Assert.AreEqual(SeasonPass.SeasonIndex, snapshot.seasonIndex);
    }

    [Test]
    public void Season_FinalSkinIsGivenOnce()
    {
        SeasonPass.ResetForDebug();
        SeasonPass.UnlockPremium();
        SeasonPass.AddPoints(SeasonPass.PointsPerTier * SeasonPass.TierCount);

        SeasonPass.Reward reward = SeasonPass.PremiumReward(SeasonPass.TierCount);

        if (!PlayerProgress.IsCompanionSkinOwned(SeasonPass.RewardSkinId))
        {
            Assert.AreEqual(SeasonPass.RewardSkinId, reward.skinId);
        }

        Assert.IsTrue(SeasonPass.ClaimPremium(SeasonPass.TierCount));
        Assert.IsTrue(PlayerProgress.IsCompanionSkinOwned(SeasonPass.RewardSkinId));

        // Görünüm artık var: aynı seviye bir daha görünüm vermez, altın gösterir
        Assert.IsFalse(SeasonPass.PremiumReward(SeasonPass.TierCount).IsSkin);
    }
}
