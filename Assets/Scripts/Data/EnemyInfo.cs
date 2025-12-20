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
	private static readonly List<Types> EnemyTypeList = GenerateAllTypes();
	private static EnemyDatabase EnemyDatabase;
	private static string LastMissingTypeLogKey;
	private static void LoadDatabase()
	{
		if (EnemyDatabase != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/EnemyDefinitions");
		if (JsonFile != null)
			EnemyDatabase = JsonUtility.FromJson<EnemyDatabase>(JsonFile.text);
		else
			Debug.LogError("EnemyDefinitions.json not found in Resources folder!");
	}
	private static List<Rarity> GenerateAllRarities()
	{
		LoadDatabase();
		List<Rarity> Rarities = new();
		// First, try to get rarities from JSON
		if (EnemyDatabase != null
		 && EnemyDatabase.Enemies != null)
		{
			for (int i = 0; i < lastEnemyIndex; i++)
			{
				Tags Tag = (Tags)i;
				string TagName = Tag.ToString();
				EnemyData Data = EnemyDatabase.Enemies.Find(Enemy => Enemy.Tag == TagName);
				if (Data != null && !Data.disabled)
					Rarities.Add(Rarity.Parse(Data.Rarity));
				// Skip disabled items
				else if (Data != null && Data.disabled)
					continue;
				// Fallback to creating EnemyInfo if not found in JSON
				else
					Rarities.Add(new EnemyInfo(i).Rarity);
			}
		}
		else
		{
			Debug.LogWarning($"Database failed to load, returning empty list");
		}
		return Rarities;
	}
	private static List<Types> GenerateAllTypes()
	{
		LoadDatabase();
		List<Types> TypeList = new();
		// First, try to get types from JSON
		if (EnemyDatabase != null
		 && EnemyDatabase.Enemies != null)
		{
			for (int i = 0; i < lastEnemyIndex; i++)
			{
				Tags Tag = (Tags)i;
				string TagName = Tag.ToString();
				EnemyData Data = EnemyDatabase.Enemies.Find(Enemy => Enemy.Tag == TagName);
				if (Data != null && !Data.disabled)
				{
					if (Enum.TryParse(Data.Type, out Types ParsedType))
						TypeList.Add(ParsedType);
					else
						TypeList.Add(Types.Unknown);
				}
				// Skip disabled items
				else if (Data != null && Data.disabled)
					continue;
				// Fallback to creating EnemyInfo if not found in JSON
				else
					TypeList.Add(new EnemyInfo(i).Type);
			}
		}
		else
		{
			Debug.LogWarning($"Database failed to load, returning empty list");
		}
		return TypeList;
	}
	/// <summary>
	/// Gets list of allowed types based on current region's enemy pool
	/// </summary>
	public static List<Types> GetAllowedTypes()
	{
        List<string> AllowedTypeNames = RegionManager.CurrentRegion.EnemyPool;
		// No region filtering, return all types
		if (AllowedTypeNames == null || AllowedTypeNames.Count == 0)
			return new List<Types> { Types.Weak, Types.Mediocre, Types.Strong, Types.Exotic };
		// Convert type names to Types
		HashSet<Types> AllowedTypes = new();
		foreach (string TypeName in AllowedTypeNames)
		{
			if (Enum.TryParse(TypeName, out Types ParsedType))
				AllowedTypes.Add(ParsedType);
			else
				Debug.LogWarning($"Unknown enemy type: {TypeName}");
		}
		return new List<Types>(AllowedTypes);
	}
	/// <summary>
	/// Gets list of allowed rarities based on allowed enemy types
	/// </summary>
	public static List<Rarity> GetAllowedRarities()
	{
		RegionInfo.Tags RegionTag = RegionManager.CurrentRegion.Tag;
		List<Types> AllowedTypes = GetAllowedTypes();
		if (AllowedTypes == null || AllowedTypes.Count == 0)
			return new List<Rarity>(Rarity.RarityList);
		HashSet<Types> AllowedTypeSet = new(AllowedTypes);
		HashSet<Rarity> AllowedRarities = new();
		int Count = Mathf.Min(EnemyRarityList.Count, EnemyTypeList.Count);
		int eligibleEnemyCount = 0;
		for (int i = 0; i < Count; i++)
		{
			if (AllowedTypeSet.Contains(EnemyTypeList[i]))
			{
				AllowedRarities.Add(EnemyRarityList[i]);
				eligibleEnemyCount++;
			}
		}
		List<Types> OrderedTypes = new(AllowedTypes);
		OrderedTypes.Sort();
		string allowedTypesString = string.Join(", ", OrderedTypes);
		string logKey = $"{RegionTag}:{allowedTypesString}";
		// Log if no eligible enemies found for the region
		if (eligibleEnemyCount == 0 && logKey != LastMissingTypeLogKey)
		{
			Debug.Log($"No enemy definitions match region {RegionTag} allowed types [{allowedTypesString}].");
			LastMissingTypeLogKey = logKey;
		}
		return new List<Rarity>(AllowedRarities);
	}
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		List<Types> AllowedTypes = GetAllowedTypes();
		HashSet<Types> AllowedTypeSet = AllowedTypes == null || AllowedTypes.Count == 0
			? null
			: new(AllowedTypes);
		int Count = Mathf.Min(EnemyRarityList.Count, EnemyTypeList.Count);
		List<int> Indices = Enumerable.Range(0, Count)
									  .Where(i => EnemyRarityList[i] == Rarity
											&& (AllowedTypeSet == null
											|| AllowedTypeSet.Contains(EnemyTypeList[i])))
									  .ToList();
		if (Indices.Count == 0)
			return -1;
		return Indices[UnityEngine.Random.Range(0, Indices.Count)];
	}
	/// <summary>
	/// Decreases CurrentHealth by 1
	/// </summary>
	public void DecreaseHealthBy(int amount) => CurrentHealth -= amount;
	/// <summary>
	/// Decreases CurrentEnergy by 1
	/// </summary>
	public void DecrementEnergy() => CurrentEnergy--;
	/// <summary>
	/// Restores enemy's CurrentEnergy to maxEnergy
	/// </summary>
	public void RestoreEnergy() => CurrentEnergy = Data.maxEnergy;
	/// <summary>
	/// Returns info for a desired enemy,
	/// index must match Tag order and GameManager EnemyTemplates order
	/// </summary>
	public EnemyInfo(int index)
	{
		LoadDatabase();
		Tags TagData = (Tags)index;
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
				Debug.LogWarning($"Enemy {index} {TagName} is disabled in JSON");
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
	/// <summary>
	/// Loads enemy data from source EnemyData object
	/// </summary>
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
		Tag 	= Enum.TryParse(Data.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
		Rarity 	= Rarity.Parse(Data.Rarity);
		Type 	= Enum.TryParse(Data.Type, out Types ParsedType) ? ParsedType : Types.Unknown;
	}
}
