using System.Collections.Generic;
using UnityEngine;

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
	private EnemyTag tag;						// Name of enemy
	private Rarity.RarityTag rarity;			// Rarity of enemy
	private EnemyType type;						// Type of enemy
	public string name;                         // Ingame name of enemy
	public int maxHealth = 1;
	public int currentHealth = 1;
	public int maxEnergy = 1;
	public int currentEnergy = 1;
	public int damagePoints = -1;               // Set only for enemies that do direct attacks
	public int range = 0;                       // Maximum distance a Ranged enemy can attack to, 0 = Melee
	public bool isHunting = true;               // true = will hunt the player, false = will guard nearby items
	public bool isShelled = false;              // false = will not have resistance to Steel Beam and Mallet, true = will have resistance and will be vulnerable to Axe, Honed Gavel, and Shell Piercer
	private static readonly int lastEnemyIndex = (int)EnemyTag.Unknown;
	private static List<Rarity.RarityTag> GenerateAllRarities()
	{
		List<Rarity.RarityTag> enemyRarityList = new();
		for (int i = 0; i < lastEnemyIndex; i++)
		{
			EnemyInfo enemy = EnemyFactory(i);
			enemyRarityList.Add(enemy.rarity);
		}
		return enemyRarityList;
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity.RarityTag specifiedRarity)
	{
		List<Rarity.RarityTag> enemyRarityList = GenerateAllRarities();
		List<int> indicesOfSpecifiedRarity = new();
		for (int i = 0; i < enemyRarityList.Count; i++)
		{
			if (enemyRarityList[i] == specifiedRarity) indicesOfSpecifiedRarity.Add(i);
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
				info.rarity = Rarity.RarityTag.Common;
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
				info.rarity = Rarity.RarityTag.Limited;
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
