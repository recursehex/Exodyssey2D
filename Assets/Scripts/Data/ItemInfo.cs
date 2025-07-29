using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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
		//Flamethrower,
		HuntingRifle,
		PlasmaRailgun,

		// THROWABLE
		Rock,
		//SmokeGrenade,
		//Dynamite,
		//StickyGrenade,

		// ARMOR
		//Helmet,
		//Vest,
		//GrapheneShield,

		// UTILITY
		//Battery,
		Flare,
		//Lightrod,
		Extinguisher,
		//Spotlight,
		//Blowtorch,
		//ThermalImager,
		//NightVision,
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
	private ItemData Data = new();										// Internal data
	public Tags Tag 			{ get; private set; } = Tags.Unknown;	// Name of item
	public Rarity Rarity 		{ get; private set; } = Rarity.Common;	// Rarity of item
	public Types Type 			{ get; private set; } = Types.Unknown;	// Type of item
	public string Name 			=> Data.Name;							// Ingame name of item
	public string Description 	=> Data.Description;					// Ingame description of item
	public string Stats 		{ get; private set; }					// Ingame list of durability, damage, armor damage, and range
	public int CurrentUses 		{ get; private set; } = 1;				// Current durability of item
	public int DamagePoints 	=> Data.damagePoints;					// Damage of item, -1 = not a weapon
	public int ArmorDamage 		=> Data.armorDamage;					// Damage of item to armor, -1 = does same damage as DamagePoints
	public int Range 			=> Data.range;							// Range of item, -1 = not a ranged weapon
	public bool IsEquipable 	=> Data.isEquipable;					// If item can be equipped, enabling and removing from inventory
	public bool IsAttachable 	=> Data.isAttachable;					// If item can be attached to vehicles, enabling and removing from inventory
	public bool IsFlammable 	=> Data.isFlammable;					// If item is flammable, can be destroyed by fire and helps it spread
	public bool IsStunning 		=> Data.isStunning;						// If item stuns enemies when used
	private static readonly int lastItemIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> ItemRarityList = GenerateAllRarities();
	private static ItemDatabase ItemDatabase;
	private static bool databaseLoaded = false;
	
	private static void LoadDatabase()
	{
		if (databaseLoaded)
		{
			return;
		}
		TextAsset jsonFile = Resources.Load<TextAsset>("ItemDefinitions");
		if (jsonFile != null)
		{
			ItemDatabase 	= JsonUtility.FromJson<ItemDatabase>(jsonFile.text);
			databaseLoaded 	= true;
		}
		else
		{
			Debug.LogError("ItemDefinitions.json not found in Resources folder!");
		}
	}
	
	private static List<Rarity> GenerateAllRarities()
	{
		LoadDatabase();
		List<Rarity> rarities = new();
		// First, try to get rarities from JSON
		if (ItemDatabase != null
		 && ItemDatabase.Items != null)
		{
			for (int i = 0; i < lastItemIndex; i++)
			{
				Tags tag 		= (Tags)i;
				string tagName 	= tag.ToString();
				ItemData data = ItemDatabase.Items.Find(item => item.Tag == tagName);
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
					// Fallback to creating ItemInfo if not found in JSON
					rarities.Add(new ItemInfo(i).Rarity);
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
		List<int> indices = Enumerable.Range(0, ItemRarityList.Count)
									  .Where(i => ItemRarityList[i] == Rarity)
									  .ToList();
		if (indices.Count == 0)
			return -1;
		return indices[UnityEngine.Random.Range(0, indices.Count)];
	}
	/// <summary>
	/// Decreases item durability by amount and updates description
	/// </summary>
	public void DecreaseDurability(int amount = 1)
	{
		CurrentUses -= amount;
		Stats = $"\nUP:{CurrentUses}/{Data.maxUses}";
		if (Type is Types.Weapon) 
		{
			Stats += $"\tDP:{DamagePoints}";
			if (ArmorDamage >= 0)
			{
				Stats += $"\nAD:{ArmorDamage}";
			}
			if (Range > 0)
			{
				Stats += $"\nRP:{Range}";
			}
		}
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
		if (ItemDatabase != null
		 && ItemDatabase.Items != null)
		{
			ItemData Data = ItemDatabase.Items.Find(Item => Item.Tag == TagName);
			if (Data != null && !Data.disabled)
			{
				LoadFromData(Data);
				CurrentUses = Data.maxUses;
				return;
			}
			else if (Data != null && Data.disabled)
			{
				Debug.LogWarning($"Item {n} {TagName} is disabled in JSON");
			}
		}
		
		// Fallback to hardcoded values if JSON loading fails
		Debug.LogWarning($"Item {n} {TagName} not found in JSON, using default values");
		
		// Set minimal defaults for unknown items
		Tag 				= TagData;
		Rarity 				= Rarity.Common;
		Type 				= Types.Unknown;
		Data.Tag 			= TagName;
		Data.Name 			= TagName.ToUpper();
		Data.Description 	= "Unknown item";
		Data.maxUses 		= 1;
		CurrentUses 		= Data.maxUses;
		Stats 				= $"\nUP:{Data.maxUses}/{Data.maxUses}";
	}
	
	private void LoadFromData(ItemData SourceData)
	{
		// Copy the data
		Data = new ItemData
		{
			Tag 			= SourceData.Tag,
			Rarity 			= SourceData.Rarity,
			Type 			= SourceData.Type,
			Name 			= SourceData.Name,
			Description 	= SourceData.Description,
			maxUses 		= SourceData.maxUses,
			damagePoints 	= SourceData.damagePoints,
			armorDamage 	= SourceData.armorDamage,
			range 			= SourceData.range,
			isEquipable 	= SourceData.isEquipable,
			isAttachable 	= SourceData.isAttachable,
			isFlammable 	= SourceData.isFlammable,
			isStunning 		= SourceData.isStunning
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
		
		// Generate stats string
		Stats = $"\nUP:{Data.maxUses}/{Data.maxUses}";
		if (Type == Types.Weapon)
		{
			Stats += $"\tDP:{Data.damagePoints}";
			if (Data.armorDamage >= 0)
			{
				Stats += $"\nAD:{Data.armorDamage}";
			}
			if (Data.range > 0)
			{
				Stats += $"\nRP:{Data.range}";
			}
		}
	}
}