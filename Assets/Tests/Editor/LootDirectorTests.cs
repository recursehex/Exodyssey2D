using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Tests the pure loot director with synthetic candidates and contexts so
/// every behavior (ramp, gate, cap, anti-chain, downgrade) is checked in
/// isolation from the JSON databases
/// </summary>
public class LootDirectorTests
{
    private const int commonIndex = 0;
    private const int limitedIndex = 1;
    private const int scarceIndex = 2;
    private const int rareIndex = 3;
    private const int anomalousIndex = 4;

    private static LootDirector.Candidate MakeCandidate(int index, Rarity rarity, int minRegion = 0, int lootWeight = 100, bool unique = false)
    {
        return new LootDirector.Candidate
        {
            index = index,
            Rarity = rarity,
            lootWeight = lootWeight,
            minRegionIndex = minRegion,
            uniquePerRun = unique,
            isWeapon = true,
        };
    }

    /// <summary>
    /// One candidate per tier, whose index equals its tier
    /// </summary>
    private static List<LootDirector.Candidate> OnePerTier(int anomalousMinRegion = 0)
    {
        return new List<LootDirector.Candidate>
        {
            MakeCandidate(commonIndex, Rarity.Common),
            MakeCandidate(limitedIndex, Rarity.Limited),
            MakeCandidate(scarceIndex, Rarity.Scarce),
            MakeCandidate(rareIndex, Rarity.Rare),
            MakeCandidate(anomalousIndex, Rarity.Anomalous, anomalousMinRegion),
        };
    }

    private static LootDirector.Context MakeContext(
        int[] start, int[] end = null, int regionIndex = 1, int gridsCompleted = 0, int gridsRequired = 5, int cap = 0)
    {
        return new LootDirector.Context
        {
            regionIndex = regionIndex,
            gridsCompleted = gridsCompleted,
            gridsRequired = gridsRequired,
            RarityWeightsStart = start,
            RarityWeightsEnd = end ?? start,
            anomalousCap = cap,
            Profile = LootProfileInfo.GetProfile(LootProfileInfo.DefaultProfileName),
        };
    }

    private static LootDirector MakeDirector(List<LootDirector.Candidate> candidates)
    {
        return new LootDirector(candidates, new LootTuning());
    }

    // --- Interpolation ---

    [Test]
    public void EffectiveWeights_InterpolateByRegionProgress()
    {
        LootDirector director = MakeDirector(OnePerTier());
        int[] start = { 50, 36, 12, 2, 0 };
        int[] end = { 46, 35, 15, 4, 0 };
        float[] atStart = director.GetEffectiveWeights(MakeContext(start, end, gridsCompleted: 0));
        float[] atEnd = director.GetEffectiveWeights(MakeContext(start, end, gridsCompleted: 5));
        float[] pastEnd = director.GetEffectiveWeights(MakeContext(start, end, gridsCompleted: 9));
        Assert.AreEqual(2f, atStart[LootDirector.rareTier], 0.001f);
        Assert.AreEqual(4f, atEnd[LootDirector.rareTier], 0.001f);
        Assert.AreEqual(4f, pastEnd[LootDirector.rareTier], 0.001f, "progress past the region length should clamp");
    }

    // --- Anomalous gate ---

