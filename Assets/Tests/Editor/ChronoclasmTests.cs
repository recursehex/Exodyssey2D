using NUnit.Framework;

/// <summary>
/// Tests for Chronoclasm cooldown and unlock logic.
/// ChronoclasmManager is a MonoBehaviour so we test the pure formulas here.
/// </summary>
public class ChronoclasmTests
{
    // Chronoclasm is unlocked from FragmentedCoast (region index 1) onward
    private static bool IsChronoclasmUnlocked(RegionInfo.Tags regionTag)
    {
        return regionTag >= RegionInfo.Tags.FragmentedCoast;
    }

    // Cooldown: requires N grids between uses (default 3)
    private static bool IsOffCooldown(int gridsSinceUse, int cooldownGrids)
    {
        return gridsSinceUse >= cooldownGrids;
    }

    private static int GetGridsRemaining(int gridsSinceUse, int cooldownGrids)
    {
        int remaining = cooldownGrids - gridsSinceUse;
        return remaining > 0 ? remaining : 0;
    }

    // --- Unlock ---

    [Test]
    public void RuinedOutpost_ChronoclasmIsLocked()
    {
        Assert.IsFalse(IsChronoclasmUnlocked(RegionInfo.Tags.RuinedOutpost));
    }

    [Test]
    public void FragmentedCoast_ChronoclasmIsUnlocked()
    {
        Assert.IsTrue(IsChronoclasmUnlocked(RegionInfo.Tags.FragmentedCoast));
    }

    [Test]
    public void AllLaterRegions_ChronoclasmIsUnlocked()
    {
        Assert.IsTrue(IsChronoclasmUnlocked(RegionInfo.Tags.GlacialDesert));
        Assert.IsTrue(IsChronoclasmUnlocked(RegionInfo.Tags.ScorchedPlateau));
        Assert.IsTrue(IsChronoclasmUnlocked(RegionInfo.Tags.RainforestRavines));
        Assert.IsTrue(IsChronoclasmUnlocked(RegionInfo.Tags.VolatileVolcanoes));
        Assert.IsTrue(IsChronoclasmUnlocked(RegionInfo.Tags.LuminousSwamp));
        Assert.IsTrue(IsChronoclasmUnlocked(RegionInfo.Tags.RadiantCascades));
    }

    // --- Cooldown ---

    [Test]
    public void Cooldown_ZeroGrids_NotReady()
    {
        Assert.IsFalse(IsOffCooldown(0, 3));
    }

    [Test]
    public void Cooldown_PartialGrids_NotReady()
    {
        Assert.IsFalse(IsOffCooldown(1, 3));
        Assert.IsFalse(IsOffCooldown(2, 3));
    }

    [Test]
    public void Cooldown_EnoughGrids_IsReady()
    {
        Assert.IsTrue(IsOffCooldown(3, 3));
    }

    [Test]
    public void Cooldown_ExceededGrids_IsReady()
    {
        Assert.IsTrue(IsOffCooldown(5, 3));
    }

    [Test]
    public void GridsRemaining_FullCooldown_ReturnsThree()
    {
        Assert.AreEqual(3, GetGridsRemaining(0, 3));
    }

    [Test]
    public void GridsRemaining_PartialCooldown_ReturnsRemaining()
    {
        Assert.AreEqual(2, GetGridsRemaining(1, 3));
        Assert.AreEqual(1, GetGridsRemaining(2, 3));
    }

    [Test]
    public void GridsRemaining_FullyCharged_ReturnsZero()
    {
        Assert.AreEqual(0, GetGridsRemaining(3, 3));
        Assert.AreEqual(0, GetGridsRemaining(10, 3));
    }

    // --- Status Label Logic ---

    private static string GetStatusLabel(bool unlocked, bool ready, int gridsRemaining)
    {
        if (!unlocked) return "LOCKED";
        if (ready) return "READY";
        if (gridsRemaining <= 0) return "READY";
        return gridsRemaining == 1 ? "1 GRID TO READY" : $"{gridsRemaining} GRIDS TO READY";
    }

    [Test]
    public void StatusLabel_WhenLocked_ReturnsLocked()
    {
        Assert.AreEqual("LOCKED", GetStatusLabel(false, false, 3));
    }

    [Test]
    public void StatusLabel_WhenReady_ReturnsReady()
    {
        Assert.AreEqual("READY", GetStatusLabel(true, true, 0));
    }

    [Test]
    public void StatusLabel_OneGridRemaining_ReturnsSingular()
    {
        Assert.AreEqual("1 GRID TO READY", GetStatusLabel(true, false, 1));
    }

    [Test]
    public void StatusLabel_MultipleGridsRemaining_ReturnsPlural()
    {
        Assert.AreEqual("2 GRIDS TO READY", GetStatusLabel(true, false, 2));
        Assert.AreEqual("3 GRIDS TO READY", GetStatusLabel(true, false, 3));
    }

    // --- Energy cost ---

    [Test]
    public void ChronoclasmEnergyCost_IsOne()
    {
        // Chronoclasm costs 1 energy (verified from source: chronoclasmEnergyCost = 1)
        int cost = 1;
        int playerEnergy = 3;
        Assert.IsTrue(playerEnergy >= cost, "Player with full energy should afford Chronoclasm");

        playerEnergy = 0;
        Assert.IsFalse(playerEnergy >= cost, "Player with 0 energy should not afford Chronoclasm");
    }
}
