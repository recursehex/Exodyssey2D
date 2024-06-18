using System.Collections.Generic;
using UnityEngine;
using Exodyssey.Rarity;

public class EnemyInfo
{
	private enum EnemyTag
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
	private enum EnemyType
	{
		Weak = 0,
		Mediocre,
		Strong,
		Exotic,

		Unknown,
	}
	// Name of enemy
	private EnemyTag tag = EnemyTag.Unknown;
	// Rarity of enemy
	private Rarity rarity = Rarity.Common;
	// Type of enemy
	private EnemyType type = EnemyType.Unknown;
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
	private static readonly int lastEnemyIndex = (int)EnemyTag.Unknown;
	private static List<Rarity> GenerateAllRarities()
	{
		List<Rarity> enemyRarityList = new();
		for (int i = 0; i < lastEnemyIndex; i++)
		{
			EnemyInfo enemy = EnemyFactory(i);
			enemyRarityList.Add(enemy.rarity);
		}
		return enemyRarityList;
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity specifiedRarity)
	{
		List<Rarity> enemyRarityList = GenerateAllRarities();
		List<int> indicesOfSpecifiedRarity = new();
		for (int i = 0; i < enemyRarityList.Count; i++)
		{
			if (enemyRarityList[i] == specifiedRarity) 
			{
				indicesOfSpecifiedRarity.Add(i);
			}
		}
		if (indicesOfSpecifiedRarity.Count == 0)
		{
			return -1;
		}
		int randomIndex = Random.Range(0, indicesOfSpecifiedRarity.Count);
		return indicesOfSpecifiedRarity[randomIndex];
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
				info.tag = EnemyTag.Crawler;
				info.rarity = Rarity.Common;
				info.type = EnemyType.Weak;
				info.maxHealth = 2;
				info.currentHealth = info.maxHealth;
				info.maxEnergy = 1;
				info.currentEnergy = info.maxEnergy;
				info.damagePoints = 1;
				info.name = "CRAWLER";
				info.isHunting = true;
				break;
			case 1:
				info.tag = EnemyTag.Launcher;
				info.rarity = Rarity.Limited;
				info.type = EnemyType.Mediocre;
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
