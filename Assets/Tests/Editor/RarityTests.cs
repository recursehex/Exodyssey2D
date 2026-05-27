using NUnit.Framework;
using UnityEngine;

public class RarityTests
{
    [Test]
    public void RarityList_ContainsFiveEntries()
    {
        Assert.AreEqual(5, Rarity.RarityList.Count);
    }

    [Test]
    public void DropRates_SumToOneHundred()
    {
        int total = 0;
        foreach (Rarity rarity in Rarity.RarityList)
            total += rarity.GetDropRate();
        Assert.AreEqual(100, total);
    }

    [Test]
    public void GetDropRate_ReturnsEnumValue()
    {
        Assert.AreEqual(45, Rarity.Common.GetDropRate());
        Assert.AreEqual(35, Rarity.Limited.GetDropRate());
        Assert.AreEqual(15, Rarity.Scarce.GetDropRate());
        Assert.AreEqual(4, Rarity.Rare.GetDropRate());
        Assert.AreEqual(1, Rarity.Anomalous.GetDropRate());
    }

    [Test]
    public void Parse_ValidStrings_ReturnsCorrectRarity()
    {
        Assert.AreEqual(Rarity.Common, Rarity.Parse("Common"));
        Assert.AreEqual(Rarity.Limited, Rarity.Parse("Limited"));
        Assert.AreEqual(Rarity.Scarce, Rarity.Parse("Scarce"));
        Assert.AreEqual(Rarity.Rare, Rarity.Parse("Rare"));
        Assert.AreEqual(Rarity.Anomalous, Rarity.Parse("Anomalous"));
    }

    [Test]
    public void Parse_UnknownString_DefaultsToCommon()
    {
        Assert.AreEqual(Rarity.Common, Rarity.Parse("Nonexistent"));
    }

    [Test]
    public void Parse_EmptyString_DefaultsToCommon()
    {
        Assert.AreEqual(Rarity.Common, Rarity.Parse(""));
    }

    [Test]
    public void Equality_DifferentRarity_ReturnsFalse()
    {
        Assert.IsTrue(Rarity.Common != Rarity.Rare);
        Assert.IsFalse(Rarity.Limited == Rarity.Scarce);
    }

    [Test]
    public void Equals_WithObject_WorksCorrectly()
    {
        Rarity a = Rarity.Rare;
        Rarity b = Rarity.Rare;
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsFalse(a.Equals((object)Rarity.Common));
        Assert.IsFalse(a.Equals("not a rarity"));
    }

    [Test]
    public void GetHashCode_SameRarity_SameHash()
    {
        Assert.AreEqual(Rarity.Scarce.GetHashCode(), Rarity.Scarce.GetHashCode());
    }

    [Test]
    public void Each_Rarity_HasDistinctColor()
    {
        for (int i = 0; i < Rarity.RarityList.Count; i++)
        {
            for (int j = i + 1; j < Rarity.RarityList.Count; j++)
            {
                Assert.AreNotEqual(Rarity.RarityList[i].Color, Rarity.RarityList[j].Color,
                    $"{Rarity.RarityList[i].Tag} and {Rarity.RarityList[j].Tag} should have different colors");
            }
        }
    }

    [Test]
    public void DropRates_AreInDescendingOrder()
    {
        // Common > Limited > Scarce > Rare > Anomalous
        Assert.Greater(Rarity.Common.GetDropRate(), Rarity.Limited.GetDropRate());
        Assert.Greater(Rarity.Limited.GetDropRate(), Rarity.Scarce.GetDropRate());
        Assert.Greater(Rarity.Scarce.GetDropRate(), Rarity.Rare.GetDropRate());
        Assert.Greater(Rarity.Rare.GetDropRate(), Rarity.Anomalous.GetDropRate());
    }
}
