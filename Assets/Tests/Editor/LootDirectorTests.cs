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

    // --- Profiles: rarity shift, guarantees, category bias ---

    private static LootDirector.Context MakeProfileContext(string profileName, int[] weights, int gridsCompleted = 0, int gridsRequired = 5)
    {
        LootDirector.Context context = MakeContext(weights, gridsCompleted: gridsCompleted, gridsRequired: gridsRequired);
        context.Profile = LootProfileInfo.GetProfile(profileName);
        return context;
    }

    [Test]
    public void RarityShiftPlus1_ShiftsRollsUpOneTier()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeProfileContext("CacheCrucible", new[] { 100, 0, 0, 0, 0 });
        List<int> plan = director.PlanGridLoot(context, new Random(4));
        Assert.Greater(plan.Count, 0);
        foreach (int index in plan)
            Assert.AreEqual(limitedIndex, index, "a +1 shift should turn Common rolls into Limited");
    }

    [Test]
    public void RarityShiftPlus1_NeverBypassesAnomalousGate()
    {
        LootDirector director = MakeDirector(OnePerTier());
        // Anomalous weight is 0 (gated region): Rare rolls must stay Rare
        LootDirector.Context context = MakeProfileContext("CacheCrucible", new[] { 0, 0, 0, 100, 0 });
        Random rng = new Random(6);
        for (int grid = 0; grid < 20; grid++)
        {
            foreach (int index in director.PlanGridLoot(context, rng))
                Assert.AreNotEqual(anomalousIndex, index, "+1 shift bypassed the Anomalous gate");
        }
    }

    [Test]
    public void RarityShiftMinus1_ShiftsRollsDownOneTier()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeProfileContext("MemorySite", new[] { 0, 100, 0, 0, 0 });
        List<int> plan = director.PlanGridLoot(context, new Random(4));
        Assert.Greater(plan.Count, 0);
        foreach (int index in plan)
            Assert.AreEqual(commonIndex, index, "a -1 shift should turn Limited rolls into Common");
    }

    [Test]
    public void GuaranteedSlot_IsAlwaysInThePlan()
    {
        const int medicalIndex = 30;
        List<LootDirector.Candidate> candidates = OnePerTier();
        LootDirector.Candidate medical = MakeCandidate(medicalIndex, Rarity.Common);
        medical.Categories.Add(LootCategory.Medical);
        candidates.Add(medical);
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeProfileContext("TriageCorridor", new[] { 100, 0, 0, 0, 0 });
        Random rng = new Random(12);
        for (int grid = 0; grid < 20; grid++)
            Assert.Contains(medicalIndex, director.PlanGridLoot(context, rng),
                "Triage Corridor must always contain a Medical item");
    }

    [Test]
    public void CategoryMultiplier_BiasesPickWithinTier()
    {
        const int medicalIndex = 30, plainIndex = 31;
        int medicalCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            List<LootDirector.Candidate> candidates = new()
            {
                MakeCandidate(plainIndex, Rarity.Common),
            };
            LootDirector.Candidate medical = MakeCandidate(medicalIndex, Rarity.Common);
            medical.Categories.Add(LootCategory.Medical);
            candidates.Add(medical);
            LootDirector director = MakeDirector(candidates);
            // Quarantine Line has Medical x2.0 and no guaranteed slots
            LootDirector.Context context = MakeProfileContext("QuarantineLine", new[] { 100, 0, 0, 0, 0 });
            if (director.RollItem(context, new Random(i)) == medicalIndex)
                medicalCount++;
        }
        // x2.0 vs x1.0 means the medical item should take about 2/3 of picks
        Assert.Greater(medicalCount, 550);
        Assert.Less(medicalCount, 780);
    }

    // --- Fuel meter ---

    private const int fuelIndex = 40;

    private static List<LootDirector.Candidate> OnePerTierPlusFuel()
    {
        List<LootDirector.Candidate> candidates = OnePerTier();
        LootDirector.Candidate fuel = MakeCandidate(fuelIndex, Rarity.Scarce);
        fuel.Categories.Add(LootCategory.Fuel);
        candidates.Add(fuel);
        return candidates;
    }

    [Test]
    public void Fuel_SpawnsAtMostOncePerGrid_AndAtMeterCadence()
    {
        LootDirector director = MakeDirector(OnePerTierPlusFuel());
        // gridsRequired is huge so the region fuel budget never interferes
        LootDirector.Context context = MakeContext(new[] { 50, 36, 12, 2, 0 }, gridsRequired: 1000);
        Random rng = new Random(17);
        int totalFuel = 0;
        for (int grid = 0; grid < 100; grid++)
        {
            int fuelInGrid = director.PlanGridLoot(context, rng).FindAll(index => index == fuelIndex).Count;
            Assert.LessOrEqual(fuelInGrid, 1, "more than one fuel item in a single generic grid");
            totalFuel += fuelInGrid;
        }
        Assert.Greater(totalFuel, 24, "fuel should average about one drop per 2-3 grids");
        Assert.Less(totalFuel, 51, "fuel should stay scarce, not appear most grids");
    }

    [Test]
    public void FuelMeter_NeverAllowsDroughtLongerThan3Grids()
    {
        LootDirector director = MakeDirector(OnePerTierPlusFuel());
        LootDirector.Context context = MakeContext(new[] { 50, 36, 12, 2, 0 }, gridsRequired: 1000);
        Random rng = new Random(23);
        int dryStreak = 0;
        for (int grid = 0; grid < 300; grid++)
        {
            if (director.PlanGridLoot(context, rng).Contains(fuelIndex))
                dryStreak = 0;
            else
                dryStreak++;
            Assert.LessOrEqual(dryStreak, 3, "a fuel drought exceeded 3 consecutive grids");
        }
    }

    [Test]
    public void RegionFuelBudget_GuaranteesMinimumPerRegion()
    {
        LootTuning tuning = new LootTuning();
        for (int seed = 0; seed < 10; seed++)
        {
            LootDirector director = MakeDirector(OnePerTierPlusFuel());
            Random rng = new Random(seed);
            for (int grid = 0; grid < 5; grid++)
                director.PlanGridLoot(MakeContext(new[] { 50, 36, 12, 2, 0 }, gridsCompleted: grid, gridsRequired: 5), rng);
            Assert.GreaterOrEqual(director.State.fuelSpawnedThisRegion, tuning.minFuelItemsPerRegion,
                $"seed {seed}: a region ended below its minimum fuel budget");
        }
    }

    [Test]
    public void FuelSpawns_DoNotCountAsTheRarePlusTaste()
    {
        // PowerCell keeps a display rarity (currently Rare) for UI color, but
        // a fuel drop must never satisfy the run's Rare+ "taste" or touch pity
        List<LootDirector.Candidate> candidates = new() { MakeCandidate(commonIndex, Rarity.Common) };
        LootDirector.Candidate fuel = MakeCandidate(fuelIndex, Rarity.Rare);
        fuel.Categories.Add(LootCategory.Fuel);
        candidates.Add(fuel);
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeContext(new[] { 100, 0, 0, 0, 0 }, gridsRequired: 1000);
        Random rng = new Random(3);
        bool fuelSeen = false;
        for (int grid = 0; grid < 10; grid++)
            fuelSeen |= director.PlanGridLoot(context, rng).Contains(fuelIndex);
        Assert.IsTrue(fuelSeen, "the meter should have spawned fuel within 10 grids");
        Assert.IsFalse(director.State.hasRarePlusSpawned, "a fuel drop counted as the run's first Rare+");
    }

    [Test]
    public void Fuel_NeverComesFromTheRarityRoll()
    {
        // Scarce-only weights with the fuel item as the only Scarce candidate:
        // every rarity roll must downgrade to something else rather than pick fuel
        List<LootDirector.Candidate> candidates = new()
        {
            MakeCandidate(commonIndex, Rarity.Common),
        };
        LootDirector.Candidate fuel = MakeCandidate(fuelIndex, Rarity.Scarce);
        fuel.Categories.Add(LootCategory.Fuel);
        candidates.Add(fuel);
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeContext(new[] { 0, 0, 100, 0, 0 }, gridsRequired: 1000);
        Random rng = new Random(29);
        for (int grid = 0; grid < 30; grid++)
        {
            int fuelInGrid = director.PlanGridLoot(context, rng).FindAll(index => index == fuelIndex).Count;
            Assert.LessOrEqual(fuelInGrid, 1, "fuel leaked into the rarity roll");
        }
    }

    // --- Containers ---

    [Test]
    public void WeaponSafe_OnlyProducesWeapons_AtShiftedRarity()
    {
        const int armorIndex = 50;
        List<LootDirector.Candidate> candidates = OnePerTier();
        foreach (LootDirector.Candidate candidate in candidates)
            candidate.Categories.Add(LootCategory.MeleeWeapon);
        LootDirector.Candidate armor = MakeCandidate(armorIndex, Rarity.Scarce);
        armor.Categories.Add(LootCategory.Armor);
        candidates.Add(armor);
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeProfileContext("WeaponSafe", new[] { 0, 0, 100, 0, 0 });
        Random rng = new Random(41);
        for (int i = 0; i < 30; i++)
        {
            List<int> loot = director.RollContainerLoot(context, rng);
            Assert.AreEqual(1, loot.Count, "a Weapon Safe holds exactly one item");
            Assert.AreNotEqual(armorIndex, loot[0], "Weapon Safe produced a non-weapon");
            // Scarce rolls shift +1 to Rare inside a Weapon Safe
            Assert.Contains(loot[0], new List<int> { rareIndex, scarceIndex, limitedIndex, commonIndex });
        }
    }

    [Test]
    public void ContainerRolls_DoNotAdvanceGridPacingOrFuelMeter()
    {
        LootDirector director = MakeDirector(OnePerTier());
        LootDirector.Context context = MakeProfileContext("ReserveCrate", new[] { 50, 36, 12, 2, 0 });
        int gridNumberBefore = director.State.globalGridNumber;
        int fuelMeterBefore = director.State.fuelMeter;
        director.RollContainerLoot(context, new Random(43));
        Assert.AreEqual(gridNumberBefore, director.State.globalGridNumber);
        Assert.AreEqual(fuelMeterBefore, director.State.fuelMeter);
    }

    [Test]
    public void ContainerRolls_UpdateSharedLootState()
    {
        List<LootDirector.Candidate> candidates = OnePerTier();
        foreach (LootDirector.Candidate candidate in candidates)
            candidate.Categories.Add(LootCategory.MeleeWeapon);
        LootDirector director = MakeDirector(candidates);
        director.State.rarePityOffset = 15f;
        // A guaranteed-Rare container roll must reset the pity like scatter
        LootDirector.Context context = MakeProfileContext("WeaponSafe", new[] { 0, 0, 0, 100, 0 });
        List<int> loot = director.RollContainerLoot(context, new Random(43));
        Assert.AreEqual(rareIndex, loot[0]);
        Assert.IsTrue(director.State.hasRarePlusSpawned);
        Assert.AreEqual(0f, director.State.rarePityOffset);
    }

    // --- Ts'urath tier ---

    private static LootDirector.Candidate MakeTsurathCandidate(int index, bool unique = true)
    {
        LootDirector.Candidate candidate = MakeCandidate(index, Rarity.Tsurath, unique: unique);
        return candidate;
    }

    [Test]
    public void TsurathItems_NeverEnterRandomGeneration()
    {
        const int tsurathIndex = 60;
        List<LootDirector.Candidate> candidates = OnePerTier();
        candidates.Add(MakeTsurathCandidate(tsurathIndex));
        LootDirector director = MakeDirector(candidates);
        LootDirector.Context context = MakeContext(new[] { 20, 20, 20, 20, 20 }, cap: 99);
        Random rng = new Random(47);
        for (int grid = 0; grid < 100; grid++)
        {
            foreach (int index in director.PlanGridLoot(context, rng))
                Assert.AreNotEqual(tsurathIndex, index, "a Ts'urath item leaked into random generation");
        }
    }

    [Test]
    public void RollTsurathDrop_ReturnsTsurathItem_AndRespectsUniqueness()
    {
        const int tsurathIndex = 60;
        List<LootDirector.Candidate> candidates = OnePerTier();
        candidates.Add(MakeTsurathCandidate(tsurathIndex));
        LootDirector director = MakeDirector(candidates);
        Random rng = new Random(53);
        Assert.AreEqual(tsurathIndex, director.RollTsurathDrop(rng));
        Assert.AreEqual(-1, director.RollTsurathDrop(rng), "a unique Ts'urath item dropped twice");
    }

    [Test]
    public void RollTsurathDrop_WithNoTsurathItems_ReturnsMinusOne()
    {
        LootDirector director = MakeDirector(OnePerTier());
        Assert.AreEqual(-1, director.RollTsurathDrop(new Random(53)));
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
