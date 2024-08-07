using System.Collections.Generic;
using UnityEngine;
using Exodyssey.Rarity;

public class EnemyInfo
{
	private enum Tags
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
	private enum Types
	{
		Weak = 0,
		Mediocre,
		Strong,
		Exotic,

		Unknown,
	}
	// Name of enemy
	private Tags Tag = Tags.Unknown;
	// Rarity of enemy
	private Rarity Rarity = Rarity.Common;
	// Type of enemy
	private Types Type = Types.Unknown;
	// Ingame name of enemy
	public string name = "UNKNOWN";
	// Maximum health
	public int maxHealth = 1;
	// Current health
	public int currentHealth = 1;
	// Maximum energy
	public int maxEnergy = 1;
	// Current energy
	public int currentEnergy = 1;
	// Set only for enemies that do direct attacks
	public int damagePoints = -1;
	// Maximum distance a ranged enemy can attack to, 0 = melee
	public int range = 0;
	// true = will hunt the player, false = will guard nearby items
	public bool isHunting = true;
	// true = resistant to certain types of damage, false = not
	public bool isArmored = false;
	// true = currently stunned, false = not
	public bool isStunned = false;
	private static readonly int lastEnemyIndex = (int)Tags.Unknown;
	private static List<Rarity> GenerateAllRarities()
	{
		List<Rarity> EnemyRarityList = new();
		for (int i = 0; i < lastEnemyIndex; i++)
		{
			EnemyInfo Enemy = EnemyFactory(i);
			EnemyRarityList.Add(Enemy.Rarity);
		}
		return EnemyRarityList;
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity SpecifiedRarity)
	{
		List<Rarity> EnemyRarityList = GenerateAllRarities();
		List<int> IndicesOfSpecifiedRarity = new();
		for (int i = 0; i < EnemyRarityList.Count; i++)
		{
			if (EnemyRarityList[i] == SpecifiedRarity) 
			{
				IndicesOfSpecifiedRarity.Add(i);
			}
		}
		if (IndicesOfSpecifiedRarity.Count == 0)
		{
			return -1;
		}
		int randomIndex = Random.Range(0, IndicesOfSpecifiedRarity.Count);
		return IndicesOfSpecifiedRarity[randomIndex];
	}
	/// <summary>
	/// Returns info for a desired enemy 
	/// </summary>
	/// <param name="n"></param>
	/// <returns></returns>
	public static EnemyInfo EnemyFactory(int n)
	{
		EnemyInfo info = new();
		switch (n)
		{
			case 0:
				info.Tag = Tags.Crawler;
				info.Rarity = Rarity.Common;
				info.Type = Types.Weak;
				info.maxHealth = 2;
				info.currentHealth = info.maxHealth;
				info.maxEnergy = 1;
				info.currentEnergy = info.maxEnergy;
				info.damagePoints = 1;
				info.name = "CRAWLER";
				info.isHunting = true;
				break;
			case 1:
				info.Tag = Tags.Launcher;
				info.Rarity = Rarity.Limited;
				info.Type = Types.Mediocre;
				info.maxHealth = 4;
				info.currentHealth = info.maxHealth;
				info.maxEnergy = 2;
				info.currentEnergy = info.maxEnergy;
				info.damagePoints = 2;
				info.range = 3;
				info.name = "LAUNCHER";
				info.isHunting = false;
				break;
		}
		return info;
	}
}
