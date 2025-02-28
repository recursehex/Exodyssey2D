using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyInfo
{
	public enum Tags
	{
		// WEAK
		Crawler = 0,

		// MEDIOCRE
		//Spawner,
		Launcher,

		// STRONG
		//Beast,

		// EXOTIC
		//OvergrownScanner,

		Unknown,
	}
	public enum Types
	{
		Weak = 0,
		Mediocre,
		Strong,
		Exotic,

		Unknown,
	}
	// Name of enemy
	public Tags Tag = Tags.Unknown;
	// Rarity of enemy
	private Rarity Rarity = Rarity.Common;
	// Type of enemy
	private Types Type = Types.Unknown;
	// Ingame name of enemy
	public string Name { get; private set; } = "UNKNOWN";
	// Maximum health
	private int maxHealth = 1;
	// Current health
	public int CurrentHealth { get; private set; } = 1;
	// Maximum energy
	private int maxEnergy = 1;
	// Current energy
	public int CurrentEnergy { get; private set; } = 1;
	// Set only for enemies that do direct attacks
	public int DamagePoints { get; private set; } = -1;
	// Maximum distance a ranged enemy can attack to, 0 = melee
	public int Range { get; private set; } = 0;
	// true = will hunt the player, false = will guard nearby items
	public bool IsHunting { get; private set; } = true;
	// true = resistant to certain types of damage, false = not
	public bool IsArmored { get; private set; } = false;
	// true = currently stunned, false = not
	public bool IsStunned { get; set; } = false;
	private static readonly int lastEnemyIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> EnemyRarityList = GenerateAllRarities();
	private static List<Rarity> GenerateAllRarities()
	{
		return Enumerable.Range(0, lastEnemyIndex)
						.Select(i => EnemyFactory(i).Rarity)
						.ToList();
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity SpecifiedRarity)
	{
		var indices = Enumerable.Range(0, EnemyRarityList.Count)
								.Where(i => EnemyRarityList[i] == SpecifiedRarity)
								.ToList();
		if (indices.Count == 0)
			return -1;
		return indices[Random.Range(0, indices.Count)];
	}
	/// <summary>
	/// Decreases CurrentHealth by 1
	/// </summary>
	public void DecreaseHealthBy(int amount)
	{
		CurrentHealth -= amount;
	}
	/// <summary>
	/// Decreases CurrentEnergy by 1
	/// </summary>
	public void DecrementEnergy()
	{
		CurrentEnergy--;
	}
	/// <summary>
	/// Restores enemy's CurrentEnergy to maxEnergy
	/// </summary>
	public void RestoreEnergy()
	{
		CurrentEnergy = maxEnergy;
	}
	/// <summary>
	/// Returns info for a desired enemy 
	/// </summary>
	public static EnemyInfo EnemyFactory(int n)
	{
		EnemyInfo Info = new();
		switch (n)
		{
			case 0:
				Info.Tag 			= Tags.Crawler;
				Info.Rarity 		= Rarity.Common;
				Info.Type 			= Types.Weak;
				Info.maxHealth 		= 2;
				Info.CurrentHealth 	= Info.maxHealth;
				Info.maxEnergy 		= 1;
				Info.CurrentEnergy 	= Info.maxEnergy;
				Info.DamagePoints 	= 1;
				Info.Name 			= "CRAWLER";
				Info.IsHunting 		= true;
				break;
			case 1:
				Info.Tag 			= Tags.Launcher;
				Info.Rarity 		= Rarity.Limited;
				Info.Type 			= Types.Mediocre;
				Info.maxHealth 		= 4;
				Info.CurrentHealth 	= Info.maxHealth;
				Info.maxEnergy 		= 2;
				Info.CurrentEnergy 	= Info.maxEnergy;
				Info.DamagePoints 	= 2;
				Info.Range 			= 3;
				Info.Name 			= "LAUNCHER";
				Info.IsHunting 		= false;
				break;
		}
		return Info;
	}
}
