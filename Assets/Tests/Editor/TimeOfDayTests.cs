using NUnit.Framework;

/// <summary>
/// Tests the time-of-day progression formula used by LevelManager.
/// The actual LevelManager is a MonoBehaviour, so we test the pure math here.
/// </summary>
public class TimeOfDayTests
{
    // LevelManager uses: CurrentTimeOfDay = (TimeOfDay)(Level % 5)
    // Day increments: if (Level % 5 == 0) Day++

    private static LevelManager.TimeOfDay GetTimeOfDay(int level)
    {
        return (LevelManager.TimeOfDay)(level % 5);
    }

    private static int GetDay(int level)
    {
        // Day starts at 1, increments every 5 levels
        return (level / 5) + 1;
    }

    [Test]
    public void Level0_IsDawn()
    {
        Assert.AreEqual(LevelManager.TimeOfDay.Dawn, GetTimeOfDay(0));
    }

    [Test]
    public void Level1_IsNoon()
    {
        Assert.AreEqual(LevelManager.TimeOfDay.Noon, GetTimeOfDay(1));
    }

    [Test]
    public void Level2_IsAfternoon()
    {
        Assert.AreEqual(LevelManager.TimeOfDay.Afternoon, GetTimeOfDay(2));
    }

    [Test]
    public void Level3_IsDusk()
    {
        Assert.AreEqual(LevelManager.TimeOfDay.Dusk, GetTimeOfDay(3));
    }

    [Test]
    public void Level4_IsNight()
    {
        Assert.AreEqual(LevelManager.TimeOfDay.Night, GetTimeOfDay(4));
    }

    [Test]
    public void Level5_CyclesBackToDawn()
    {
        Assert.AreEqual(LevelManager.TimeOfDay.Dawn, GetTimeOfDay(5));
    }

    [Test]
    public void FullCycle_RepeatsCorrectly()
    {
        var expected = new[]
        {
            LevelManager.TimeOfDay.Dawn,
            LevelManager.TimeOfDay.Noon,
            LevelManager.TimeOfDay.Afternoon,
            LevelManager.TimeOfDay.Dusk,
            LevelManager.TimeOfDay.Night
        };

        for (int cycle = 0; cycle < 3; cycle++)
        {
            for (int i = 0; i < 5; i++)
            {
                int level = cycle * 5 + i;
                Assert.AreEqual(expected[i], GetTimeOfDay(level),
                    $"Level {level} should be {expected[i]}");
            }
        }
    }

    [Test]
    public void Day_StartsAtOne()
    {
        Assert.AreEqual(1, GetDay(0));
    }

    [Test]
    public void Day_IncrementsEveryFiveLevels()
    {
        Assert.AreEqual(1, GetDay(0));
        Assert.AreEqual(1, GetDay(4));
        Assert.AreEqual(2, GetDay(5));
        Assert.AreEqual(2, GetDay(9));
        Assert.AreEqual(3, GetDay(10));
    }

    [Test]
    public void NightTime_OccursOncePerDay()
    {
        int nightCount = 0;
        for (int level = 0; level < 15; level++)
        {
            if (GetTimeOfDay(level) == LevelManager.TimeOfDay.Night)
                nightCount++;
        }
        Assert.AreEqual(3, nightCount); // 3 full day cycles in 15 levels
    }
}
