using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

/// <summary>
/// Monte Carlo simulation of the loot director over the full run shape
/// (every region at its required grid count) against the real item and
/// region databases. Asserts the distribution bands the design promises
/// and dumps per-region tier histograms to the console for tuning passes
/// </summary>
public class LootSimulationTests
{
    private const int seedCount = 300;

    private class RunStats
    {
        public List<int> AnomalousGlobalGrids = new();
        public int[] AnomalousPerRegion = new int[(int)RegionInfo.Tags.Unknown];
        public int[] FuelPerRegion = new int[(int)RegionInfo.Tags.Unknown];
        public int firstRarePlusGlobalGrid = -1;
        public int scarcePlusRepeatGrids;
        public int limitedRepeatGrids;
        public int totalGrids;
        public int totalFuel;
        public int medKits;
        public int longestFuelDrought;
        public List<int> PreScorchedPlateauAnomalous = new();
        public List<int> EarlyPlasmaRailguns = new();
    }

    private static readonly List<LootDirector.Candidate> Candidates = LootManager.BuildCandidates();

    private static LootDirector.Candidate CandidateOf(int index) =>
        Candidates.Find(candidate => candidate.index == index);

    private static RunStats SimulateRun(int seed, long[,] tierCountsPerRegion)
    {
        LootTuning tuning = LootProfileInfo.Tuning;
        LootDirector director = new LootDirector(Candidates, tuning);
        director.State.ResetForNewRun(seed, tuning.rarePityStart);
        LootProfileInfo defaultProfile = LootProfileInfo.GetProfile(LootProfileInfo.DefaultProfileName);
        RunStats stats = new RunStats();
        int globalGrid = 0;
        int fuelDrought = 0;
        for (int regionIndex = 0; regionIndex < (int)RegionInfo.Tags.Unknown; regionIndex++)
        {
            RegionInfo region = new RegionInfo(regionIndex);
            for (int grid = 0; grid < region.GridsRequired; grid++)
            {
                globalGrid++;
                stats.totalGrids++;
                LootDirector.Context context = new LootDirector.Context
                {
                    regionIndex = regionIndex,
                    gridsCompleted = grid,
                    gridsRequired = region.GridsRequired,
                    RarityWeightsStart = region.ItemRarityWeightsStart,
                    RarityWeightsEnd = region.ItemRarityWeightsEnd,
                    anomalousCap = region.AnomalousCap,
                    Profile = defaultProfile,
                };
                Random rng = new Random(seed ^ director.State.globalGridNumber);
                List<int> plan = director.PlanGridLoot(context, rng);
                Assert.GreaterOrEqual(plan.Count, defaultProfile.MinItems, $"seed {seed} grid {globalGrid}");
                Assert.LessOrEqual(plan.Count, defaultProfile.MaxItems, $"seed {seed} grid {globalGrid}");
                bool fuelThisGrid = false;
                Dictionary<int, int> countsByIndex = new();
                foreach (int index in plan)
                {
                    LootDirector.Candidate candidate = CandidateOf(index);
                    countsByIndex.TryGetValue(index, out int already);
                    countsByIndex[index] = already + 1;
                    // Fuel is metered rather than ramped, so it is tracked
                    // separately and never counts toward the rarity bands
                    if (candidate.Categories.Contains(LootCategory.Fuel))
                    {
                        fuelThisGrid = true;
                        stats.totalFuel++;
                        stats.FuelPerRegion[regionIndex]++;
                        continue;
                    }
                    int tier = LootDirector.TierIndexOf(candidate.Rarity);
                    tierCountsPerRegion[regionIndex, tier]++;
                    if (tier >= LootDirector.rareTier && stats.firstRarePlusGlobalGrid < 0)
                        stats.firstRarePlusGlobalGrid = globalGrid;
                    if (tier == LootDirector.anomalousTier)
                    {
                        stats.AnomalousGlobalGrids.Add(globalGrid);
                        stats.AnomalousPerRegion[regionIndex]++;
                        if (regionIndex < (int)RegionInfo.Tags.ScorchedPlateau)
                            stats.PreScorchedPlateauAnomalous.Add(globalGrid);
                    }
                    if (index == (int)ItemInfo.Tags.MedKit)
                        stats.medKits++;
                    if (index == (int)ItemInfo.Tags.PlasmaRailgun && regionIndex < (int)RegionInfo.Tags.RainforestRavines)
                        stats.EarlyPlasmaRailguns.Add(globalGrid);
                }
                foreach (KeyValuePair<int, int> entry in countsByIndex)
                {
                    if (entry.Value < 2)
                        continue;
                    int tier = LootDirector.TierIndexOf(CandidateOf(entry.Key).Rarity);
                    if (tier >= LootDirector.scarceTier)
                        stats.scarcePlusRepeatGrids++;
                    else if (tier == LootDirector.limitedTier)
                        stats.limitedRepeatGrids++;
                }
                if (fuelThisGrid)
                    fuelDrought = 0;
                else
                    fuelDrought++;
                stats.longestFuelDrought = Math.Max(stats.longestFuelDrought, fuelDrought);
            }
            director.State.ResetForNewRegion();
        }
        return stats;
    }

