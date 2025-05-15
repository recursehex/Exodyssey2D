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
	public Tags Tag 			= Tags.Unknown;					// Name of enemy
	public Rarity Rarity 		= Rarity.Common;				// Rarity of enemy
	public Types Type 			= Types.Unknown;				// Type of enemy
	public string Name 			{ get; private set; }			// Ingame name of enemy
	public string Description 	{ get; private set; }			// Ingame description of enemy
	private readonly int maxHealth = 1;							// Maximum health
	public int CurrentHealth 	{ get; private set; } = 1;		// Current health
	private readonly int maxEnergy = 1;							// Maximum energy
	public int CurrentEnergy 	{ get; private set; } = 1;		// Current energy
	public int Speed 			{ get; private set; } = 2;		// Movement speed
	public int DamagePoints 	{ get; private set; } = -1;		// Set only for enemies that do direct attacks
	public int Range 			{ get; private set; } = 0;		// Maximum distance a ranged enemy can attack to, 0 = melee
	public bool IsHunting 		{ get; private set; } = true;	// true = will hunt the player, false = will guard nearby items
	public bool IsArmored 		{ get; private set; } = false;	// true = resistant to certain types of damage, false = not
	public bool IsStunned 		{ get; set; } 		  = false;	// true = currently stunned, false = not
	private static readonly int lastEnemyIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> EnemyRarityList = GenerateAllRarities();
	private static List<Rarity> GenerateAllRarities()
	{
		return Enumerable.Range(0, lastEnemyIndex)
						 .Select(i => new EnemyInfo(i).Rarity)
						 .ToList();
	}
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		var indices = Enumerable.Range(0, EnemyRarityList.Count)
								.Where(i => EnemyRarityList[i] == Rarity)
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
	/// Returns info for a desired enemy,
	/// n must match Tag order and GameManager EnemyTemplates order
	/// </summary>
	public EnemyInfo(int n)
	{
		switch (n)
		{
			case 0:
				Tag 			= Tags.Crawler;
				Rarity 			= Rarity.Common;
				Type 			= Types.Weak;
				maxHealth 		= 2;
				CurrentHealth 	= maxHealth;
				maxEnergy 		= 1;
				CurrentEnergy 	= maxEnergy;
				Speed 			= 3;
				DamagePoints 	= 1;
				Name 			= "CRAWLER";
				IsHunting 		= true;
				break;
			case 1:
				Tag 			= Tags.Launcher;
				Rarity 			= Rarity.Limited;
				Type 			= Types.Mediocre;
				maxHealth 		= 4;
				CurrentHealth 	= maxHealth;
				maxEnergy 		= 2;
				CurrentEnergy 	= maxEnergy;
				DamagePoints 	= 2;
				Range 			= 3;
				Name 			= "LAUNCHER";
				IsHunting 		= false;
				break;
		}
	}
}