    [Test]
    public void ZeroAnomalousWeight_NeverProducesAnomalous()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 50, 36, 12, 2, 0 }, cap: 5);
        Random rng = new Random(1234);
        for (int grid = 0; grid < 200; grid++)
        {
            foreach (int index in director.PlanGridLoot(context, rng))
                Assert.AreNotEqual(anomalousIndex, index, "Anomalous item generated despite a zero weight gate");
        }
    }

    [Test]
    public void MinRegionGate_DowngradesToRare()
    {
        // The only Anomalous candidate is gated to region 4; rolling in region 3
        // with a guaranteed-Anomalous table must fall back to the Rare item
        LootDirector director = MakeDirector(OnePerTier(anomalousMinRegion: 4));
        LootDirector.Context context = MakeContext(new[] { 0, 0, 0, 0, 100 }, regionIndex: 3, cap: 99);
        Random rng = new Random(99);
        int index = director.RollItem(context, rng);
        Assert.AreEqual(rareIndex, index);
    }

    [Test]
    public void MinRegionGate_AllowsItemOnceRegionReached()
    {
        LootDirector director = MakeDirector(OnePerTier(anomalousMinRegion: 4));
        LootDirector.Context context = MakeContext(new[] { 0, 0, 0, 0, 100 }, regionIndex: 4, cap: 99);
        Random rng = new Random(99);
        Assert.AreEqual(anomalousIndex, director.RollItem(context, rng));
    }

    // --- Anomalous cap ---

    [Test]
    public void AnomalousCap_IsNeverExceededInARegion()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 40, 30, 15, 10, 5 }, cap: 1);
        Random rng = new Random(42);
        for (int grid = 0; grid < 60; grid++)
            director.PlanGridLoot(context, rng);
        Assert.AreEqual(1, director.State.anomalousSpawnedThisRegion,
            "with 60 grids at 5% Anomalous weight the cap of 1 should be reached exactly");
    }

    [Test]
    public void AnomalousCap_ResetsOnNewRegion()
    {
        LootDirector director = MakeDirector(OnePerTier());
        director.State.anomalousSpawnedThisRegion = 1;
        director.State.ResetForNewRegion();
        float[] weights = director.GetEffectiveWeights(MakeContext(new[] { 40, 30, 15, 10, 5 }, cap: 1));
        Assert.Greater(weights[LootDirector.anomalousTier], 0f);
    }

    // --- Anti-chain ---

    [Test]
    public void AntiChain_BlocksAnomalousForFollowingGrids()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 30, 20, 20, 15, 15 }, cap: 99);
        Random rng = new Random(7);
        List<int> anomalousGrids = new();
        for (int grid = 0; grid < 300; grid++)
        {
            List<int> plan = director.PlanGridLoot(context, rng);
            if (plan.Contains(anomalousIndex))
                anomalousGrids.Add(grid);
        }
        Assert.Greater(anomalousGrids.Count, 1, "test needs at least two Anomalous drops to check spacing");
        for (int i = 1; i < anomalousGrids.Count; i++)
            Assert.GreaterOrEqual(anomalousGrids[i] - anomalousGrids[i - 1], 3,
                "an Anomalous drop must be followed by at least 2 fully blocked grids");
    }

    [Test]
    public void AntiChain_BlocksSecondAnomalousInSameGrid()
    {
        LootDirector director = MakeDirector(OnePerTier());
        // All-Anomalous weights: only the first roll of the grid may produce one
        LootDirector.Context context = MakeContext(new[] { 50, 0, 0, 0, 50 }, cap: 99);
        Random rng = new Random(3);
        for (int grid = 0; grid < 50; grid++)
        {
            List<int> plan = director.PlanGridLoot(context, rng);
            int anomalousCount = plan.FindAll(index => index == anomalousIndex).Count;
            Assert.LessOrEqual(anomalousCount, 1, "two Anomalous items generated in one grid");
        }
    }

    // --- Out-of-depth escalation ---

    [Test]
    public void OodEscalation_OnlyHappensFromItsMinRegion()
    {
        LootTuning tuning = new LootTuning();
        int oodMinRegion = (int)tuning.GetOodEscalationMinRegionTag();
        LootDirector earlyDirector = MakeDirector(OnePerTier());
        LootDirector lateDirector = MakeDirector(OnePerTier());
        int[] scarceOnly = { 0, 0, 100, 0, 0 };
        Random rng = new Random(2024);
        int earlyRares = 0, lateRares = 0;
        for (int i = 0; i < 5000; i++)
        {
            if (earlyDirector.RollItem(MakeContext(scarceOnly, regionIndex: oodMinRegion - 1), rng) == rareIndex)
                earlyRares++;
            if (lateDirector.RollItem(MakeContext(scarceOnly, regionIndex: oodMinRegion), rng) == rareIndex)
                lateRares++;
        }
        Assert.AreEqual(0, earlyRares, "escalation must never fire before its min region");
        Assert.Greater(lateRares, 0, "escalation should occasionally fire from its min region");
        Assert.Less(lateRares, 5000 * 0.08f, "escalation should stay a rare event");
    }

    // --- Downgrade fallback ---

    [Test]
    public void EmptyTier_DowngradesInsteadOfFailing()
    {
        // No Rare candidate exists: a guaranteed-Rare roll must fall to Scarce
        List<LootDirector.Candidate> candidates = new()
        {
            MakeCandidate(commonIndex, Rarity.Common),
            MakeCandidate(scarceIndex, Rarity.Scarce),
        };
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeContext(new[] { 0, 0, 0, 100, 0 });
        Assert.AreEqual(scarceIndex, director.RollItem(context, new Random(5)));
    }

    [Test]
    public void NoCandidatesAtAll_ReturnsMinusOne()
    {
        LootDirector director = MakeDirector(new List<LootDirector.Candidate>());
        LootDirector.Context context = MakeContext(new[] { 50, 30, 15, 4, 1 });
        Assert.AreEqual(-1, director.RollItem(context, new Random(5)));
    }

    // --- Plan shape and determinism ---

    [Test]
    public void PlanGridLoot_CountWithinProfileBounds()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 50, 36, 12, 2, 0 });
        Random rng = new Random(11);
        for (int grid = 0; grid < 50; grid++)
        {
            List<int> plan = director.PlanGridLoot(context, rng);
            Assert.GreaterOrEqual(plan.Count, context.Profile.MinItems);
            Assert.LessOrEqual(plan.Count, context.Profile.MaxItems);
        }
    }

    [Test]
    public void SameSeedAndState_ProducesIdenticalPlans()
    {
        List<int> RunPlans(int seed)
        {
            LootDirector director = MakeDirector(OnePerTier());
            LootDirector.Context context = MakeContext(new[] { 40, 30, 15, 10, 5 }, cap: 3);
            List<int> all = new();
            for (int grid = 0; grid < 20; grid++)
                all.AddRange(director.PlanGridLoot(context, new Random(seed ^ grid)));
            return all;
        }
        CollectionAssert.AreEqual(RunPlans(777), RunPlans(777));
    }

    // --- Rare pity ---

    [Test]
    public void Pity_AccumulatesOnNonRareDrops_WhereRareIsPossible()
    {
        LootDirector director = MakeDirector(OnePerTier());
        director.State.ResetForNewRun(0, -2f);
        // Rare weight is 0 at region start but positive at the end, so pity
        // accumulates even though nothing but Common can roll right now
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 }, new[] { 96, 0, 0, 4, 0 });
        Random rng = new Random(1);
        for (int i = 0; i < 5; i++)
            director.RollItem(context, rng);
        Assert.AreEqual(-2f + 5 * 1.2f, director.State.rarePityOffset, 0.001f);
    }

    [Test]
    public void Pity_DoesNotAccumulateWhereRareIsImpossible()
    {
        LootDirector director = MakeDirector(OnePerTier());
        director.State.ResetForNewRun(0, -2f);
        LootDirector.Context context = MakeContext(new[] { 55, 35, 10, 0, 0 });
        Random rng = new Random(1);
        for (int i = 0; i < 10; i++)
            director.RollItem(context, rng);
        Assert.AreEqual(-2f, director.State.rarePityOffset, 0.001f,
            "tutorial-region drops must not pre-charge the pity meter");
    }

    [Test]
    public void Pity_ResetsToZeroOnRareDrop()
    {
        LootDirector director = MakeDirector(OnePerTier());
        director.State.ResetForNewRun(0, -2f);
        director.State.rarePityOffset = 15f;
        LootDirector.Context context = MakeContext(new[] { 0, 0, 0, 100, 0 });
        director.RollItem(context, new Random(1));
        Assert.AreEqual(0f, director.State.rarePityOffset);
        Assert.IsTrue(director.State.hasRarePlusSpawned);
    }

    [Test]
    public void Pity_CapsAtTuningCap()
    {
        LootTuning tuning = new LootTuning();
        LootDirector director = MakeDirector(OnePerTier());
        director.State.ResetForNewRun(0, 0f);
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 }, new[] { 96, 0, 0, 4, 0 });
        Random rng = new Random(1);
        for (int i = 0; i < 50; i++)
            director.RollItem(context, rng);
        Assert.AreEqual(tuning.rarePityCap, director.State.rarePityOffset, 0.001f);
    }

    [Test]
    public void Pity_NeverUnlocksRareWhereTableSaysZero()
    {
        LootDirector director = MakeDirector(OnePerTier());
        director.State.rarePityOffset = 20f;
        float[] weights = director.GetEffectiveWeights(MakeContext(new[] { 55, 35, 10, 0, 0 }));
        Assert.AreEqual(0f, weights[LootDirector.rareTier]);
    }

    // --- Backstop ---

    [Test]
    public void Backstop_ForcesRareWeaponWhenDue()
    {
        LootDirector director = MakeDirector(OnePerTier());
        // All-Common weights: only the backstop can produce the Rare item.
        // Region index 1 is the default backstop region (Fragmented Coast)
        // and gridsCompleted 3 means grid 4 is being generated
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 }, regionIndex: 1, gridsCompleted: 3);
        List<int> plan = director.PlanGridLoot(context, new Random(8));
        Assert.Contains(rareIndex, plan, "the backstop grid must contain a guaranteed Rare weapon");
    }

    [Test]
    public void Backstop_NotDueOnEarlierGrids()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 }, regionIndex: 1, gridsCompleted: 2);
        for (int grid = 0; grid < 20; grid++)
        {
            List<int> plan = director.PlanGridLoot(context, new Random(grid));
            Assert.IsFalse(plan.Contains(rareIndex), "backstop fired before its configured grid");
        }
    }

    [Test]
    public void Backstop_DoesNotFireOnceRareHasSpawned()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 }, regionIndex: 1, gridsCompleted: 3);
        List<int> firstPlan = director.PlanGridLoot(context, new Random(8));
        Assert.Contains(rareIndex, firstPlan);
        for (int grid = 0; grid < 20; grid++)
        {
            List<int> plan = director.PlanGridLoot(context, new Random(grid));
            Assert.IsFalse(plan.Contains(rareIndex), "backstop must fire at most once per run");
        }
    }

    [Test]
    public void Backstop_CoversLaterRegionsToo()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 }, regionIndex: 2, gridsCompleted: 0);
        List<int> plan = director.PlanGridLoot(context, new Random(8));
        Assert.Contains(rareIndex, plan, "a run that somehow reaches region 3 with no Rare must still be caught");
    }

    // --- Duplicate suppression ---

    [Test]
    public void WithinGrid_ScarcePlusNeverRepeats()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeContext(new[] { 0, 0, 100, 0, 0 });
        Random rng = new Random(21);
        for (int grid = 0; grid < 30; grid++)
        {
            List<int> plan = director.PlanGridLoot(context, rng);
            int scarceCount = plan.FindAll(index => index == scarceIndex).Count;
            Assert.LessOrEqual(scarceCount, 1, "the same Scarce item generated twice in one grid");
        }
    }

    [Test]
    public void WithinGrid_CommonCanRepeat()
    {
        List<LootDirector.Candidate> candidates = new() { MakeCandidate(commonIndex, Rarity.Common) };
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 });
        List<int> plan = director.PlanGridLoot(context, new Random(2));
        Assert.GreaterOrEqual(plan.Count, 3, "Common junk must still be able to repeat within a grid");
        foreach (int index in plan)
            Assert.AreEqual(commonIndex, index);
    }

    [Test]
    public void History_ReducesRepeatChanceAcrossGrids()
    {
        const int itemA = 10, itemB = 11;
        int aCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            List<LootDirector.Candidate> candidates = new()
            {
                MakeCandidate(itemA, Rarity.Scarce),
                MakeCandidate(itemB, Rarity.Scarce),
            };
            LootDirector director = MakeDirector(candidates);
            director.State.PushHistory(itemA, 10);
            LootDirector.Context context = MakeContext(new[] { 0, 0, 100, 0, 0 });
            if (director.RollItem(context, new Random(i)) == itemA)
                aCount++;
        }
        // itemA is weighted x0.35 against itemB's x1, so its share should be
        // near 0.35 / 1.35 = 26%
        Assert.Less(aCount, 400, "recently seen Scarce item was not suppressed");
        Assert.Greater(aCount, 100, "history decay should suppress, not eliminate");
    }

    [Test]
    public void UniquePerRun_SpawnsAtMostOnce()
    {
        List<LootDirector.Candidate> candidates = new()
        {
            MakeCandidate(commonIndex, Rarity.Common),
            MakeCandidate(anomalousIndex, Rarity.Anomalous, unique: true),
        };
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeContext(new[] { 50, 0, 0, 0, 50 }, cap: 99);
        Random rng = new Random(31);
        int totalAnomalous = 0;
        for (int grid = 0; grid < 50; grid++)
            totalAnomalous += director.PlanGridLoot(context, rng).FindAll(index => index == anomalousIndex).Count;
        Assert.AreEqual(1, totalAnomalous, "a uniquePerRun item must generate exactly once given ample chances");
    }
}
