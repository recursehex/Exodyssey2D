using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
		Boss,

		Unknown,
	}
	private int maxHealth = 1;
	private int maxEnergy = 1;
	public Tags Tag 			{ get; private set; } = Tags.Unknown;	// Name of enemy
	public Rarity Rarity 		{ get; private set; } = Rarity.Common;	// Rarity of enemy
	public Types Type 			{ get; private set; } = Types.Unknown;	// Type of enemy
	public string Name 			{ get; private set; }					// Ingame name of enemy
	public string Description 	{ get; private set; }					// Ingame description of enemy
	public int CurrentHealth 	{ get; private set; } = 1;				// Current health
	public int CurrentEnergy 	{ get; private set; } = 1;				// Current energy
	public int Speed 			{ get; private set; } = 2;				// Movement speed
	public int DamagePoints 	{ get; private set; } = -1;				// Set only for enemies that do direct attacks
	public int Range 			{ get; private set; } = 0;				// Maximum distance a ranged enemy can attack to, 0 = melee
	public bool IsHunting 		{ get; private set; } = true;			// true = will hunt the player, false = will guard nearby items
	public bool IsArmored 		{ get; private set; } = false;			// true = resistant to certain types of damage, false = not
	public bool IsStunned 		{ get; set; } = false;					// true = currently stunned, false = not
	[Serializable] private class Entry
	{
		public string Tag, Rarity, Type, Name, Description;
		public int maxHealth = 1, maxEnergy = 1, speed = 2, damagePoints = -1, range = 0;
		public bool isHunting = true, isArmored = false, disabled = false;
	}
	[Serializable] private class EntryList { public List<Entry> Enemies; }
	private static readonly int lastEnemyIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> EnemyRarityList = GenerateAll(
		Entry => Rarity.Parse(Entry.Rarity), i => new EnemyInfo(i).Rarity);
	private static readonly List<Types> EnemyTypeList = GenerateAll(
		Entry => Enum.TryParse(Entry.Type, out Types Type) ? Type : Types.Unknown, i => new EnemyInfo(i).Type);
	private static List<Entry> Database;
	private static string LastMissingTypeLogKey;
	private static void LoadDatabase()
	{
		if (Database != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/EnemyDefinitions");
		if (JsonFile != null)
			Database = JsonUtility.FromJson<EntryList>(JsonFile.text).Enemies;
		else
			Debug.LogError("EnemyDefinitions.json not found in Resources folder!");
	}
	private static List<T> GenerateAll<T>(Func<Entry, T> Extract, Func<int, T> Fallback)
	{
		LoadDatabase();
		if (Database == null)
		{
			Debug.LogWarning("Database failed to load, returning empty list");
			return new();
		}
		List<T> Result = new();
		for (int i = 0; i < lastEnemyIndex; i++)
		{
			string TagName = ((Tags)i).ToString();
			Entry Entry = Database.Find(Entry => Entry.Tag == TagName);
			if (Entry != null && !Entry.disabled)
				Result.Add(Extract(Entry));
			else if (Entry == null)
				Result.Add(Fallback(i));
		}
		return Result;
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
	public void RestoreEnergy() => CurrentEnergy = maxEnergy;
	/// <summary>
	/// Returns info for a desired enemy,
	/// index must match Tag order and GameManager EnemyTemplates order
	/// </summary>
	public EnemyInfo(int index)
	{
		LoadDatabase();
		Tags TagData = (Tags)index;
		string TagName = TagData.ToString();
		if (Database != null)
		{
			Entry Entry = Database.Find(Entry => Entry.Tag == TagName);
			if (Entry != null && !Entry.disabled)
			{
				LoadFrom(Entry);
				CurrentHealth = maxHealth;
				CurrentEnergy = maxEnergy;
				return;
			}
			else if (Entry != null && Entry.disabled)
				Debug.LogWarning($"Enemy {index} {TagName} is disabled in JSON");
		}
		// Fallback to hardcoded values if JSON loading fails
		Debug.LogWarning($"Enemy {TagName} not found in JSON, using default values");
		Tag 			= TagData;
		Name 			= TagName.ToUpper();
		Description 	= "Unknown enemy";
		CurrentHealth 	= maxHealth;
		CurrentEnergy 	= maxEnergy;
	}
	private void LoadFrom(Entry Source)
	{
		Name 			= Source.Name;
		Description 	= Source.Description;
		maxHealth 		= Source.maxHealth;
		maxEnergy 		= Source.maxEnergy;
		Speed 			= Source.speed;
		DamagePoints 	= Source.damagePoints;
		Range 			= Source.range;
		IsHunting 		= Source.isHunting;
		IsArmored 		= Source.isArmored;
		Tag 	= Enum.TryParse(Source.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
		Rarity 	= Rarity.Parse(Source.Rarity);
		Type 	= Enum.TryParse(Source.Type, out Types ParsedType) ? ParsedType : Types.Unknown;
	}
}
