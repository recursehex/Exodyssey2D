using NUnit.Framework;

/// <summary>
/// Tests for the armor damage absorption formulas used in Player.DecreaseHealthBy.
/// Since Player is a MonoBehaviour we test the pure math here.
/// </summary>
public class ArmorTests
{
    /// <summary>
    /// Simulates the armor absorption logic from Player.DecreaseHealthBy.
    /// Returns (remainingDamage, remainingArmorHealth, armorDestroyed)
    /// </summary>
    private static (int damage, int armorHealth, bool destroyed) ApplyArmor(int incomingDamage, int armorHealth)
    {
        int absorbed = System.Math.Min(incomingDamage, armorHealth);
        armorHealth -= absorbed;
        int damage = incomingDamage - absorbed;
        bool destroyed = armorHealth <= 0;
        return (damage, armorHealth, destroyed);
    }

    // --- Helmet (Melee) ---

    [Test]
    public void Helmet_AbsorbsFullMeleeDamage_WhenHealthSufficient()
    {
        var (damage, health, destroyed) = ApplyArmor(1, 3);
        Assert.AreEqual(0, damage);
        Assert.AreEqual(2, health);
        Assert.IsFalse(destroyed);
    }

    [Test]
    public void Helmet_AbsorbsPartialDamage_WhenHealthInsufficient()
    {
        var (damage, health, destroyed) = ApplyArmor(3, 1);
        Assert.AreEqual(2, damage); // 3 - 1 = 2 passes through
        Assert.AreEqual(0, health);
        Assert.IsTrue(destroyed);
    }

    [Test]
    public void Helmet_DestroyedByExactDamage()
    {
        var (damage, health, destroyed) = ApplyArmor(2, 2);
        Assert.AreEqual(0, damage);
        Assert.AreEqual(0, health);
        Assert.IsTrue(destroyed);
    }

    [Test]
    public void Helmet_ZeroDamage_NoEffect()
    {
        var (damage, health, destroyed) = ApplyArmor(0, 3);
        Assert.AreEqual(0, damage);
        Assert.AreEqual(3, health);
        Assert.IsFalse(destroyed);
    }

    // --- Vest (Ranged) ---

    [Test]
    public void Vest_AbsorbsRangedDamage()
    {
        var (damage, health, destroyed) = ApplyArmor(2, 4);
        Assert.AreEqual(0, damage);
        Assert.AreEqual(2, health);
        Assert.IsFalse(destroyed);
    }

    [Test]
    public void Vest_BreaksFromHighDamage()
    {
        var (damage, health, destroyed) = ApplyArmor(5, 3);
        Assert.AreEqual(2, damage);
        Assert.AreEqual(0, health);
        Assert.IsTrue(destroyed);
    }

    // --- Critical Health Energy Reduction ---

    /// <summary>
    /// When player health reaches 1, max energy should drop to 1 to simulate weakness.
    /// Tests the condition check.
    /// </summary>
    [Test]
    public void CriticalHealth_TriggersEnergyReduction()
    {
        int maxHealth = 3;
        int currentHealth = maxHealth;
        int maxEnergy = 3;

        // Take 2 damage to reach 1 HP
        currentHealth -= 2;
        Assert.AreEqual(1, currentHealth);

        // At 1 HP, max energy is reduced
        if (currentHealth == 1)
            maxEnergy = 1;
        Assert.AreEqual(1, maxEnergy);
    }

    [Test]
    public void NonCriticalHealth_NoEnergyReduction()
    {
        int currentHealth = 2;
        int maxEnergy = 3;

        if (currentHealth == 1)
            maxEnergy = 1;
        Assert.AreEqual(3, maxEnergy); // Should remain unchanged
    }

    // --- Combined scenarios ---

    [Test]
    public void MeleeDamageWithHelmet_ThenRangedDamageWithVest()
    {
        int helmetHealth = 2;
        int vestHealth = 3;
        int playerHealth = 3;

        // Melee hit of 1
        var meleeResult = ApplyArmor(1, helmetHealth);
        helmetHealth = meleeResult.armorHealth;
        playerHealth -= meleeResult.damage;
        Assert.AreEqual(3, playerHealth); // No damage passed through
        Assert.AreEqual(1, helmetHealth);

        // Ranged hit of 2
        var rangedResult = ApplyArmor(2, vestHealth);
        vestHealth = rangedResult.armorHealth;
        playerHealth -= rangedResult.damage;
        Assert.AreEqual(3, playerHealth); // Still no damage
        Assert.AreEqual(1, vestHealth);
    }

    [Test]
    public void NoArmor_FullDamagePasses()
    {
        // Without armor, player takes full damage
        // hasHelmet = false, so armor check is skipped
        int playerHealth = 3;
        int damage = 2;
        playerHealth -= damage;
        Assert.AreEqual(1, playerHealth);
    }
}
