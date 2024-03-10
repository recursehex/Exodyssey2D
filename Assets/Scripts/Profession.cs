using System.Collections.Generic;
using UnityEngine;

public class Profession
{
	private enum ProfessionTag 
	{
		Medic = 0,
		Mechanic,
		Hunter,
		Hiker,
		Navigator,
		Ranger,
		Unknown
	}
	
	private int Level { get; set; } = 1;	// Level of item from 1 to 3
	
	public static Profession ProfessionFactory(int n, Player player) 
	{
		Profession info = new();
		switch (n) 
		{
			case 0:
				break;
			default:
				break;
		}
		return info;
	}
}