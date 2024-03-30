using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all item variables, creates items with specific values, and manages items after usage
/// </summary>
public class ItemInfo
{
	/// <summary>
	/// Contains identifying tag for a unique item, usually matches name
	/// </summary>
	public enum ItemTag
	{
		// CONSUMABLE
		MedKit = 0,
		//ToolKit,
		//FusionCell,

		// MELEE
		Branch,
		Knife,
		//Wrench,
		//Mallet,
		//Axe,
		DiamondChainsaw,

		// THROWABLE
		//Rock,
		//SmokeGrenade,
		//Dynamite,
		//StickyGrenade,

		// RANGED
		//Tranquilizer,
		//Carbine,
		//Flamethrower,
		PlasmaRailgun,
		HuntingRifle,

		// ARMOR
		//Helmet,
		//Vest,
		//GrapheneShield,

		// STORAGE
		//Backpack,
		//Battery,
		//Crate,

		// UTILITY
		//Lightrod,
		//Extinguisher,
		//Spotlight,
		//Blowtorch,
		//ThermalImager,
		//NightVision,
		Unknown,
	}
	/// <summary>
	/// Contains type of an item
	/// </summary>
	public enum ItemType
	{
		Utility = 0,
		Weapon,
		Consumable,
		Armor,
		Storage,
		Unknown,
	}
	// All default values for an item
	public ItemTag tag;                 // Name of item
	public Rarity.RarityTag rarity;     // Rarity of item
	public ItemType type;               // Type of item
	public string name;                 // Ingame name of item
	public string description;          // Ingame desc of item
	public string stats = "";           // Ingame list of durability, damage, shell damage, range
	public int maxUses = -1;            // Max uses item has
	public int currentUses = -1;        // Current uses item has left
	public int damagePoints = -1;       // Set only for weapons
	public int range = 0;               // Maximum distance a Ranged weapon can attack to, 0 = Melee
	public bool isEquipable = false;    // Can be equipped by characters, enabling the item and removing it from inventory
	public bool isAttachable = false;   // Can be attached to vehicles, enabling the item and removing it from inventory
	public bool isFlammable = false;    // Will be destroyed by fire and helps it to spread
	public int shellDamage = -1;        // Set only if weapon does different damage to shelled aliens
	private static readonly int lastItemIndex = (int)ItemTag.Unknown;
	private static List<Rarity.RarityTag> GenerateAllRarities()
	{
		List<Rarity.RarityTag> itemRarityList = new();
		for (int i = 0; i < lastItemIndex; i++)
		{
			ItemInfo item = ItemFactory(i);
			itemRarityList.Add(item.rarity);
		}
		return itemRarityList;
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity.RarityTag specifiedRarity)
	{
		List<Rarity.RarityTag> itemRarityList = GenerateAllRarities();
		List<int> indicesOfSpecifiedRarity = new();
		for (int i = 0; i < itemRarityList.Count; i++)
		{
			if (itemRarityList[i] == specifiedRarity) indicesOfSpecifiedRarity.Add(i);
		}
		if (indicesOfSpecifiedRarity.Count == 0) return -1;
		int randomIndex = Random.Range(0, indicesOfSpecifiedRarity.Count);
		return indicesOfSpecifiedRarity[randomIndex];
	}
	/// <summary>
	/// Changes item durabilty and description after use
	/// </summary>
	public void ChangeDurability(int change) 
	{
		currentUses += change;
		stats = $"\nUP:{currentUses}/{maxUses}";
		if (type is ItemType.Weapon) 
		{
			stats += $"\tDP:{damagePoints}";
			if (shellDamage >= 0) stats += $"\nSDP:{shellDamage}";
			if (range > 0) stats += $"\nRP:{range}";
		}
	}
	/// <summary>
	/// Returns info for a desired item 
	/// </summary>
	/// <param name="n">Must match ItemTag order and GameManager ItemTemplates order</param>
	/// <returns></returns>
	public static ItemInfo ItemFactory(int n)
	{
		ItemInfo info = new();
		switch (n)
		{
			case 0:
				info.tag = ItemTag.MedKit;
				info.rarity = Rarity.RarityTag.Limited;
				info.type = ItemType.Consumable;
				info.name = "MEDKIT";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.isFlammable = true;
				info.description = "Heals oneself";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}";
				break;
			case 1:
				info.tag = ItemTag.Branch;
				info.rarity = Rarity.RarityTag.Common;
				info.type = ItemType.Weapon;
				info.name = "BRANCH";
				info.maxUses = 2;
				info.currentUses = info.maxUses;
				info.damagePoints = 1;
				info.shellDamage = 0;
				info.isFlammable = true;
				info.description = "Fragile stick";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nSDP:{info.shellDamage}";
				break;
				
			case 2:
				info.tag = ItemTag.Knife;
				info.rarity = Rarity.RarityTag.Limited;
				info.type = ItemType.Weapon;
				info.name = "KNIFE";
				info.maxUses = 3;
				info.currentUses = info.maxUses;
				info.damagePoints = 2;
				info.shellDamage = 1;
				info.description = "Can stab shelled aliens";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nSDP:{info.shellDamage}";
				break;
			
			case 3:
				info.tag = ItemTag.DiamondChainsaw;
				info.rarity = Rarity.RarityTag.Rare;
				//info.rarity = Rarity.RarityTag.Anomalous;
				info.type = ItemType.Weapon;
				info.name = "CHAINSAW";
				info.maxUses = 8;
				info.currentUses = info.maxUses;
				info.damagePoints = 5;
				info.description = "Handheld rock saw";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}";
				break;
			case 4:
				info.tag = ItemTag.PlasmaRailgun;
				//info.rarity = Rarity.RarityTag.Anomalous;
				info.rarity = Rarity.RarityTag.Common;
				info.type = ItemType.Weapon;
				info.name = "PLASMA RAILGUN";
				info.maxUses = 5;
				info.currentUses = info.maxUses;
				info.damagePoints = 10;
				info.range = 5;
				info.description = "Fires a voltaic plasma bolt";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			
			// case 4:
			//     info.tag = ItemTag.ToolKit;
			//     info.rarity = Rarity.Scarce;
			//     info.type = ItemType.Consumable;
			//     info.name = "TOOLKIT";
			//     info.maxUses = 1;
			//     info.currentUses = info.maxUses;
			//     info.description = "Repairs vehicles";
			//     info.stats = "\nUP:" + info.maxUses;
			//     break;

			