    [Test]
    public void FullRunSimulation_MatchesTheDesignBands()
    {
        LootTuning tuning = LootProfileInfo.Tuning;
        long[,] tierCounts = new long[(int)RegionInfo.Tags.Unknown, 5];
        long totalAnomalous = 0, totalFuel = 0, totalMedKits = 0, totalLimitedRepeats = 0, totalGrids = 0;
        int backstopGlobalGrid = 3 + tuning.backstopGrid; // Ruined Outpost grids + backstop grid in Fragmented Coast
        foreach (int caseSeed in SeedRange())
        {
            RunStats stats = SimulateRun(caseSeed, tierCounts);
            // Anomalous is structurally impossible before Scorched Plateau
            Assert.IsEmpty(stats.PreScorchedPlateauAnomalous, $"seed {caseSeed}: Anomalous before Scorched Plateau");
            // Per-region caps hold
            for (int regionIndex = 0; regionIndex < (int)RegionInfo.Tags.Unknown; regionIndex++)
                Assert.LessOrEqual(stats.AnomalousPerRegion[regionIndex], new RegionInfo(regionIndex).AnomalousCap,
                    $"seed {caseSeed}: Anomalous cap exceeded in region {regionIndex}");
            // Anti-chain spacing: at least 2 fully blocked grids between drops
            for (int i = 1; i < stats.AnomalousGlobalGrids.Count; i++)
                Assert.GreaterOrEqual(stats.AnomalousGlobalGrids[i] - stats.AnomalousGlobalGrids[i - 1], 3,
                    $"seed {caseSeed}: Anomalous drops chained");
            // The first exciting drop lands in Fragmented Coast, never in the
            // tutorial region and never later than the backstop grid
            Assert.GreaterOrEqual(stats.firstRarePlusGlobalGrid, 4, $"seed {caseSeed}: Rare+ in Ruined Outpost");
            Assert.LessOrEqual(stats.firstRarePlusGlobalGrid, backstopGlobalGrid,
                $"seed {caseSeed}: first Rare+ arrived after the backstop grid");
            // Duplicate suppression: a Scarce+ item never repeats within a grid
            Assert.AreEqual(0, stats.scarcePlusRepeatGrids, $"seed {caseSeed}: Scarce+ duplicate within a grid");
            totalLimitedRepeats += stats.limitedRepeatGrids;
            totalGrids += stats.totalGrids;
            // Fuel: droughts hard-capped, every region meets its budget
            Assert.LessOrEqual(stats.longestFuelDrought, 3, $"seed {caseSeed}: fuel drought exceeded 3 grids");
            for (int regionIndex = 0; regionIndex < (int)RegionInfo.Tags.Unknown; regionIndex++)
                Assert.GreaterOrEqual(stats.FuelPerRegion[regionIndex], tuning.minFuelItemsPerRegion,
                    $"seed {caseSeed}: region {regionIndex} below its fuel budget");
            // Plasma Railgun respects its per-item region gate
            Assert.IsEmpty(stats.EarlyPlasmaRailguns, $"seed {caseSeed}: Plasma Railgun before Rainforest Ravines");
            totalAnomalous += stats.AnomalousGlobalGrids.Count;
            totalFuel += stats.totalFuel;
            totalMedKits += stats.medKits;
        }
        double avgAnomalous = (double)totalAnomalous / seedCount;
        double avgFuel = (double)totalFuel / seedCount;
        double avgMedKits = (double)totalMedKits / seedCount;
        double limitedRepeatRate = (double)totalLimitedRepeats / totalGrids;
        DumpHistogram(tierCounts, avgAnomalous, avgFuel, avgMedKits, limitedRepeatRate);
        // Averages per full 50-grid run stay in sane bands
        Assert.That(avgAnomalous, Is.InRange(1.5, 8.0), "average Anomalous per run drifted out of band");
        Assert.That(avgFuel, Is.InRange(12.0, 30.0), "average fuel per run drifted out of band");
        Assert.That(avgMedKits, Is.InRange(4.0, 40.0), "average MedKits per run drifted out of band");
        // Only 3 Limited items are enabled today, so a grid that rolls the
        // Limited tier 4+ times must repeat one; this bound should tighten
        // as more Limited items are enabled
        Assert.Less(limitedRepeatRate, 0.08, "Limited items repeat within a grid too often");
    }

    private static IEnumerable<int> SeedRange()
    {
        for (int seed = 1; seed <= seedCount; seed++)
            yield return seed * 7919; // spread seeds instead of 1..N
    }

    private static void DumpHistogram(long[,] tierCounts, double avgAnomalous, double avgFuel, double avgMedKits, double limitedRepeatRate)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Loot simulation over {seedCount} runs (values are items per single run):");
        builder.AppendLine("Region                 Common Limited Scarce  Rare  Anomalous");
        for (int regionIndex = 0; regionIndex < (int)RegionInfo.Tags.Unknown; regionIndex++)
        {
            builder.Append($"{(RegionInfo.Tags)regionIndex,-22}");
            for (int tier = 0; tier < 5; tier++)
                builder.Append($"{(double)tierCounts[regionIndex, tier] / seedCount,7:F2}");
            builder.AppendLine();
        }
        builder.AppendLine($"Avg Anomalous/run: {avgAnomalous:F2}   Avg fuel/run: {avgFuel:F2}   Avg MedKits/run: {avgMedKits:F2}");
        builder.AppendLine($"Limited within-grid repeat rate: {limitedRepeatRate:P2}");
        UnityEngine.Debug.Log(builder.ToString());
    }
}
