using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Contains all item variables, creates items with specific values, and manages items after usage
/// </summary>
public class ItemInfo
{
	/// <summary>
	/// Contains identifying name for a unique item, usually matches Name
	/// </summary>
	public enum Tags
	{
		// CONSUMABLE
		MedKit = 0,
		ToolKit,
		PowerCell,

		// MELEE
		Branch,
		Knife,
		Wrench,
		Mallet,
		FireAxe,
		Chainsaw,

		// RANGED
		Tranquilizer,
		Carbine,
		Flamethrower,
		HuntingRifle,
		PlasmaRailgun,

		// THROWABLE
		Rock,
		//SmokeGrenade,
		Dynamite,
		//StickyGrenade,

		// ARMOR
		Helmet,
		Vest,
		//GrapheneShield,

		// UTILITY
		//Battery,
		Flare,
		//Lightrod,
		Extinguisher,
		//Spotlight,
		//ThermalImager,
		NightVision,
		Blowtorch,
		Unknown,
	}
	/// <summary>
	/// Contains Type of an item
	/// </summary>
	public enum Types
	{
		Consumable = 0,
		Weapon,
		Armor,
		Utility,
		Unknown,
	}
	private int maxUses = 1;
	public Tags Tag 			{ get; private set; } = Tags.Unknown;	// Name of item
	public Rarity Rarity 		{ get; private set; } = Rarity.Common;	// Rarity of item
	public Types Type 			{ get; private set; } = Types.Unknown;	// Type of item
	public string Name 			{ get; private set; }					// Ingame name of item
	public string Description 	{ get; private set; }					// Ingame description of item
	public string Stats 		{ get; private set; }					// Ingame list of durability, damage, armor damage, and range
	public int CurrentUses 		{ get; private set; } = 1;				// Current durability of item
	public bool IsActiveFlare 	{ get; private set; } = false;
	public int ActiveFlareTurnsRemaining { get; private set; } = 0;
	public int DamagePoints 	{ get; private set; } = -1;				// Damage of item, -1 = not a weapon
	public int ArmorDamage 		{ get; private set; } = -1;				// Damage of item to armor, -1 = does same damage as DamagePoints
	public int Range 			{ get; private set; } = -1;				// Range of item, -1 = not a ranged weapon
	public bool HasRange 		=> Range > 0;
	public bool IsUnbreakable	=> Tag is Tags.PlasmaRailgun;
	public bool IsEquipable 	{ get; private set; } = false;			// If item can be equipped, enabling and removing from inventory
	public bool IsAttachable 	{ get; private set; } = false;			// If item can be attached to vehicles, enabling and removing from inventory
	public bool IsFlammable 	{ get; private set; } = false;			// If item is flammable, can be destroyed by fire and helps it spread
	public bool IsStunning 		{ get; private set; } = false;			// If item stuns enemies when used
	[Serializable] private class Entry
	{
		public string Tag, Rarity, Type, Name, Description;
		public int maxUses = 1, damagePoints = -1, armorDamage = -1, range = -1;
		public bool isEquipable = false, isAttachable = false, isFlammable = false, isStunning = false, disabled = false;
	}
	[Serializable] private class EntryList { public List<Entry> Items; }
	private static readonly int lastItemIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> ItemRarityList = GenerateAllRarities();
	private static List<Entry> Database;
	/// <summary>
	/// Loads item definitions from JSON file in Resources folder
	/// </summary>
	private static void LoadDatabase()
	{
		if (Database != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/ItemDefinitions");
		if (JsonFile != null)
			Database = JsonUtility.FromJson<EntryList>(JsonFile.text).Items;
		else
			Debug.LogError("ItemDefinitions.json not found in Resources folder!");
	}
	/// <summary>
	/// Generates list of all rarities based on database
	/// </summary>
	private static List<Rarity> GenerateAllRarities()
	{
		LoadDatabase();
		if (Database == null)
		{
			Debug.LogWarning("Database failed to load, returning empty list");
			return new();
		}
		List<Rarity> Rarities = new();
		for (int i = 0; i < lastItemIndex; i++)
		{
			string TagName = ((Tags)i).ToString();
			Entry Entry = Database.Find(Entry => Entry.Tag == TagName);
			if (Entry != null && !Entry.disabled)
				Rarities.Add(Rarity.Parse(Entry.Rarity));
			else if (Entry == null)
				Rarities.Add(new ItemInfo(i).Rarity);
		}
		return Rarities;
	}
	/// <summary>
	/// Gets list of allowed rarities based on current region's item pool
	/// </summary>
	public static List<Rarity> GetAllowedRarities()
	{
		RegionManager RegionManager = GameManager.Instance.GetRegionManager();
		List<string> AllowedRarityNames = RegionManager.CurrentRegion?.ItemPool;
		// No region filtering, return all rarities
		if (AllowedRarityNames == null || AllowedRarityNames.Count == 0)
		{
			return new List<Rarity>(Rarity.RarityList);
		}
		// Convert rarity names to Rarity objects
		HashSet<Rarity> AllowedRarities = new();
		foreach (string RarityName in AllowedRarityNames)
		{
			Rarity Rarity = Rarity.Parse(RarityName);
			AllowedRarities.Add(Rarity);
		}
		return new List<Rarity>(AllowedRarities);
	}
	/// <summary>
	/// Returns a random index of an item within the specified rarity
	/// </summary>
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		List<int> Indices = Enumerable.Range(0, ItemRarityList.Count)
									  .Where(i => ItemRarityList[i] == Rarity)
									  .ToList();
		if (Indices.Count == 0)
			return -1;
		return Indices[UnityEngine.Random.Range(0, Indices.Count)];
	}
	/// <summary>
	/// Decreases item durability by amount and updates description
	/// </summary>
	public void DecreaseDurability(int amount = 1)
	{
		if (amount <= 0)
			return;
		CurrentUses = Mathf.Max(CurrentUses - amount, 0);
		RefreshStats();
	}
	/// <summary>
	/// Restores durability to max uses and updates description
	/// </summary>
	public void RestoreDurabilityToMax()
	{
		CurrentUses = maxUses;
		RefreshStats();
	}
	private const int flareBurnTurns = 3;
	public bool ActivateFlare()
	{
		if (Tag != Tags.Flare || IsActiveFlare)
			return false;
		IsActiveFlare = true;
		ActiveFlareTurnsRemaining = flareBurnTurns;
		RefreshStats();
		return true;
	}
	/// <summary>
	/// Advances active flare lifetime by one turn.
	/// Returns true if the flare burned out this tick.
	/// </summary>
	public bool TickActiveFlare()
	{
		if (!IsActiveFlare)
			return false;
		ActiveFlareTurnsRemaining = Mathf.Max(ActiveFlareTurnsRemaining - 1, 0);
		bool burnedOut = ActiveFlareTurnsRemaining <= 0;
		if (burnedOut)
			ExtinguishFlare();
		else
			RefreshStats();
		return burnedOut;
	}
	public void ExtinguishFlare()
	{
		IsActiveFlare = false;
		ActiveFlareTurnsRemaining = 0;
		RefreshStats();
	}
	public ItemInfo Clone()
	{
		ItemInfo ClonedItem = new((int)Tag);
		ClonedItem.CurrentUses = CurrentUses;
		ClonedItem.IsActiveFlare = IsActiveFlare;
		ClonedItem.ActiveFlareTurnsRemaining = ActiveFlareTurnsRemaining;
		ClonedItem.RefreshStats();
		return ClonedItem;
	}
	private void RefreshStats()
	{
		Stats = $"\nUP:{CurrentUses}/{maxUses}";
		if (Type is Types.Weapon)
		{
			Stats += $"\tDP:{DamagePoints}";
			if (ArmorDamage >= 0)
			{
				Stats += $"\nAD:{ArmorDamage}";
			}
			if (HasRange)
			{
				Stats += $"\nRP:{Range}";
			}
		}
		if (Tag == Tags.Flare && IsActiveFlare)
			Stats += $"\nFLR:{ActiveFlareTurnsRemaining}";
	}
	/// <summary>
	/// Returns info for a desired item,
	/// n must match Tag order and GameManager ItemTemplates order
	/// </summary>
	public ItemInfo(int n)
	{
		LoadDatabase();
		Tags TagData	= (Tags)n;
		string TagName 	= TagData.ToString();
		// Try to load from JSON first
		if (Database != null)
		{
			Entry Entry = Database.Find(Entry => Entry.Tag == TagName);
			if (Entry != null && !Entry.disabled)
			{
				LoadFrom(Entry);
				CurrentUses = maxUses;
				IsActiveFlare = false;
				ActiveFlareTurnsRemaining = 0;
				RefreshStats();
				return;
			}
			else if (Entry != null && Entry.disabled)
			{
				Debug.LogWarning($"Item {n} {TagName} is disabled in JSON");
			}
		}
		// Fallback to hardcoded values if JSON loading fails
		Debug.LogWarning($"Item {n} {TagName} not found in JSON, using default values");
		Tag 			= TagData;
		Name 			= TagName.ToUpper();
		Description 	= "Unknown item";
		CurrentUses 	= maxUses;
		Stats 			= $"\nUP:{maxUses}/{maxUses}";
	}
	private void LoadFrom(Entry Source)
	{
		Name 			= Source.Name;
		Description 	= Source.Description;
		maxUses 		= Source.maxUses;
		DamagePoints 	= Source.damagePoints;
		ArmorDamage 	= Source.armorDamage;
		Range 			= Source.range;
		IsEquipable 	= Source.isEquipable;
		IsAttachable 	= Source.isAttachable;
		IsFlammable 	= Source.isFlammable;
		IsStunning 		= Source.isStunning;
		Tag 	= Enum.TryParse(Source.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
		Rarity 	= Rarity.Parse(Source.Rarity);
		Type 	= Enum.TryParse(Source.Type, out Types ParsedType) ? ParsedType : Types.Unknown;
	}
}