			case 5:
				info.tag = ItemTag.HuntingRifle;
				info.rarity = Rarity.RarityTag.Rare;
				info.type = ItemType.Weapon;
				info.name = "HUNTING RIFLE";
				info.maxUses = 3;
				info.currentUses = info.maxUses;
				info.damagePoints = 5;
				info.range = 10;
				info.description = "Fires piercing bullets";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
				/*
				case 6:
					info.tag = ItemTag.Wrench;
					info.rarity = Rarity.Scarce;
					info.type = ItemType.Weapon;
					info.name = "WRENCH";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.damagePoints = 2;
					info.shellDamage = 0;
					info.description = "Useless for shelled aliens";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nSDP:{info.shellDamage}";
					break;

				case 7:
					info.tag = ItemTag.Mallet;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Weapon;
					info.maxUses = 6;
					info.currentUses = info.maxUses;
					info.name = "MALLET";
					info.damagePoints = 3;
					info.shellDamage = 0;
					info.description = "Bounces off shelled aliens";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nSDP:{info.shellDamage}";
					break;

				case 8:
					info.tag = ItemTag.Axe;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Weapon;
					info.name = "AXE";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.damagePoints = 4;
					info.shellDamage = 2;
					info.description = "Can cut shelled aliens";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nSDP:{info.shellDamage}";
					break;

				case 9:
					info.tag = ItemTag.Rock;
					info.rarity = Rarity.Common;
					info.type = ItemType.Weapon;
					info.name = "ROCK";
					info.maxUses = 1;
					info.currentUses = info.maxUses;
					info.damagePoints = 1;
					info.range = 3;
					info.description = "Can be thrown again";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
					break;

				case 10:
					info.tag = ItemTag.SmokeGrenade;
					info.rarity = Rarity.Scarce;
					info.type = ItemType.Weapon;
					info.name = "SMOKE GRENADE";
					info.maxUses = 1;
					info.currentUses = info.maxUses;
					info.damagePoints = 0;
					info.range = 3;
					info.description = "Stuns nearby enemies for 1 turn";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
					break;

				case 11:
					info.tag = ItemTag.Dynamite;
					info.rarity = Rarity.Scarce;
					info.type = ItemType.Weapon;
					info.name = "DYNAMITE";
					info.maxUses = 1;
					info.currentUses = info.maxUses;
					info.damagePoints = 5;
					info.range = 3;
					info.isFlammable = true;
					info.description = "Fuse lights after landing";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
					break;

				case 12:
					info.tag = ItemTag.StickyGrenade;
					info.rarity = Rarity.Anomalous;
					info.type = ItemType.Weapon;
					info.name = "STICKY GRENADE";
					info.maxUses = 1;
					info.currentUses = info.maxUses;
					info.damagePoints = 3;
					info.range = 5;
					info.isFlammable = true;
					info.description = "Sticks to enemies before detonating";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
					break;

				case 13:
					info.tag = ItemTag.FusionCell;
					info.rarity = Rarity.Common;
					info.type = ItemType.Consumable;
					info.name = "FUSION CELL";
					info.maxUses = 5; // max charge
					info.currentUses = info.maxUses; // current charge
					info.isFlammable = true;
					info.description = "Powers vehicles";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 14:
					info.tag = ItemTag.Helmet;
					info.rarity = Rarity.Scarce;
					info.type = ItemType.Armor;
					info.name = "HELMET";
					info.maxUses = 2;
					info.currentUses = info.maxUses;
					info.isEquipable = true;
					info.description = "Absorbs 2 melee DP";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 15:
					info.tag = ItemTag.Vest;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Armor;
					info.name = "VEST";
					info.maxUses = 3;
					info.currentUses = info.maxUses;
					info.isEquipable = true;
					info.isFlammable = true;
					info.description = "Absorbs 3 ranged DP";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 16:
					info.tag = ItemTag.GrapheneShield;
					info.rarity = Rarity.Anomalous;
					info.type = ItemType.Armor;
					info.name = "GRAPHENE SHIELD";
					info.maxUses = 1;
					info.currentUses = info.maxUses;
					info.isEquipable = true;
					info.description = "Blocks all DP except boss DP";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 17:
					info.tag = ItemTag.Backpack;
					info.rarity = Rarity.Limited;
					info.type = ItemType.Storage;
					info.name = "BACKPACK";
					info.maxUses = 1;
					info.currentUses = info.maxUses;
					info.isEquipable = true;
					info.isFlammable = true;
					info.description = "Adds 1 inventory slot";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 18:
					info.tag = ItemTag.Crate;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Storage;
					info.name = "CRATE";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.isAttachable = true;
					info.description = "Adds 4 vehicle storage";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 19:
					info.tag = ItemTag.Battery;
					info.rarity = Rarity.Anomalous;
					info.type = ItemType.Storage;
					info.name = "BATTERY";
					info.maxUses = 5; // max charge
					info.currentUses = info.maxUses; // current charge
					info.isAttachable = true;
					info.isFlammable = true;
					info.description = "Adds 5 fuel slots";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 20:
					info.tag = ItemTag.Lightrod;
					info.rarity = Rarity.Limited;
					info.type = ItemType.Utility;
					info.name = "LIGHTROD";
					info.isFlammable = true;
					info.description = "Illuminates surroundings";
					break;

				case 21:
					info.tag = ItemTag.Extinguisher;
					info.rarity = Rarity.Scarce;
					info.type = ItemType.Utility;
					info.name = "EXTINGUISHER";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.isFlammable = true;
					info.description = "Extinguishes burning tiles";
					info.stats = "\n\UP:" + info.maxUses;
					break;

				case 22:
					info.tag = ItemTag.Spotlight;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Utility;
					info.name = "SPOTLIGHT";
					info.isAttachable = true;
					info.description = "Outputs directional light";
					break;

				case 23:
					info.tag = ItemTag.Blowtorch;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Utility;
					info.name = "BLOWTORCH";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.isFlammable = true;
					info.description = "Starts fires on tiles or enemies";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 24:
					info.tag = ItemTag.ThermalImager;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Utility;
					info.name = "THERMAL IMAGER";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.description = "Take infrared picture";
					info.stats = $"\n\UP:{info.maxUses}";
					break;

				case 25:
					info.tag = ItemTag.NightVision;
					info.rarity = Rarity.Anomalous;
					info.type = ItemType.Utility;
					info.name = "NIGHT VISION";
					info.isEquipable = true;
					info.description = "Enables nighttime visibility";
					break;

				case 26:
					info.tag = ItemTag.Tranquilizer;
					info.rarity = Rarity.Scarce;
					info.type = ItemType.Weapon;
					info.name = "TRANQUILIZER";
					info.maxUses = 2;
					info.currentUses = info.maxUses;
					info.damagePoints = 0;
					info.range = 4;
					info.description = "Stuns enemies for 1 turn";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
					break;

				case 27:
					info.tag = ItemTag.Carbine;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Weapon;
					info.name = "CARBINE";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.damagePoints = 3;
					info.range = 4;
					info.description = "Fires rifle bullets";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
					break;

				case 28:
					info.tag = ItemTag.Flamethrower;
					info.rarity = Rarity.Rare;
					info.type = ItemType.Weapon;
					info.name = "FLAMETHROWER";
					info.maxUses = 4;
					info.currentUses = info.maxUses;
					info.damagePoints = 1;
					info.range = 3;
					info.isFlammable = true;
					info.description = "Sprays a streak of fire";
					info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
					break;

				default:
					info.tag = ItemTag.Unknwon;
					info.rarity = Rarity.Unknown;
					info.type = ItemType.Unknown;
					info.name = "UNKNOWN";
					info.description = "";
					break;
				*/
		}
		return info;
	}
}
