using NUnit.Framework;

public class RegionInfoTests
{
    // --- Construction ---

    [Test]
    public void Constructor_RuinedOutpost_HasCorrectTag()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.AreEqual(RegionInfo.Tags.RuinedOutpost, region.Tag);
    }

    [Test]
    public void Constructor_AllRegions_HaveValidData()
    {
        for (int i = 0; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.AreNotEqual(RegionInfo.Tags.Unknown, region.Tag, $"Region {i} should have a valid tag");
            Assert.Greater(region.GridsRequired, 0, $"Region {region.Tag} should require at least 1 grid");
            Assert.IsNotNull(region.Name, $"Region {region.Tag} should have a name");
        }
    }

    // --- Progression ---

    [Test]
    public void NewRegion_StartsWithZeroGridsCompleted()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.AreEqual(0, region.GridsCompleted);
    }

    [Test]
    public void IncrementProgress_IncreasesGridsCompleted()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        region.IncrementProgress();
        Assert.AreEqual(1, region.GridsCompleted);
    }

    [Test]
    public void IsComplete_WhenNotEnoughGrids_ReturnsFalse()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.IsFalse(region.IsComplete());
    }

    [Test]
    public void IsComplete_WhenEnoughGrids_ReturnsTrue()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        for (int i = 0; i < region.GridsRequired; i++)
            region.IncrementProgress();
        Assert.IsTrue(region.IsComplete());
    }

    [Test]
    public void IsComplete_WhenExceeded_StillTrue()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        for (int i = 0; i < region.GridsRequired + 5; i++)
            region.IncrementProgress();
        Assert.IsTrue(region.IsComplete());
    }

    [Test]
    public void ResetProgress_ResetsToZero()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        region.IncrementProgress();
        region.IncrementProgress();
        region.ResetProgress();
        Assert.AreEqual(0, region.GridsCompleted);
        Assert.IsFalse(region.IsComplete());
    }

    // --- Pool Checks ---

    [Test]
    public void RuinedOutpost_AllowsWeakEnemies()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.IsTrue(region.IsEnemyAllowed("Weak"));
    }

    [Test]
    public void RuinedOutpost_DoesNotAllowStrongEnemies()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.IsFalse(region.IsEnemyAllowed("Strong"));
    }

    [Test]
    public void RadiantCascades_AllowsExoticEnemies()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RadiantCascades);
        Assert.IsTrue(region.IsEnemyAllowed("Exotic"));
    }

    [Test]
    public void IsVehicleAllowed_InvalidTag_ReturnsFalse()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.IsFalse(region.IsVehicleAllowed("Nonexistent"));
    }

    // --- Difficulty Scaling ---

    [Test]
    public void LaterRegions_RequireMoreGrids()
    {
        RegionInfo first = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        RegionInfo last = new RegionInfo((int)RegionInfo.Tags.RadiantCascades);
        Assert.GreaterOrEqual(last.GridsRequired, first.GridsRequired);
    }

    [Test]
    public void LaterRegions_HaveLargerEnemyPools()
    {
        RegionInfo first = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        RegionInfo mid = new RegionInfo((int)RegionInfo.Tags.ScorchedPlateau);
        Assert.GreaterOrEqual(mid.EnemyPool.Count, first.EnemyPool.Count);
    }

    // --- All Regions Have Pools ---

    [Test]
    public void AllRegions_HaveNonEmptyEnemyPools()
    {
        for (int i = 0; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.Greater(region.EnemyPool.Count, 0, $"Region {region.Tag} should have enemy pool entries");
        }
    }

    [Test]
    public void AllRegions_HaveNonEmptyVehiclePools()
    {
        for (int i = 0; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.Greater(region.VehiclePool.Count, 0, $"Region {region.Tag} should have vehicle pool entries");
        }
    }

    // --- Item Rarity Weight Tables ---

    private const int anomalousIndex = 4;
    private const int rareIndex = 3;

    [Test]
    public void AllRegions_ItemRarityWeights_SumTo100()
    {
        for (int i = 0; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            int startSum = 0, endSum = 0;
            for (int j = 0; j < 5; j++)
            {
                startSum += region.ItemRarityWeightsStart[j];
                endSum += region.ItemRarityWeightsEnd[j];
            }
            Assert.AreEqual(100, startSum, $"Region {region.Tag} start weights should sum to 100");
            Assert.AreEqual(100, endSum, $"Region {region.Tag} end weights should sum to 100");
        }
    }

    [Test]
    public void RegionsBeforeScorchedPlateau_HaveZeroAnomalousWeight()
    {
        for (int i = 0; i < (int)RegionInfo.Tags.ScorchedPlateau; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.AreEqual(0, region.ItemRarityWeightsStart[anomalousIndex], $"Region {region.Tag} should gate Anomalous at start");
            Assert.AreEqual(0, region.ItemRarityWeightsEnd[anomalousIndex], $"Region {region.Tag} should gate Anomalous at end");
            Assert.AreEqual(0, region.AnomalousCap, $"Region {region.Tag} should have a zero Anomalous cap");
        }
    }

    [Test]
    public void RegionsFromScorchedPlateau_HaveAnomalousWeightAndCap()
    {
        for (int i = (int)RegionInfo.Tags.ScorchedPlateau; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.Greater(region.ItemRarityWeightsStart[anomalousIndex], 0, $"Region {region.Tag} should allow Anomalous");
            Assert.Greater(region.AnomalousCap, 0, $"Region {region.Tag} should have a positive Anomalous cap");
        }
    }

    [Test]
    public void RuinedOutpost_HasFlatWeights_AndNoRare()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        for (int j = 0; j < 5; j++)
            Assert.AreEqual(region.ItemRarityWeightsStart[j], region.ItemRarityWeightsEnd[j], "Ruined Outpost weights should be flat");
        Assert.AreEqual(0, region.ItemRarityWeightsStart[rareIndex]);
    }

    [Test]
    public void GetItemRarityWeightsAt_Interpolates_AndClamps()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.FragmentedCoast);
        float[] atStart = region.GetItemRarityWeightsAt(0f);
        float[] atMid = region.GetItemRarityWeightsAt(0.5f);
        float[] atEnd = region.GetItemRarityWeightsAt(1f);
        float[] pastEnd = region.GetItemRarityWeightsAt(2f);
        Assert.AreEqual(region.ItemRarityWeightsStart[rareIndex], atStart[rareIndex], 0.001f);
        Assert.AreEqual((region.ItemRarityWeightsStart[rareIndex] + region.ItemRarityWeightsEnd[rareIndex]) / 2f, atMid[rareIndex], 0.001f);
        Assert.AreEqual(region.ItemRarityWeightsEnd[rareIndex], atEnd[rareIndex], 0.001f);
        Assert.AreEqual(atEnd[rareIndex], pastEnd[rareIndex], 0.001f, "t past 1 should clamp to end weights");
    }

    // --- Item Pool ---

    [Test]
    public void RuinedOutpost_RestrictsLootToItsTutorialPool()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        ItemInfo.Tags[] allowed =
        {
            ItemInfo.Tags.Branch,
            ItemInfo.Tags.Rock,
            ItemInfo.Tags.Knife,
            ItemInfo.Tags.MedKit,
            ItemInfo.Tags.Flare,
            ItemInfo.Tags.PowerCell,
        };
        Assert.AreEqual(allowed.Length, region.AllowedItemIndices.Count);
        foreach (ItemInfo.Tags tag in allowed)
            Assert.IsTrue(region.IsItemAllowed(tag), $"{tag} should be in the Ruined Outpost pool");
        Assert.IsFalse(region.IsItemAllowed(ItemInfo.Tags.ToolKit));
        Assert.IsFalse(region.IsItemAllowed(ItemInfo.Tags.Lightrod));
        Assert.IsFalse(region.IsItemAllowed(ItemInfo.Tags.Chainsaw));
    }

    [Test]
    public void RegionsWithoutAnItemPool_AllowEveryItem()
    {
        for (int i = (int)RegionInfo.Tags.FragmentedCoast; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.IsNull(region.AllowedItemIndices, $"Region {region.Tag} should not restrict its item pool");
            Assert.IsTrue(region.IsItemAllowed(ItemInfo.Tags.ToolKit));
        }
    }

    [Test]
    public void RareWeight_NeverDecreasesAcrossRegions()
    {
        int previousEnd = 0;
        for (int i = 0; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.GreaterOrEqual(region.ItemRarityWeightsStart[rareIndex], previousEnd, $"Region {region.Tag} should not drop Rare below the previous region");
            previousEnd = region.ItemRarityWeightsEnd[rareIndex];
        }
    }
}
