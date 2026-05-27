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
    public void RuinedOutpost_AllowsCommonItems()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.IsTrue(region.IsItemAllowed("Common"));
    }

    [Test]
    public void RuinedOutpost_AllowsLimitedItems()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.IsTrue(region.IsItemAllowed("Limited"));
    }

    [Test]
    public void RuinedOutpost_DoesNotAllowRareItems()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RuinedOutpost);
        Assert.IsFalse(region.IsItemAllowed("Rare"));
    }

    [Test]
    public void RadiantCascades_AllowsAllItemRarities()
    {
        RegionInfo region = new RegionInfo((int)RegionInfo.Tags.RadiantCascades);
        Assert.IsTrue(region.IsItemAllowed("Common"));
        Assert.IsTrue(region.IsItemAllowed("Limited"));
        Assert.IsTrue(region.IsItemAllowed("Scarce"));
        Assert.IsTrue(region.IsItemAllowed("Rare"));
        Assert.IsTrue(region.IsItemAllowed("Anomalous"));
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
    public void AllRegions_HaveNonEmptyItemPools()
    {
        for (int i = 0; i < (int)RegionInfo.Tags.Unknown; i++)
        {
            RegionInfo region = new RegionInfo(i);
            Assert.Greater(region.ItemPool.Count, 0, $"Region {region.Tag} should have item pool entries");
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
}
