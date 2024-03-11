using System.Collections.Generic;

public struct Rarity 
{
	public enum RarityTag
	{
		Common = 0, // white
		Limited,    // green
		Scarce,     // yellow
		Rare,       // blue
		Anomalous,  // purple

		Unknown,
	}
	public RarityTag Tag { get; set; }
	public int Chance { get; set; }
	
	public static List<Rarity> rarities = new()
	{
		new() { Tag = RarityTag.Common, Chance = 35 },
		new() { Tag = RarityTag.Limited, Chance = 30 },
		new() { Tag = RarityTag.Scarce, Chance = 20 },
		new() { Tag = RarityTag.Rare, Chance = 10 },
		new() { Tag = RarityTag.Anomalous, Chance = 5 },
	};
}