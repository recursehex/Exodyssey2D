using NUnit.Framework;

public class VehicleInfoTests
{
    // --- Construction ---

    [Test]
    public void Constructor_Rover_HasCorrectTag()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        Assert.AreEqual(VehicleInfo.Tags.Rover, rover.Tag);
        Assert.AreEqual(VehicleInfo.Types.Car, rover.Type);
    }

    [Test]
    public void Constructor_Carrier_IsLargeVehicle()
    {
        VehicleInfo carrier = new VehicleInfo((int)VehicleInfo.Tags.Carrier);
        Assert.AreEqual(VehicleInfo.Tags.Carrier, carrier.Tag);
        Assert.AreEqual(VehicleInfo.Types.LargeVehicle, carrier.Type);
    }

    [Test]
    public void Constructor_StartsAtMaxCharge()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        // Max charge for Rover is 10 (from JSON)
        Assert.Greater(rover.CurrentCharge, 0);
    }

    [Test]
    public void Constructor_StartsAtMaxHealth()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        Assert.Greater(rover.CurrentHealth, 0);
    }

    [Test]
    public void Constructor_WithStartingFuel_ClampsCharge()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover, startingFuel: 3);
        Assert.AreEqual(3, rover.CurrentCharge);
    }

    [Test]
    public void Constructor_WithZeroFuel_HasZeroCharge()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover, startingFuel: 0);
        Assert.AreEqual(0, rover.CurrentCharge);
    }

    // --- Charge Mechanics ---

    [Test]
    public void TryDecreaseChargeBy_ValidAmount_Succeeds()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        int initialCharge = rover.CurrentCharge;
        Assert.IsTrue(rover.TryDecreaseChargeBy(1));
        Assert.AreEqual(initialCharge - 1, rover.CurrentCharge);
    }

    [Test]
    public void TryDecreaseChargeBy_ExceedsCharge_Fails()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover, startingFuel: 2);
        Assert.IsFalse(rover.TryDecreaseChargeBy(3));
        Assert.AreEqual(2, rover.CurrentCharge); // Unchanged
    }

    [Test]
    public void TryDecreaseChargeBy_ExactCharge_Succeeds()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover, startingFuel: 5);
        Assert.IsTrue(rover.TryDecreaseChargeBy(5));
        Assert.AreEqual(0, rover.CurrentCharge);
    }

    [Test]
    public void TryRechargeBy_WhenNotFull_Succeeds()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover, startingFuel: 3);
        int amount = 5;
        Assert.IsTrue(rover.TryRechargeBy(ref amount));
        Assert.Greater(rover.CurrentCharge, 3);
    }

    [Test]
    public void TryRechargeBy_WhenFull_Fails()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        // Already at max charge
        int amount = 5;
        Assert.IsFalse(rover.TryRechargeBy(ref amount));
    }

    [Test]
    public void TryRechargeBy_ClampsToMaxCharge()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover, startingFuel: 0);
        int maxCharge = rover.CurrentCharge; // 0
        // Give a huge recharge amount
        int amount = 999;
        rover.TryRechargeBy(ref amount);
        // Amount should be reduced to what was actually needed
        Assert.Greater(amount, 0);
        Assert.Less(amount, 999);
    }

    [Test]
    public void TryRechargeBy_SubtractsUsedAmountFromInput()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover, startingFuel: 8);
        // Rover max charge is 10, currently at 8, so 2 charge needed
        int amount = 5;
        rover.TryRechargeBy(ref amount);
        Assert.AreEqual(2, amount); // Only 2 was used
    }

    [Test]
    public void SetCharge_ClampsToValidRange()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        rover.SetCharge(-10);
        Assert.AreEqual(0, rover.CurrentCharge);

        rover.SetCharge(999);
        // Should be clamped to maxCharge
        Assert.Greater(rover.CurrentCharge, 0);
    }

    // --- Health Mechanics ---

    [Test]
    public void DecreaseHealthBy_ReducesHealth()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        int initialHealth = rover.CurrentHealth;
        rover.DecreaseHealthBy(1);
        Assert.AreEqual(initialHealth - 1, rover.CurrentHealth);
    }

    [Test]
    public void TryRestoreHealth_WhenDamaged_Succeeds()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        int maxHealth = rover.CurrentHealth;
        rover.DecreaseHealthBy(1);
        Assert.Less(rover.CurrentHealth, maxHealth);

        Assert.IsTrue(rover.TryRestoreHealth());
        Assert.AreEqual(maxHealth, rover.CurrentHealth);
    }

    [Test]
    public void TryRestoreHealth_WhenAtMax_Fails()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        Assert.IsFalse(rover.TryRestoreHealth());
    }

    // --- Ignition ---

    [Test]
    public void SwitchIgnition_TogglesState()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        Assert.IsFalse(rover.IsOn);
        rover.SwitchIgnition();
        Assert.IsTrue(rover.IsOn);
        rover.SwitchIgnition();
        Assert.IsFalse(rover.IsOn);
    }

    // --- Vehicle Properties ---

    [Test]
    public void Buggy_CanOffroad()
    {
        VehicleInfo buggy = new VehicleInfo((int)VehicleInfo.Tags.Buggy);
        Assert.IsTrue(buggy.CanOffroad);
    }

    [Test]
    public void Rover_CannotOffroad()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        Assert.IsFalse(rover.CanOffroad);
    }

    [Test]
    public void Carrier_HasHighestStorage()
    {
        VehicleInfo carrier = new VehicleInfo((int)VehicleInfo.Tags.Carrier);
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        VehicleInfo buggy = new VehicleInfo((int)VehicleInfo.Tags.Buggy);
        Assert.Greater(carrier.Storage, rover.Storage);
        Assert.Greater(carrier.Storage, buggy.Storage);
    }

    [Test]
    public void Buggy_HasZeroStorage()
    {
        VehicleInfo buggy = new VehicleInfo((int)VehicleInfo.Tags.Buggy);
        Assert.AreEqual(0, buggy.Storage);
    }

    [Test]
    public void AllVehicles_HavePositiveEfficiency()
    {
        for (int i = 0; i < (int)VehicleInfo.Tags.Unknown; i++)
        {
            VehicleInfo vehicle = new VehicleInfo(i);
            Assert.Greater(vehicle.Efficiency, 0, $"Vehicle {vehicle.Tag} should have positive efficiency");
        }
    }

    [Test]
    public void AllVehicles_HavePositiveMovementRange()
    {
        for (int i = 0; i < (int)VehicleInfo.Tags.Unknown; i++)
        {
            VehicleInfo vehicle = new VehicleInfo(i);
            Assert.Greater(vehicle.MovementRange, 0, $"Vehicle {vehicle.Tag} should have positive movement range");
        }
    }

    // --- Run Over Ability ---

    [Test]
    public void Rover_RunsOverWeakOnly()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        Assert.IsTrue(rover.CanRunOverType(EnemyInfo.Types.Weak));
        Assert.IsFalse(rover.CanRunOverType(EnemyInfo.Types.Mediocre));
        Assert.IsFalse(rover.CanRunOverType(EnemyInfo.Types.Strong));
        Assert.IsFalse(rover.CanRunOverType(EnemyInfo.Types.Exotic));
    }

    [Test]
    public void Buggy_RunsOverWeakOnly()
    {
        VehicleInfo buggy = new VehicleInfo((int)VehicleInfo.Tags.Buggy);
        Assert.IsTrue(buggy.CanRunOverType(EnemyInfo.Types.Weak));
        Assert.IsFalse(buggy.CanRunOverType(EnemyInfo.Types.Mediocre));
    }

    [Test]
    public void Trailer_RunsOverWeakAndMediocre()
    {
        VehicleInfo trailer = new VehicleInfo((int)VehicleInfo.Tags.Trailer);
        Assert.IsTrue(trailer.CanRunOverType(EnemyInfo.Types.Weak));
        Assert.IsTrue(trailer.CanRunOverType(EnemyInfo.Types.Mediocre));
        Assert.IsFalse(trailer.CanRunOverType(EnemyInfo.Types.Strong));
        Assert.IsFalse(trailer.CanRunOverType(EnemyInfo.Types.Exotic));
    }

    [Test]
    public void Carrier_RunsOverAllNonBossTypes()
    {
        VehicleInfo carrier = new VehicleInfo((int)VehicleInfo.Tags.Carrier);
        Assert.IsTrue(carrier.CanRunOverType(EnemyInfo.Types.Weak));
        Assert.IsTrue(carrier.CanRunOverType(EnemyInfo.Types.Mediocre));
        Assert.IsTrue(carrier.CanRunOverType(EnemyInfo.Types.Strong));
        Assert.IsTrue(carrier.CanRunOverType(EnemyInfo.Types.Exotic));
    }

    [Test]
    public void CanRunOverType_UnknownType_ReturnsFalse()
    {
        VehicleInfo carrier = new VehicleInfo((int)VehicleInfo.Tags.Carrier);
        Assert.IsFalse(carrier.CanRunOverType(EnemyInfo.Types.Unknown));
    }

    // --- Ram Ability ---

    [Test]
    public void Rover_RamsMediocreThroughExotic_NotBoss()
    {
        VehicleInfo rover = new VehicleInfo((int)VehicleInfo.Tags.Rover);
        // Weak is run over, not rammed
        Assert.IsFalse(rover.CanRamType(EnemyInfo.Types.Weak));
        Assert.IsTrue(rover.CanRamType(EnemyInfo.Types.Mediocre));
        Assert.IsTrue(rover.CanRamType(EnemyInfo.Types.Strong));
        Assert.IsTrue(rover.CanRamType(EnemyInfo.Types.Exotic));
        Assert.IsFalse(rover.CanRamType(EnemyInfo.Types.Boss));
    }

    [Test]
    public void Trailer_RamsStrongAndExotic_NotBoss()
    {
        VehicleInfo trailer = new VehicleInfo((int)VehicleInfo.Tags.Trailer);
        // Weak and Mediocre are run over, not rammed
        Assert.IsFalse(trailer.CanRamType(EnemyInfo.Types.Weak));
        Assert.IsFalse(trailer.CanRamType(EnemyInfo.Types.Mediocre));
        Assert.IsTrue(trailer.CanRamType(EnemyInfo.Types.Strong));
        Assert.IsTrue(trailer.CanRamType(EnemyInfo.Types.Exotic));
        Assert.IsFalse(trailer.CanRamType(EnemyInfo.Types.Boss));
    }

    [Test]
    public void Buggy_RamsMediocreOnly()
    {
        VehicleInfo buggy = new VehicleInfo((int)VehicleInfo.Tags.Buggy);
        Assert.IsTrue(buggy.CanRamType(EnemyInfo.Types.Mediocre));
        // Buggy cannot ram Strong or Exotic
        Assert.IsFalse(buggy.CanRamType(EnemyInfo.Types.Strong));
        Assert.IsFalse(buggy.CanRamType(EnemyInfo.Types.Exotic));
        Assert.IsFalse(buggy.CanRamType(EnemyInfo.Types.Boss));
    }

    [Test]
    public void Carrier_RamsBossOnly()
    {
        VehicleInfo carrier = new VehicleInfo((int)VehicleInfo.Tags.Carrier);
        // All non-boss types are run over for free, so not rammed
        Assert.IsFalse(carrier.CanRamType(EnemyInfo.Types.Weak));
        Assert.IsFalse(carrier.CanRamType(EnemyInfo.Types.Exotic));
        // Bosses cannot be run over, only rammed
        Assert.IsFalse(carrier.CanRunOverType(EnemyInfo.Types.Boss));
        Assert.IsTrue(carrier.CanRamType(EnemyInfo.Types.Boss));
    }
}
