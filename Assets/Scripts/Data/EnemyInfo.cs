using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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
	private EnemyData Data = new();										// Internal data
	public Tags Tag 			{ get; private set; } = Tags.Unknown;	// Name of enemy
	public Rarity Rarity 		{ get; private set; } = Rarity.Common;	// Rarity of enemy
	public Types Type 			{ get; private set; } = Types.Unknown;	// Type of enemy
	public string Name 			=> Data.Name;							// Ingame name of enemy
	public string Description 	=> Data.Description;					// Ingame description of enemy
	public int CurrentHealth 	{ get; private set; } = 1;				// Current health
	public int CurrentEnergy 	{ get; private set; } = 1;				// Current energy
	public int Speed 			=> Data.speed;							// Movement speed
	public int DamagePoints 	=> Data.damagePoints;					// Set only for enemies that do direct attacks
	public int Range 			=> Data.range;							// Maximum distance a ranged enemy can attack to, 0 = melee
	public bool IsHunting 		=> Data.isHunting;						// true = will hunt the player, false = will guard nearby items
	public bool IsArmored 		=> Data.isArmored;						// true = resistant to certain types of damage, false = not
	public bool IsStunned 		{ get; set; } = false;					// true = currently stunned, false = not
	private static readonly int lastEnemyIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> EnemyRarityList = GenerateAllRarities();
	private static EnemyDatabase EnemyDatabase;
	private static bool databaseLoaded = false;
	private static void LoadDatabase()
	{
		if (databaseLoaded)
		{
			return;
		}
		TextAsset jsonFile = Resources.Load<TextAsset>("EnemyDefinitions");
		if (jsonFile != null)
		{
			EnemyDatabase = JsonUtility.FromJson<EnemyDatabase>(jsonFile.text);
			databaseLoaded = true;
		}
		else
		{
			Debug.LogError("EnemyDefinitions.json not found in Resources folder!");
		}
	}
	
	private static List<Rarity> GenerateAllRarities()
	{
		LoadDatabase();
		List<Rarity> rarities = new();
		// First, try to get rarities from JSON
		if (EnemyDatabase != null
		 && EnemyDatabase.Enemies != null)
		{
			for (int i = 0; i < lastEnemyIndex; i++)
			{
				Tags tag = (Tags)i;
				string tagName = tag.ToString();
				EnemyData data = EnemyDatabase.Enemies.Find(enemy => enemy.Tag == tagName);
				if (data != null && !data.disabled)
				{
					Rarity parsedRarity = Rarity.Parse(data.Rarity);
					rarities.Add(parsedRarity);
				}
				else if (data != null && data.disabled)
				{
					// Skip disabled items
					continue;
				}
				else
				{
					// Fallback to creating EnemyInfo if not found in JSON
					rarities.Add(new EnemyInfo(i).Rarity);
				}
			}
		}
		else
		{
			Debug.LogWarning($"Database failed to load, returning empty list");
		}
		return rarities;
	}
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		List<int> indices = Enumerable.Range(0, EnemyRarityList.Count)
									  .Where(i => EnemyRarityList[i] == Rarity)
									  .ToList();
		if (indices.Count == 0)
			return -1;
		return indices[UnityEngine.Random.Range(0, indices.Count)];
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
		CurrentEnergy = Data.maxEnergy;
	}
	/// <summary>
	/// Returns info for a desired enemy,
	/// n must match Tag order and GameManager EnemyTemplates order
	/// </summary>
	public EnemyInfo(int n)
	{
		LoadDatabase();
		
		Tags TagData = (Tags)n;
		string TagName = TagData.ToString();
		
		// Try to load from JSON first
		if (EnemyDatabase != null
		 && EnemyDatabase.Enemies != null)
		{
			EnemyData Data = EnemyDatabase.Enemies.Find(Enemy => Enemy.Tag == TagName);
			if (Data != null && !Data.disabled)
			{
				LoadFromData(Data);
				CurrentHealth = Data.maxHealth;
				CurrentEnergy = Data.maxEnergy;
				return;
			}
			else if (Data != null && Data.disabled)
			{
				Debug.LogWarning($"Enemy {n} {TagName} is disabled in JSON");
			}
		}
		
		// Fallback to hardcoded values if JSON loading fails
		Debug.LogWarning($"Enemy {TagName} not found in JSON, using default values");
		
		// Set minimal defaults for unknown enemies
		Tag 				= TagData;
		Rarity 				= Rarity.Common;
		Type 				= Types.Unknown;
		Data.Tag 			= TagName;
		Data.Name 			= TagName.ToUpper();
		Data.Description 	= "Unknown enemy";
		Data.maxHealth 		= 1;
		Data.maxEnergy 		= 1;
		Data.speed 			= 2;
		CurrentHealth 		= Data.maxHealth;
		CurrentEnergy 		= Data.maxEnergy;
	}
	
	private void LoadFromData(EnemyData SourceData)
	{
		// Copy the data
		Data = new EnemyData
		{
			Tag 			= SourceData.Tag,
			Rarity 			= SourceData.Rarity,
			Type 			= SourceData.Type,
			Name 			= SourceData.Name,
			Description 	= SourceData.Description,
			maxHealth 		= SourceData.maxHealth,
			maxEnergy 		= SourceData.maxEnergy,
			speed 			= SourceData.speed,
			damagePoints 	= SourceData.damagePoints,
			range 			= SourceData.range,
			isHunting 		= SourceData.isHunting,
			isArmored 		= SourceData.isArmored
		};
		
		// Parse enums
		if (Enum.TryParse(Data.Tag, out Tags ParsedTag))
			Tag = ParsedTag;
		else
			Tag = Tags.Unknown;
			
		Rarity = Rarity.Parse(Data.Rarity);
			
		if (Enum.TryParse(Data.Type, out Types ParsedType))
			Type = ParsedType;
		else
			Type = Types.Unknown;
	}
}
