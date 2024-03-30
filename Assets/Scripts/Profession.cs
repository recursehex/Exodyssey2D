using System.Collections.Generic;

public struct Profession
{
	public enum ProfessionTag 
	{
		Medic = 0,
		Mechanic,
		Hunter,
		Hiker,
		Navigator,
		Ranger,
		Unknown
	}
	public ProfessionTag Tag { get; set; }
	public int Level { get; set; }
	
	public static List<Profession> professions = new()
	{
		new() { Tag = ProfessionTag.Medic, Level = 1 },
		new() { Tag = ProfessionTag.Mechanic, Level = 1 },
		new() { Tag = ProfessionTag.Hunter, Level = 1 },
		new() { Tag = ProfessionTag.Hiker, Level = 1 },
		new() { Tag = ProfessionTag.Navigator, Level = 1 },
		new() { Tag = ProfessionTag.Ranger, Level = 1 },
	};
}