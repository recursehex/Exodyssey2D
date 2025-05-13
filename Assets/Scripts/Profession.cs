using System.Collections.Generic;
using UnityEngine;

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
	public Tags Tag;
	public int Level { get; set; }
	public Profession(Tags Tag, int Level)
	{
		this.Tag = Tag;
		this.Level = Level;
	}
	public static readonly Profession Medic 	= new(Tags.Medic, 		0);
    public static readonly Profession Mechanic 	= new(Tags.Mechanic, 	0);
    public static readonly Profession Hunter 	= new(Tags.Hunter, 		0);
    public static readonly Profession Hiker 	= new(Tags.Hiker, 		0);
    public static readonly Profession Navigator = new(Tags.Navigator, 	0);
	public static readonly Profession Ranger 	= new(Tags.Ranger, 		0);
	public static readonly List<Profession> ProfessionList = new() { Medic, Mechanic, Hunter, Hiker, Navigator, Ranger };
	public static Profession GetRandomProfession() => ProfessionList[Random.Range(0, ProfessionList.Count)];
}