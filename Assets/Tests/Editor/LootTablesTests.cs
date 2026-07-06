using NUnit.Framework;

/// <summary>
/// Tests the loot data layer: tuning constants, archetype profiles, and the
/// run-scoped LootState bag
/// </summary>
public class LootTablesTests
{
    // --- Tuning ---

    [Test]
    public void Tuning_LoadsFromJson()
    {
        LootTuning tuning = LootProfileInfo.Tuning;
        Assert.IsNotNull(tuning);
        Assert.Less(tuning.rarePityStart, 0f, "Pity should start suppressed");
        Assert.Greater(tuning.rarePityPerItem, 0f);
        Assert.Greater(tuning.rarePityCap, 0f);
        Assert.Greater(tuning.historySize, 0);
    }

    [Test]
    public void Tuning_BackstopIsWithinRegionLength()
    {
        LootTuning tuning = LootProfileInfo.Tuning;
        RegionInfo backstopRegion = new RegionInfo((int)tuning.GetBackstopRegionTag());
        Assert.GreaterOrEqual(tuning.backstopGrid, 1);
        Assert.LessOrEqual(tuning.backstopGrid, backstopRegion.GridsRequired,
            "Backstop grid must be reachable before the region auto-advances");
    }

    [Test]
    public void Tuning_MultipliersAreSuppressive()
    {
        LootTuning tuning = LootProfileInfo.Tuning;
        Assert.That(tuning.historyMultiplierScarce, Is.InRange(0f, 1f));
        Assert.That(tuning.historyMultiplierRarePlus, Is.InRange(0f, 1f));
        Assert.That(tuning.withinGridMultiplierCommon, Is.InRange(0f, 1f));
        Assert.That(tuning.withinGridMultiplierLimited, Is.InRange(0f, 1f));
        Assert.That(tuning.oodEscalationChance, Is.InRange(0f, 0.25f));
    }

    [Test]
    public void Tuning_FuelMeterValuesAreSane()
    {
        LootTuning tuning = LootProfileInfo.Tuning;
        Assert.Greater(tuning.fuelMeterGainPerDryGrid, 0);
        Assert.Greater(tuning.fuelMeterCostOnSpawn, 0);
        Assert.LessOrEqual(tuning.fuelMeterFloor, 0);
        Assert.Greater(tuning.fuelSurvivalFloorGrids, 0);
    }

    // --- Profiles ---

    [Test]
    public void DefaultProfile_Exists()
    {
        LootProfileInfo profile = LootProfileInfo.GetProfile(LootProfileInfo.DefaultProfileName);
        Assert.IsNotNull(profile);
        Assert.AreEqual(LootProfileInfo.DefaultProfileName, profile.Name);
    }

    [Test]
    public void UnknownProfile_FallsBackToDefault()
    {
        LootProfileInfo profile = LootProfileInfo.GetProfile("NotARealArchetype");
        Assert.IsNotNull(profile);
        Assert.AreEqual(LootProfileInfo.DefaultProfileName, profile.Name);
    }

    [Test]
    public void AllProfiles_HaveValidCounts()
    {
        foreach (LootProfileInfo profile in LootProfileInfo.AllProfiles)
        {
            Assert.GreaterOrEqual(profile.MinItems, 0, $"Profile {profile.Name}");
            Assert.GreaterOrEqual(profile.MaxItems, profile.MinItems, $"Profile {profile.Name}");
            Assert.That(profile.RarityShift, Is.InRange(-1, 1), $"Profile {profile.Name}");
        }
    }

    [Test]
    public void KnownArchetypes_Exist()
    {
        foreach (string name in new[] { "TriageCorridor", "CacheCrucible", "ObeliskClearing", "MemorySite", "FuelLineYard", "AlienNest", "BossFinality" })
        {
            LootProfileInfo profile = LootProfileInfo.GetProfile(name);
            Assert.AreEqual(name, profile.Name, $"archetype {name} should be authored in LootTables.json");
        }
    }

    [Test]
    public void FuelArchetypes_GuaranteeFuel()
    {
        foreach (string name in new[] { "FuelLineYard", "GarageYard", "MaintenanceYard", "LaunchpadDebris" })
        {
            LootProfileInfo profile = LootProfileInfo.GetProfile(name);
            bool hasFuelSlot = profile.Guaranteed.Exists(slot => slot.Category == LootCategory.Fuel);
            Assert.IsTrue(hasFuelSlot, $"fuel archetype {name} must have a guaranteed Fuel slot");
        }
    }

