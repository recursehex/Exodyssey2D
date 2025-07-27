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
		Ranger
	}
	public Tags Tag;
	public bool IsMaster { get; set; }
	public Profession(Tags Tag, bool IsMaster)
	{
		this.Tag 		= Tag;
		this.IsMaster 	= IsMaster;
	}
	public static readonly Profession Medic 	= new(Tags.Medic, 		false);
    public static readonly Profession Mechanic 	= new(Tags.Mechanic, 	false);
    public static readonly Profession Hunter 	= new(Tags.Hunter, 		false);
    public static readonly Profession Hiker 	= new(Tags.Hiker, 		false);
    public static readonly Profession Navigator = new(Tags.Navigator, 	false);
	public static readonly Profession Ranger 	= new(Tags.Ranger, 		false);
	public static readonly List<Profession> ProfessionList = new() { Medic, Mechanic, Hunter, Hiker, Navigator, Ranger };
	public static Profession GetRandomProfession() => ProfessionList[Random.Range(0, ProfessionList.Count)];
}