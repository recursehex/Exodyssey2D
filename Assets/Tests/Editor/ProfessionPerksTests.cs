using NUnit.Framework;
using UnityEngine;

public class ProfessionPerksTests
{
    private static Profession Base(Profession.Tags tag) => new(tag, false);
    private static Profession Master(Profession.Tags tag) => new(tag, true);

    // WALK SPEED (HIKER)
    [Test]
    public void GetWalkSpeed_Hiker_IsDoubled()
    {
        Assert.AreEqual(4f, ProfessionPerks.GetWalkSpeed(Base(Profession.Tags.Hiker), 2f));
    }

    [Test]
    public void GetWalkSpeed_NonHiker_IsUnchanged()
    {
        foreach (Profession profession in Profession.ProfessionList)
        {
            if (profession.Tag == Profession.Tags.Hiker)
                continue;
            Assert.AreEqual(2f, ProfessionPerks.GetWalkSpeed(profession, 2f), $"{profession.Tag} should not change walk speed");
        }
    }

    // ATTACK ANIMATION SPEED (HUNTER)
    [Test]
    public void GetAttackAnimationSpeed_Hunter_IsDoubled()
    {
        Assert.AreEqual(2f, ProfessionPerks.GetAttackAnimationSpeed(Base(Profession.Tags.Hunter)));
    }

    [Test]
    public void GetAttackAnimationSpeed_NonHunter_IsNormal()
    {
        foreach (Profession profession in Profession.ProfessionList)
        {
            if (profession.Tag == Profession.Tags.Hunter)
                continue;
            Assert.AreEqual(1f, ProfessionPerks.GetAttackAnimationSpeed(profession), $"{profession.Tag} should not change attack animation speed");
        }
    }

    // DRIVE SPEED (NAVIGATOR)
    [Test]
    public void GetDriveSpeedMultiplier_Navigator_IsDoubled()
    {
        Assert.AreEqual(2f, ProfessionPerks.GetDriveSpeedMultiplier(Base(Profession.Tags.Navigator)));
    }

    [Test]
    public void GetDriveSpeedMultiplier_NonNavigator_IsNormal()
    {
        foreach (Profession profession in Profession.ProfessionList)
        {
            if (profession.Tag == Profession.Tags.Navigator)
                continue;
            Assert.AreEqual(1f, ProfessionPerks.GetDriveSpeedMultiplier(profession), $"{profession.Tag} should not change drive speed");
        }
    }

    // HEALING (MEDIC)
    [Test]
    public void HealCostsEnergy_Medic_IsFree()
    {
        Assert.IsFalse(ProfessionPerks.HealCostsEnergy(Base(Profession.Tags.Medic)));
        Assert.IsFalse(ProfessionPerks.HealCostsEnergy(Master(Profession.Tags.Medic)));
    }

    [Test]
    public void HealCostsEnergy_NonMedic_CostsEnergy()
    {
        Assert.IsTrue(ProfessionPerks.HealCostsEnergy(Base(Profession.Tags.Hunter)));
        Assert.IsTrue(ProfessionPerks.HealCostsEnergy(Master(Profession.Tags.Mechanic)));
    }

    [Test]
    public void HealConsumesDurability_OnlyMasterMedicKeepsUses()
    {
        Assert.IsFalse(ProfessionPerks.HealConsumesDurability(Master(Profession.Tags.Medic)));
        Assert.IsTrue(ProfessionPerks.HealConsumesDurability(Base(Profession.Tags.Medic)));
        Assert.IsTrue(ProfessionPerks.HealConsumesDurability(Master(Profession.Tags.Hunter)));
    }

    // VEHICLE REPAIR (MECHANIC)
    [Test]
    public void RepairCostsEnergy_Mechanic_IsFree()
    {
        Assert.IsFalse(ProfessionPerks.RepairCostsEnergy(Base(Profession.Tags.Mechanic)));
        Assert.IsFalse(ProfessionPerks.RepairCostsEnergy(Master(Profession.Tags.Mechanic)));
    }

    [Test]
    public void RepairCostsEnergy_NonMechanic_CostsEnergy()
    {
        Assert.IsTrue(ProfessionPerks.RepairCostsEnergy(Base(Profession.Tags.Medic)));
        Assert.IsTrue(ProfessionPerks.RepairCostsEnergy(Master(Profession.Tags.Hiker)));
    }

    [Test]
    public void RepairConsumesDurability_OnlyMasterMechanicKeepsUses()
    {
        Assert.IsFalse(ProfessionPerks.RepairConsumesDurability(Master(Profession.Tags.Mechanic)));
        Assert.IsTrue(ProfessionPerks.RepairConsumesDurability(Base(Profession.Tags.Mechanic)));
        Assert.IsTrue(ProfessionPerks.RepairConsumesDurability(Master(Profession.Tags.Hunter)));
    }

    // FREE FIRST STEP (MASTER HIKER)
    [Test]
    public void HasFreeFirstStep_MasterHiker_IsTrue()
    {
        Assert.IsTrue(ProfessionPerks.HasFreeFirstStep(Master(Profession.Tags.Hiker)));
    }

    [Test]
    public void HasFreeFirstStep_NonMasterHiker_IsFalse()
    {
        Assert.IsFalse(ProfessionPerks.HasFreeFirstStep(Base(Profession.Tags.Hiker)));
    }

    [Test]
    public void HasFreeFirstStep_MasterNonHiker_IsFalse()
    {
        Assert.IsFalse(ProfessionPerks.HasFreeFirstStep(Master(Profession.Tags.Hunter)));
    }

    // BONUS DAMAGE (MASTER HUNTER)
    [Test]
    public void GetAttackDamage_MasterHunter_GainsOneDamage()
    {
        Assert.AreEqual(3, ProfessionPerks.GetAttackDamage(Master(Profession.Tags.Hunter), 2));
    }

