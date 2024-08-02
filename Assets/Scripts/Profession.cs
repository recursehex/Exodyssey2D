using System.Collections.Generic;

public struct Profession
{
	public enum Tags
	{
		Medic = 0,
		Mechanic,
		Hunter,
		Hiker,
		Navigator,
		Ranger,
		Unknown
	}
	public Tags Tag { get; set; }
	public int Level { get; set; }
	public Profession(Tags Tag, int Level)
	{
		this.Tag = Tag;
		this.Level = Level;
	}
	public static List<Profession> professions = new()
	{
		new() { Tag = Tags.Medic, Level = 1 },
		new() { Tag = Tags.Mechanic, Level = 1 },
		new() { Tag = Tags.Hunter, Level = 1 },
		new() { Tag = Tags.Hiker, Level = 1 },
		new() { Tag = Tags.Navigator, Level = 1 },
		new() { Tag = Tags.Ranger, Level = 1 },
	};
}