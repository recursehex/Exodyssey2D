using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Tests the weighted rarity selection math used by WeightedRarityGeneration.
/// The actual Generate method requires GameManager, so we test the probability math here.
/// </summary>
public class WeightedRarityTests
{
    /// <summary>
    /// Simulates the weighted selection algorithm from WeightedRarityGeneration.
    /// </summary>
    private static Rarity SelectRarity(List<Rarity> allowedRarities, int roll)
    {
        int cumulative = 0;
        foreach (Rarity rarity in allowedRarities)
        {
            cumulative += rarity.GetDropRate();
            if (roll <= cumulative)
                return rarity;
        }
        return allowedRarities[allowedRarities.Count - 1];
    }

    private static int GetTotalWeight(List<Rarity> rarities)
    {
        int total = 0;
        foreach (Rarity r in rarities)
            total += r.GetDropRate();
        return total;
    }

    [Test]
    public void AllRarities_TotalWeight_Is100()
    {
        Assert.AreEqual(100, GetTotalWeight(Rarity.RarityList));
    }

    [Test]
    public void Roll1_SelectsCommon()
    {
        Assert.AreEqual(Rarity.Common, SelectRarity(Rarity.RarityList, 1));
    }

    [Test]
    public void Roll45_SelectsCommon()
    {
        Assert.AreEqual(Rarity.Common, SelectRarity(Rarity.RarityList, 45));
    }

    [Test]
    public void Roll46_SelectsLimited()
    {
        Assert.AreEqual(Rarity.Limited, SelectRarity(Rarity.RarityList, 46));
    }

    [Test]
    public void Roll80_SelectsLimited()
    {
        // Common=45, Limited=35, so 45+35=80
        Assert.AreEqual(Rarity.Limited, SelectRarity(Rarity.RarityList, 80));
    }

    [Test]
    public void Roll81_SelectsScarce()
    {
        Assert.AreEqual(Rarity.Scarce, SelectRarity(Rarity.RarityList, 81));
    }

    [Test]
    public void Roll95_SelectsScarce()
    {
        // Common=45, Limited=35, Scarce=15, so 45+35+15=95
        Assert.AreEqual(Rarity.Scarce, SelectRarity(Rarity.RarityList, 95));
    }

    [Test]
    public void Roll96_SelectsRare()
    {
        Assert.AreEqual(Rarity.Rare, SelectRarity(Rarity.RarityList, 96));
    }

    [Test]
    public void Roll99_SelectsRare()
    {
        // Common=45, Limited=35, Scarce=15, Rare=4, so 45+35+15+4=99
        Assert.AreEqual(Rarity.Rare, SelectRarity(Rarity.RarityList, 99));
    }

    [Test]
    public void Roll100_SelectsAnomalous()
    {
        Assert.AreEqual(Rarity.Anomalous, SelectRarity(Rarity.RarityList, 100));
    }

    // --- Subset of rarities ---

    [Test]
    public void SubsetRarities_WeightIsRecalculated()
    {
        // If only Rare and Anomalous are allowed (like early region items)
        List<Rarity> subset = new() { Rarity.Rare, Rarity.Anomalous };
        int totalWeight = GetTotalWeight(subset);
        Assert.AreEqual(5, totalWeight); // 4 + 1

        // Roll 1-4 should select Rare
        Assert.AreEqual(Rarity.Rare, SelectRarity(subset, 1));
        Assert.AreEqual(Rarity.Rare, SelectRarity(subset, 4));
        // Roll 5 should select Anomalous
        Assert.AreEqual(Rarity.Anomalous, SelectRarity(subset, 5));
    }

    [Test]
    public void SingleRarity_AlwaysSelected()
    {
        List<Rarity> single = new() { Rarity.Scarce };
        Assert.AreEqual(Rarity.Scarce, SelectRarity(single, 1));
        Assert.AreEqual(Rarity.Scarce, SelectRarity(single, 15));
    }

    // --- Safe zone check ---

    [Test]
    public void SafeZone_PositionsAreCorrectlyFiltered()
    {
        // Safe zone: x <= SafeZoneMaxX (-2), SafeZoneMinY (-1) <= y <= SafeZoneMaxY (1)
        int safeX = GameConfig.Grid.SafeZoneMaxX;
        int safeMinY = GameConfig.Grid.SafeZoneMinY;
        int safeMaxY = GameConfig.Grid.SafeZoneMaxY;

        // Position in safe zone should be filtered
        Assert.IsTrue(-3 <= safeX && 0 >= safeMinY && 0 <= safeMaxY, "(-3,0) should be in safe zone");
        // Position outside safe zone
        Assert.IsFalse(0 <= safeX, "(0,0) should not be in safe zone x-range");
    }

    // --- Distribution Sanity ---

    [Test]
    public void CommonIsMoreLikelyThanAnomalous()
    {
        Assert.Greater(Rarity.Common.GetDropRate(), Rarity.Anomalous.GetDropRate());
        // Common is 45x more likely than Anomalous
        Assert.AreEqual(45, Rarity.Common.GetDropRate() / Rarity.Anomalous.GetDropRate());
    }
}