    [Test]
    public void RarityShiftedArchetypes_MatchTheDesign()
    {
        Assert.AreEqual(1, LootProfileInfo.GetProfile("ObeliskClearing").RarityShift);
        Assert.AreEqual(1, LootProfileInfo.GetProfile("CacheCrucible").RarityShift);
        Assert.AreEqual(-1, LootProfileInfo.GetProfile("MemorySite").RarityShift);
    }

    [Test]
    public void GetMultiplierFor_UndeclaredCategory_Returns1()
    {
        LootProfileInfo profile = LootProfileInfo.GetProfile(LootProfileInfo.DefaultProfileName);
        float multiplier = profile.GetMultiplierFor(new[] { LootCategory.Medical });
        Assert.AreEqual(1f, multiplier);
    }

    // --- Ts'urath tier ---

    [Test]
    public void TsurathRarity_HasZeroDropRate_AndStaysOutOfTheWeightedPool()
    {
        Assert.AreEqual(0, Rarity.Tsurath.GetDropRate());
        Assert.IsFalse(Rarity.RarityList.Contains(Rarity.Tsurath),
            "Ts'urath must never be part of the weighted rarity pool");
        Assert.AreEqual(Rarity.Tsurath, Rarity.Parse("Tsurath"));
        Assert.AreEqual(Rarity.Tsurath, Rarity.Parse("Ts'urath"));
    }

    [Test]
    public void ContainerProfiles_Exist()
    {
        foreach (string name in new[] { "ReserveCrate", "RocketWreck", "WeaponSafe", "CarrierWreck" })
        {
            LootProfileInfo profile = LootProfileInfo.GetProfile(name);
            Assert.AreEqual(name, profile.Name, $"container source {name} should be authored in LootTables.json");
            Assert.AreEqual("Container", profile.GridType);
        }
    }

    // --- LootState ---

    [Test]
    public void PushHistory_CapsAtHistorySize()
    {
        LootState state = new LootState();
        for (int i = 0; i < 15; i++)
            state.PushHistory(i, 10);
        Assert.AreEqual(10, state.RecentItemHistory.Count);
        Assert.AreEqual(5, state.RecentItemHistory[0], "Oldest entries should be evicted first");
    }

    [Test]
    public void ResetForNewRun_ClearsEverything()
    {
        LootState state = new LootState();
        state.rarePityOffset = 12f;
        state.hasRarePlusSpawned = true;
        state.anomalousSpawnedThisRegion = 2;
        state.gridsSinceAnomalous = 1;
        state.fuelMeter = 70;
        state.globalGridNumber = 30;
        state.PushHistory(3, 10);
        state.UniqueItemsSpawned.Add(5);
        state.ResetForNewRun(42, -2f);
        Assert.AreEqual(-2f, state.rarePityOffset);
        Assert.IsFalse(state.hasRarePlusSpawned);
        Assert.AreEqual(0, state.anomalousSpawnedThisRegion);
        Assert.AreEqual(LootState.neverSpawned, state.gridsSinceAnomalous);
        Assert.AreEqual(0, state.fuelMeter);
        Assert.AreEqual(0, state.globalGridNumber);
        Assert.AreEqual(42, state.runSeed);
        Assert.AreEqual(0, state.RecentItemHistory.Count);
        Assert.AreEqual(0, state.UniqueItemsSpawned.Count);
    }

    [Test]
    public void ResetForNewRegion_OnlyResetsRegionCounters()
    {
        LootState state = new LootState();
        state.rarePityOffset = 5f;
        state.anomalousSpawnedThisRegion = 2;
        state.fuelSpawnedThisRegion = 3;
        state.gridsSinceAnomalous = 1;
        state.ResetForNewRegion();
        Assert.AreEqual(0, state.anomalousSpawnedThisRegion);
        Assert.AreEqual(0, state.fuelSpawnedThisRegion);
        Assert.AreEqual(5f, state.rarePityOffset, "Pity should persist across regions");
        Assert.AreEqual(1, state.gridsSinceAnomalous, "Anti-chain window should persist across regions");
    }
}
