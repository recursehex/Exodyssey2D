using NUnit.Framework;

public class ProfessionTests
{
    [Test]
    public void ProfessionList_ContainsSixEntries()
    {
        Assert.AreEqual(6, Profession.ProfessionList.Count);
    }

    [Test]
    public void AllProfessions_AreNotMasterByDefault()
    {
        foreach (Profession profession in Profession.ProfessionList)
            Assert.IsFalse(profession.IsMaster, $"{profession.Tag} should not be master by default");
    }

    [Test]
    public void ProfessionList_ContainsAllExpectedTags()
    {
        var expectedTags = new[]
        {
            Profession.Tags.Medic,
            Profession.Tags.Mechanic,
            Profession.Tags.Hunter,
            Profession.Tags.Hiker,
            Profession.Tags.Navigator,
            Profession.Tags.Ranger
        };

        foreach (var tag in expectedTags)
        {
            bool found = false;
            foreach (var profession in Profession.ProfessionList)
            {
                if (profession.Tag == tag)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, $"ProfessionList should contain {tag}");
        }
    }

    [Test]
    public void Constructor_SetsTagAndMaster()
    {
        Profession p = new(Profession.Tags.Hunter, true);
        Assert.AreEqual(Profession.Tags.Hunter, p.Tag);
        Assert.IsTrue(p.IsMaster);
    }

    [Test]
    public void IsMaster_CanBeSetToTrue()
    {
        Profession p = new(Profession.Tags.Medic, false);
        Assert.IsFalse(p.IsMaster);
        p.IsMaster = true;
        Assert.IsTrue(p.IsMaster);
    }

    [Test]
    public void GetRandomProfession_ReturnsValidProfession()
    {
        // Run multiple times to increase coverage
        for (int i = 0; i < 50; i++)
        {
            Profession p = Profession.GetRandomProfession();
            Assert.IsTrue(Profession.ProfessionList.Contains(p),
                $"Random profession {p.Tag} should be in ProfessionList");
        }
    }
}