    [Test]
    public void GetAttackDamage_MasterHunter_StunOnlyWeaponStaysDamageless()
    {
        Assert.AreEqual(0, ProfessionPerks.GetAttackDamage(Master(Profession.Tags.Hunter), 0));
    }

    [Test]
    public void GetAttackDamage_NonMasterHunter_IsUnchanged()
    {
        Assert.AreEqual(2, ProfessionPerks.GetAttackDamage(Base(Profession.Tags.Hunter), 2));
    }

    [Test]
    public void GetAttackDamage_MasterNonHunter_IsUnchanged()
    {
        Assert.AreEqual(2, ProfessionPerks.GetAttackDamage(Master(Profession.Tags.Medic), 2));
    }

    // GRID TRAVEL CHARGE (MASTER NAVIGATOR)
    [Test]
    public void GetGridTravelCharge_MasterNavigator_UsesOneLessFuel()
    {
        Assert.AreEqual(1, ProfessionPerks.GetGridTravelCharge(Master(Profession.Tags.Navigator), 2));
    }

    [Test]
    public void GetGridTravelCharge_MasterNavigator_NeverGoesNegative()
    {
        Assert.AreEqual(0, ProfessionPerks.GetGridTravelCharge(Master(Profession.Tags.Navigator), 1));
        Assert.AreEqual(0, ProfessionPerks.GetGridTravelCharge(Master(Profession.Tags.Navigator), 0));
    }

    [Test]
    public void GetGridTravelCharge_NonMasterNavigator_IsUnchanged()
    {
        Assert.AreEqual(2, ProfessionPerks.GetGridTravelCharge(Base(Profession.Tags.Navigator), 2));
    }

    [Test]
    public void GetGridTravelCharge_MasterNonNavigator_IsUnchanged()
    {
        Assert.AreEqual(2, ProfessionPerks.GetGridTravelCharge(Master(Profession.Tags.Hiker), 2));
    }

    // WEAPON RANGE (RANGER)
    [Test]
    public void GetWeaponRange_Ranger_ThrowableCoversWholeGrid()
    {
        ItemInfo rock = new ItemInfo((int)ItemInfo.Tags.Rock);
        Assert.IsTrue(rock.IsThrowable);
        Assert.AreEqual(ProfessionPerks.FullGridRange, ProfessionPerks.GetWeaponRange(Base(Profession.Tags.Ranger), rock));
    }

    [Test]
    public void GetWeaponRange_MasterRanger_ThrowableStillCoversWholeGrid()
    {
        ItemInfo dynamite = new ItemInfo((int)ItemInfo.Tags.Dynamite);
        Assert.IsTrue(dynamite.IsThrowable);
        Assert.AreEqual(ProfessionPerks.FullGridRange, ProfessionPerks.GetWeaponRange(Master(Profession.Tags.Ranger), dynamite));
    }

    [Test]
    public void GetWeaponRange_MasterRanger_RangedWeaponGainsOneRange()
    {
        ItemInfo carbine = new ItemInfo((int)ItemInfo.Tags.Carbine);
        Assert.AreEqual(carbine.Range + 1, ProfessionPerks.GetWeaponRange(Master(Profession.Tags.Ranger), carbine));
    }

    [Test]
    public void GetWeaponRange_NonMasterRanger_RangedWeaponIsUnchanged()
    {
        ItemInfo carbine = new ItemInfo((int)ItemInfo.Tags.Carbine);
        Assert.AreEqual(carbine.Range, ProfessionPerks.GetWeaponRange(Base(Profession.Tags.Ranger), carbine));
    }

    [Test]
    public void GetWeaponRange_NonRanger_IsUnchanged()
    {
        ItemInfo rock = new ItemInfo((int)ItemInfo.Tags.Rock);
        ItemInfo carbine = new ItemInfo((int)ItemInfo.Tags.Carbine);
        Assert.AreEqual(rock.Range, ProfessionPerks.GetWeaponRange(Base(Profession.Tags.Hunter), rock));
        Assert.AreEqual(carbine.Range, ProfessionPerks.GetWeaponRange(Master(Profession.Tags.Hunter), carbine));
    }

    [Test]
    public void GetWeaponRange_NullOrRangelessItem_ReturnsZero()
    {
        ItemInfo medkit = new ItemInfo((int)ItemInfo.Tags.MedKit);
        Assert.AreEqual(0, ProfessionPerks.GetWeaponRange(Base(Profession.Tags.Ranger), null));
        Assert.AreEqual(0, ProfessionPerks.GetWeaponRange(Base(Profession.Tags.Ranger), medkit));
    }

    [Test]
    public void FullGridRange_CoversMaxGridDistance()
    {
        float maxDistance = Mathf.Sqrt(
            Mathf.Pow(GameConfig.Grid.MaxX - GameConfig.Grid.MinX, 2) +
            Mathf.Pow(GameConfig.Grid.MaxY - GameConfig.Grid.MinY, 2));
        Assert.GreaterOrEqual(ProfessionPerks.FullGridRange, maxDistance);
    }

    // MASTERY THRESHOLD
    [Test]
    public void ShouldMaster_BeforeThreeRegions_IsFalse()
    {
        Assert.IsFalse(ProfessionPerks.ShouldMaster(0));
        Assert.IsFalse(ProfessionPerks.ShouldMaster(2));
    }

    [Test]
    public void ShouldMaster_AtThreeRegions_IsTrue()
    {
        Assert.IsTrue(ProfessionPerks.ShouldMaster(3));
        Assert.IsTrue(ProfessionPerks.ShouldMaster(4));
    }
}
