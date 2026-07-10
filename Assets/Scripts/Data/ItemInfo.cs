using System;
using System.Collections.Generic;
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
		Lightrod,
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
	public int MaxUses			{ get; private set; } = 1;				// Max durability of item, also max uses for consumables
	public Tags Tag 			{ get; private set; } = Tags.Unknown;	// Name of item
	public Rarity Rarity 		{ get; private set; } = Rarity.Common;	// Rarity of item
	public Types Type 			{ get; private set; } = Types.Unknown;	// Type of item
	public string Name 			{ get; private set; }					// Ingame name of item
	public string Description 	{ get; private set; }					// Ingame description of item
	public string Stats 		{ get; private set; }					// Ingame list of durability, damage, armor damage, and range
	public int CurrentUses 		{ get; private set; } = 1;				// Current durability of item
	public int DamagePoints 	{ get; private set; } = -1;				// Damage of item, -1 = not a weapon
	public int ArmorDamage 		{ get; private set; } = -1;				// Damage of item to armor, -1 = does same damage as DamagePoints
	public int Range 			{ get; private set; } = -1;				// Range of item, -1 = not a ranged weapon
	public int ActiveFlareTurnsRemaining { get; private set; } = 0;		// Number of turns remaining for an active flare, 0 = not active
	public bool IsActiveFlare 	{ get; private set; } = false;			// If item is an active flare, burning and illuminating the area
	public bool HasRange 		=> Range > 0;							// If item has a range value, meaning it is a ranged weapon
	public bool IsUnbreakable	=> Tag is Tags.PlasmaRailgun;			// If item is unbreakable, meaning it cannot be depleted or destroyed
	public bool IsDepleted		=> CurrentUses <= 0;					// If item is out of UP
	public bool IsEquipable 	{ get; private set; } = false;			// If item can be equipped, enabling and removing from inventory
	public bool IsAttachable 	{ get; private set; } = false;			// If item can be attached to vehicles, enabling and removing from inventory
	public bool IsFlammable 	{ get; private set; } = false;			// If item is flammable, can be destroyed by fire and helps it spread
	public bool IsStunning 		{ get; private set; } = false;			// If item stuns enemies when used
	public List<LootCategory> Categories { get; private set; } = new();	// Functional categories for loot profile biasing
	public bool IsThrowable 	=> Categories.Contains(LootCategory.Throwable);
	public int GetDamageAgainst(bool isArmored) =>
		isArmored && ArmorDamage >= 0 ? ArmorDamage : DamagePoints;
	public int LootWeight 		{ get; private set; } = 100;			// Relative pick weight within its rarity tier
	public bool UniquePerRun 	{ get; private set; } = false;			// If item can generate at most once per run
	public RegionInfo.Tags MinRegion { get; private set; } = RegionInfo.Tags.RuinedOutpost; // Earliest region item can generate in
	[Serializable] private class Entry
	{
		public string Tag, Rarity, Type, Name, Description, MinRegion;
		public int maxUses = 1, damagePoints = -1, armorDamage = -1, range = -1, lootWeight = 100;
		public bool isEquipable = false, isAttachable = false, isFlammable = false, isStunning = false, uniquePerRun = false, disabled = false;
		public List<string> Categories = new();
	}
	[Serializable] private class EntryList { public List<Entry> Items; }
	private static readonly int lastItemIndex = (int)Tags.Unknown;
	private static List<Entry> Database;
	private static Dictionary<Tags, Entry> EntryByTag;
	/// <summary>
	/// Loads item definitions from JSON file in Resources folder
	/// </summary>
	private static void LoadDatabase()
	{
		if (Database != null)
			return;
		TextAsset JsonFile = Resources.Load<TextAsset>("Definitions/ItemDefinitions");
		if (JsonFile == null)
		{
			Debug.LogError("ItemDefinitions.json not found in Resources folder!");
			return;
		}
		Database = JsonUtility.FromJson<EntryList>(JsonFile.text).Items;
		// Index entries by tag so per-construction lookups (items are cloned on
		// pickup and drop) avoid a linear scan with enum-to-string conversion
		EntryByTag = new();
		foreach (Entry Entry in Database)
		{
			if (Enum.TryParse(Entry.Tag, out Tags Tag))
				EntryByTag[Tag] = Entry;
		}
	}
	/// <summary>
	/// Returns indices of all enabled items in the database, in Tags order
	/// </summary>
	public static List<int> GetEnabledItemIndices()
	{
		LoadDatabase();
		List<int> Indices = new();
		if (EntryByTag == null)
			return Indices;
		for (int i = 0; i < lastItemIndex; i++)
		{
			if (EntryByTag.TryGetValue((Tags)i, out Entry Entry) && !Entry.disabled)
				Indices.Add(i);
		}
		return Indices;
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
	/// Adds uses up to maxUses, returns the amount actually added
	/// </summary>
	public int AddUses(int amount)
	{
		if (amount <= 0)
			return 0;
		int spaceLeft = MaxUses - CurrentUses;
		int added = Mathf.Min(amount, spaceLeft);
		CurrentUses += added;
		RefreshStats();
		return added;
	}
	/// <summary>
	/// Restores durability to max uses and updates description
	/// </summary>
	public void RestoreDurabilityToMax()
	{
		CurrentUses = MaxUses;
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
		Stats = $"\nUP:{CurrentUses}/{MaxUses}";
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
		// Try to load from JSON first
		if (EntryByTag != null && EntryByTag.TryGetValue(TagData, out Entry Entry))
		{
			if (!Entry.disabled)
			{
				LoadFrom(Entry);
				CurrentUses = MaxUses;
				IsActiveFlare = false;
				ActiveFlareTurnsRemaining = 0;
				RefreshStats();
				return;
			}
			Debug.LogWarning($"Item {n} {TagData} is disabled in JSON");
		}
		// Fallback to hardcoded values if JSON loading fails
		string TagName = TagData.ToString();
		Debug.LogWarning($"Item {n} {TagName} not found in JSON, using default values");
		Tag 			= TagData;
		Name 			= TagName.ToUpper();
		Description 	= "Unknown item";
		CurrentUses 	= MaxUses;
		Stats 			= $"\nUP:{MaxUses}/{MaxUses}";
	}
	private void LoadFrom(Entry Source)
	{
		Name 			= Source.Name;
		Description 	= Source.Description;
		MaxUses 		= Source.maxUses;
		DamagePoints 	= Source.damagePoints;
		ArmorDamage 	= Source.armorDamage;
		Range 			= Source.range;
		IsEquipable 	= Source.isEquipable;
		IsAttachable 	= Source.isAttachable;
		IsFlammable 	= Source.isFlammable;
		IsStunning 		= Source.isStunning;
		LootWeight 		= Mathf.Max(0, Source.lootWeight);
		UniquePerRun 	= Source.uniquePerRun;
		Categories 		= new();
		foreach (string CategoryName in Source.Categories)
		{
			if (Enum.TryParse(CategoryName, out LootCategory ParsedCategory))
				Categories.Add(ParsedCategory);
			else
				Debug.LogWarning($"Unknown loot category '{CategoryName}' on item {Source.Tag}");
		}
		MinRegion = Enum.TryParse(Source.MinRegion, out RegionInfo.Tags ParsedRegion) ? ParsedRegion : RegionInfo.Tags.RuinedOutpost;
		Tag 	= Enum.TryParse(Source.Tag, out Tags ParsedTag) ? ParsedTag : Tags.Unknown;
		Rarity 	= Rarity.Parse(Source.Rarity);
		Type 	= Enum.TryParse(Source.Type, out Types ParsedType) ? ParsedType : Types.Unknown;
	}
}
