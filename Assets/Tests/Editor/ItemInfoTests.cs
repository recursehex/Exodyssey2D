using NUnit.Framework;

public class ItemInfoTests
{
    [Test]
    public void Constructor_Branch_HasCorrectProperties()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        Assert.AreEqual(ItemInfo.Tags.Branch, item.Tag);
        Assert.AreEqual(ItemInfo.Types.Weapon, item.Type);
        Assert.IsTrue(item.CurrentUses > 0);
        Assert.IsTrue(item.DamagePoints > 0);
        Assert.IsFalse(item.HasRange);
    }

    [Test]
    public void Constructor_Carbine_IsRanged()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Carbine);
        Assert.AreEqual(ItemInfo.Tags.Carbine, item.Tag);
        Assert.AreEqual(ItemInfo.Types.Weapon, item.Type);
        Assert.IsTrue(item.HasRange);
        Assert.Greater(item.Range, 0);
    }

    [Test]
    public void Constructor_MedKit_IsConsumable()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.MedKit);
        Assert.AreEqual(ItemInfo.Tags.MedKit, item.Tag);
        Assert.AreEqual(ItemInfo.Types.Consumable, item.Type);
    }

    [Test]
    public void Constructor_Helmet_IsEquipable()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Helmet);
        Assert.AreEqual(ItemInfo.Types.Armor, item.Type);
        Assert.IsTrue(item.IsEquipable);
    }

    [Test]
    public void Constructor_Flare_IsUtility()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Flare);
        Assert.AreEqual(ItemInfo.Tags.Flare, item.Tag);
        Assert.AreEqual(ItemInfo.Types.Utility, item.Type);
    }

    // --- Durability ---

    [Test]
    public void DecreaseDurability_ByOne_ReducesUses()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        int initial = item.CurrentUses;
        item.DecreaseDurability();
        Assert.AreEqual(initial - 1, item.CurrentUses);
    }

    [Test]
    public void DecreaseDurability_ByMultiple_ReducesCorrectly()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Carbine);
        int initial = item.CurrentUses;
        item.DecreaseDurability(3);
        Assert.AreEqual(initial - 3, item.CurrentUses);
    }

    [Test]
    public void DecreaseDurability_ClampsToZero()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        item.DecreaseDurability(999);
        Assert.AreEqual(0, item.CurrentUses);
    }

    [Test]
    public void DecreaseDurability_ZeroAmount_DoesNothing()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        int initial = item.CurrentUses;
        item.DecreaseDurability(0);
        Assert.AreEqual(initial, item.CurrentUses);
    }

    [Test]
    public void DecreaseDurability_NegativeAmount_DoesNothing()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        int initial = item.CurrentUses;
        item.DecreaseDurability(-5);
        Assert.AreEqual(initial, item.CurrentUses);
    }

    [Test]
    public void RestoreDurabilityToMax_RestoresFullUses()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Carbine);
        int max = item.CurrentUses;
        item.DecreaseDurability(2);
        Assert.Less(item.CurrentUses, max);

        item.RestoreDurabilityToMax();
        Assert.AreEqual(max, item.CurrentUses);
    }

    // --- PlasmaRailgun Unbreakable ---

    [Test]
    public void PlasmaRailgun_IsUnbreakable()
    {
        ItemInfo railgun = new ItemInfo((int)ItemInfo.Tags.PlasmaRailgun);
        Assert.IsTrue(railgun.IsUnbreakable);
    }

    [Test]
    public void NonRailgun_IsNotUnbreakable()
    {
        ItemInfo branch = new ItemInfo((int)ItemInfo.Tags.Branch);
        Assert.IsFalse(branch.IsUnbreakable);
    }

    // --- Flare Lifecycle ---

    [Test]
    public void ActivateFlare_OnFlare_ReturnsTrue()
    {
        ItemInfo flare = new ItemInfo((int)ItemInfo.Tags.Flare);
        Assert.IsFalse(flare.IsActiveFlare);
        Assert.IsTrue(flare.ActivateFlare());
        Assert.IsTrue(flare.IsActiveFlare);
        Assert.AreEqual(3, flare.ActiveFlareTurnsRemaining);
    }

    [Test]
    public void ActivateFlare_OnNonFlare_ReturnsFalse()
    {
        ItemInfo branch = new ItemInfo((int)ItemInfo.Tags.Branch);
        Assert.IsFalse(branch.ActivateFlare());
        Assert.IsFalse(branch.IsActiveFlare);
    }

    [Test]
    public void ActivateFlare_AlreadyActive_ReturnsFalse()
    {
        ItemInfo flare = new ItemInfo((int)ItemInfo.Tags.Flare);
        flare.ActivateFlare();
        Assert.IsFalse(flare.ActivateFlare());
    }

    [Test]
    public void TickActiveFlare_DecrementsTurnsRemaining()
    {
        ItemInfo flare = new ItemInfo((int)ItemInfo.Tags.Flare);
        flare.ActivateFlare();

        bool burnedOut = flare.TickActiveFlare();
        Assert.IsFalse(burnedOut);
        Assert.AreEqual(2, flare.ActiveFlareTurnsRemaining);
    }

    [Test]
    public void TickActiveFlare_BurnsOutAfterThreeTicks()
    {
        ItemInfo flare = new ItemInfo((int)ItemInfo.Tags.Flare);
        flare.ActivateFlare();

        Assert.IsFalse(flare.TickActiveFlare()); // 2 remaining
        Assert.IsFalse(flare.TickActiveFlare()); // 1 remaining
        Assert.IsTrue(flare.TickActiveFlare());  // 0 remaining, burned out

        Assert.IsFalse(flare.IsActiveFlare);
        Assert.AreEqual(0, flare.ActiveFlareTurnsRemaining);
    }

    [Test]
    public void TickActiveFlare_OnInactiveFlare_ReturnsFalse()
    {
        ItemInfo flare = new ItemInfo((int)ItemInfo.Tags.Flare);
        Assert.IsFalse(flare.TickActiveFlare());
    }

    [Test]
    public void ExtinguishFlare_ResetsState()
    {
        ItemInfo flare = new ItemInfo((int)ItemInfo.Tags.Flare);
        flare.ActivateFlare();
        Assert.IsTrue(flare.IsActiveFlare);

        flare.ExtinguishFlare();
        Assert.IsFalse(flare.IsActiveFlare);
        Assert.AreEqual(0, flare.ActiveFlareTurnsRemaining);
    }

    // --- Clone ---

    [Test]
    public void Clone_PreservesTag()
    {
        ItemInfo original = new ItemInfo((int)ItemInfo.Tags.Knife);
        ItemInfo clone = original.Clone();
        Assert.AreEqual(original.Tag, clone.Tag);
    }

    [Test]
    public void Clone_PreservesDurability()
    {
        ItemInfo original = new ItemInfo((int)ItemInfo.Tags.Knife);
        original.DecreaseDurability(1);
        ItemInfo clone = original.Clone();
        Assert.AreEqual(original.CurrentUses, clone.CurrentUses);
    }

    [Test]
    public void Clone_PreservesFlareState()
    {
        ItemInfo original = new ItemInfo((int)ItemInfo.Tags.Flare);
        original.ActivateFlare();
        original.TickActiveFlare();

        ItemInfo clone = original.Clone();
        Assert.AreEqual(original.IsActiveFlare, clone.IsActiveFlare);
        Assert.AreEqual(original.ActiveFlareTurnsRemaining, clone.ActiveFlareTurnsRemaining);
    }

    [Test]
    public void Clone_IsIndependentCopy()
    {
        ItemInfo original = new ItemInfo((int)ItemInfo.Tags.Carbine);
        ItemInfo clone = original.Clone();

        clone.DecreaseDurability(1);
        Assert.AreNotEqual(original.CurrentUses, clone.CurrentUses);
    }

    // --- Weapon Properties ---

    [Test]
    public void MeleeWeapon_HasNegativeRange()
    {
        ItemInfo branch = new ItemInfo((int)ItemInfo.Tags.Branch);
        Assert.IsFalse(branch.HasRange);
        Assert.Less(branch.Range, 1);
    }

    [Test]
    public void RangedWeapon_HasPositiveRange()
    {
        ItemInfo rifle = new ItemInfo((int)ItemInfo.Tags.HuntingRifle);
        Assert.IsTrue(rifle.HasRange);
        Assert.Greater(rifle.Range, 0);
    }

    [Test]
    public void Consumable_HasNoDamage()
    {
        ItemInfo medkit = new ItemInfo((int)ItemInfo.Tags.MedKit);
        Assert.AreEqual(-1, medkit.DamagePoints);
    }

    // --- Flammable ---

    [Test]
    public void Branch_IsFlammable()
    {
        ItemInfo branch = new ItemInfo((int)ItemInfo.Tags.Branch);
        Assert.IsTrue(branch.IsFlammable);
    }

    // --- Stunning ---

    [Test]
    public void Tranquilizer_IsStunning()
    {
        ItemInfo tranq = new ItemInfo((int)ItemInfo.Tags.Tranquilizer);
        Assert.IsTrue(tranq.IsStunning);
    }

    [Test]
    public void Branch_IsNotStunning()
    {
        ItemInfo branch = new ItemInfo((int)ItemInfo.Tags.Branch);
        Assert.IsFalse(branch.IsStunning);
    }

    // --- Stats String ---

    [Test]
    public void Stats_ContainsDurabilityInfo()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Branch);
        Assert.IsTrue(item.Stats.Contains("UP:"));
    }

    [Test]
    public void Stats_ForWeapon_ContainsDamagePoints()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Knife);
        Assert.IsTrue(item.Stats.Contains("DP:"));
    }

    [Test]
    public void Stats_ForRangedWeapon_ContainsRange()
    {
        ItemInfo item = new ItemInfo((int)ItemInfo.Tags.Carbine);
        Assert.IsTrue(item.Stats.Contains("RP:"));
    }
}
