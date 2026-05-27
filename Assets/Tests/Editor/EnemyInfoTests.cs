using NUnit.Framework;

public class EnemyInfoTests
{
    // --- Construction ---

    [Test]
    public void Constructor_Crawler_HasCorrectProperties()
    {
        EnemyInfo crawler = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        Assert.AreEqual(EnemyInfo.Tags.Crawler, crawler.Tag);
        Assert.AreEqual(EnemyInfo.Types.Weak, crawler.Type);
        Assert.Greater(crawler.CurrentHealth, 0);
        Assert.Greater(crawler.CurrentEnergy, 0);
    }

    [Test]
    public void Constructor_Launcher_IsRanged()
    {
        EnemyInfo launcher = new EnemyInfo((int)EnemyInfo.Tags.Launcher);
        Assert.AreEqual(EnemyInfo.Tags.Launcher, launcher.Tag);
        Assert.AreEqual(EnemyInfo.Types.Mediocre, launcher.Type);
        Assert.Greater(launcher.Range, 0);
    }

    [Test]
    public void Crawler_IsMelee()
    {
        EnemyInfo crawler = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        Assert.AreEqual(0, crawler.Range);
    }

    [Test]
    public void Crawler_IsHunting()
    {
        EnemyInfo crawler = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        Assert.IsTrue(crawler.IsHunting);
    }

    [Test]
    public void Launcher_IsNotHunting()
    {
        EnemyInfo launcher = new EnemyInfo((int)EnemyInfo.Tags.Launcher);
        Assert.IsFalse(launcher.IsHunting);
    }

    // --- Health ---

    [Test]
    public void DecreaseHealthBy_ReducesHealth()
    {
        EnemyInfo enemy = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        int initial = enemy.CurrentHealth;
        enemy.DecreaseHealthBy(1);
        Assert.AreEqual(initial - 1, enemy.CurrentHealth);
    }

    [Test]
    public void DecreaseHealthBy_CanGoNegative()
    {
        EnemyInfo enemy = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        enemy.DecreaseHealthBy(999);
        Assert.Less(enemy.CurrentHealth, 0);
    }

    // --- Energy ---

    [Test]
    public void DecrementEnergy_ReducesByOne()
    {
        EnemyInfo enemy = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        int initial = enemy.CurrentEnergy;
        enemy.DecrementEnergy();
        Assert.AreEqual(initial - 1, enemy.CurrentEnergy);
    }

    [Test]
    public void RestoreEnergy_RestoresToMax()
    {
        EnemyInfo enemy = new EnemyInfo((int)EnemyInfo.Tags.Launcher);
        int maxEnergy = enemy.CurrentEnergy;
        enemy.DecrementEnergy();
        enemy.DecrementEnergy();
        Assert.Less(enemy.CurrentEnergy, maxEnergy);

        enemy.RestoreEnergy();
        Assert.AreEqual(maxEnergy, enemy.CurrentEnergy);
    }

    // --- Stun ---

    [Test]
    public void IsStunned_DefaultsFalse()
    {
        EnemyInfo enemy = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        Assert.IsFalse(enemy.IsStunned);
    }

    [Test]
    public void IsStunned_CanBeSet()
    {
        EnemyInfo enemy = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        enemy.IsStunned = true;
        Assert.IsTrue(enemy.IsStunned);
        enemy.IsStunned = false;
        Assert.IsFalse(enemy.IsStunned);
    }

    // --- Launcher vs Crawler stats comparison ---

    [Test]
    public void Launcher_HasMoreHealthThanCrawler()
    {
        EnemyInfo crawler = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        EnemyInfo launcher = new EnemyInfo((int)EnemyInfo.Tags.Launcher);
        Assert.Greater(launcher.CurrentHealth, crawler.CurrentHealth);
    }

    [Test]
    public void Launcher_HasMoreEnergyThanCrawler()
    {
        EnemyInfo crawler = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        EnemyInfo launcher = new EnemyInfo((int)EnemyInfo.Tags.Launcher);
        Assert.Greater(launcher.CurrentEnergy, crawler.CurrentEnergy);
    }

    [Test]
    public void Launcher_HasMoreDamageThanCrawler()
    {
        EnemyInfo crawler = new EnemyInfo((int)EnemyInfo.Tags.Crawler);
        EnemyInfo launcher = new EnemyInfo((int)EnemyInfo.Tags.Launcher);
        Assert.Greater(launcher.DamagePoints, crawler.DamagePoints);
    }

    // --- All enemies have valid stats ---

    [Test]
    public void AllEnemies_HavePositiveHealth()
    {
        for (int i = 0; i < (int)EnemyInfo.Tags.Unknown; i++)
        {
            EnemyInfo enemy = new EnemyInfo(i);
            Assert.Greater(enemy.CurrentHealth, 0, $"Enemy {enemy.Tag} should have positive health");
        }
    }

    [Test]
    public void AllEnemies_HavePositiveEnergy()
    {
        for (int i = 0; i < (int)EnemyInfo.Tags.Unknown; i++)
        {
            EnemyInfo enemy = new EnemyInfo(i);
            Assert.Greater(enemy.CurrentEnergy, 0, $"Enemy {enemy.Tag} should have positive energy");
        }
    }

    [Test]
    public void AllEnemies_HavePositiveDamage()
    {
        for (int i = 0; i < (int)EnemyInfo.Tags.Unknown; i++)
        {
            EnemyInfo enemy = new EnemyInfo(i);
            Assert.Greater(enemy.DamagePoints, 0, $"Enemy {enemy.Tag} should have positive damage");
        }
    }
}
