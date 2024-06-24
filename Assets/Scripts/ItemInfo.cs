using System.Collections.Generic;
using UnityEngine;
using Exodyssey.Rarity;

/// <summary>
/// Contains all item variables, creates items with specific values, and manages items after usage
/// </summary>
public class ItemInfo
{
	/// <summary>
	/// Contains identifying Name for a unique item, usually matches name
	/// </summary>
	public enum Tags
	{
		// CONSUMABLE
		MedKit = 0,
		//ToolKit,
		//FusionCell,

		// MELEE
		Branch,
		Knife,
		Wrench,
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
		HuntingRifle,
		PlasmaRailgun,

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
	/// Contains Type of an item
	/// </summary>
	public enum Types
	{
		Utility = 0,
		Weapon,
		Consumable,
		Armor,
		Storage,
		Unknown,
	}
	// Name of item
	public Tags Tag = Tags.Unknown;
	// Rarity of item
	public Rarity Rarity = Rarity.Common;
	// Type of item
	public Types Type = Types.Unknown;
	// Ingame name of item
	public string name = "UNKNOWN";
	// Ingame description of item
	public string description = "UNKNOWN";
	// Ingame list of durability, damage, armor damage, and range
	public string stats = "UNKNOWN";
	// Max durability of item
	public int maxUses = 1;
	// Current durability of item
	public int currentUses = 1;
	// Damage of item, -1 = not a weapon
	public int damagePoints = -1;
	// Damage of item to armor, -1 = does same damage as damagePoints
	public int armorDamage = -1;
	// Range of item, -1 = not a ranged weapon
	public int range = 0;
	// Whether item can be equipped, enabling it and removing it from inventory
	public bool isEquipable = false;
	// Whether item can be attached to vehicles, enabling it and removing it from inventory
	public bool isAttachable = false;
	// Whether item is flammable, can be destroyed by fire and helps it spread
	public bool isFlammable = false;
	private static readonly int lastItemIndex = (int)Tags.Unknown;
	private static List<Rarity> GenerateAllRarities()
	{
		List<Rarity> ItemRarityList = new();
		for (int i = 0; i < lastItemIndex; i++)
		{
			ItemInfo Item = ItemFactory(i);
			ItemRarityList.Add(Item.Rarity);
		}
		return ItemRarityList;
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity specifiedRarity)
	{
		List<Rarity> ItemRarityList = GenerateAllRarities();
		List<int> IndicesOfSpecifiedRarity = new();
		for (int i = 0; i < ItemRarityList.Count; i++)
		{
			if (ItemRarityList[i] == specifiedRarity)
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
	/// Decreases item durability by 1 and updates description
	/// </summary>
	public void DecreaseDurability() 
	{
		currentUses--;
		stats = $"\nUP:{currentUses}/{maxUses}";
		if (Type is Types.Weapon) 
		{
			stats += $"\tDP:{damagePoints}";
			if (armorDamage >= 0)
			{
				stats += $"\nADP:{armorDamage}";
			}
			if (range > 0)
			{
				stats += $"\nRP:{range}";
			}
		}
	}
	/// <summary>
	/// Returns info for a desired item,
	/// n must match Tag order and GameManager ItemTemplates order
	/// </summary>
	public static ItemInfo ItemFactory(int n)
	{
		ItemInfo info = new();
		switch (n)
		{
			case 0:
				info.Tag = Tags.MedKit;
				info.Rarity = Rarity.Limited;
				info.Type = Types.Consumable;
				info.name = "MEDKIT";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.isFlammable = true;
				info.description = "Fully heals injuries";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}";
				break;
			case 1:
				info.Tag = Tags.Branch;
				info.Rarity = Rarity.Common;
				info.Type = Types.Weapon;
				info.name = "BRANCH";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.damagePoints = 1;
				info.armorDamage = 0;
				info.isFlammable = true;
				info.description = "Fragile stick";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nADP:{info.armorDamage}";
				break;
				
			case 2:
				info.Tag = Tags.Knife;
				info.Rarity = Rarity.Limited;
				info.Type = Types.Weapon;
				info.name = "KNIFE";
				info.maxUses = 2;
				info.currentUses = info.maxUses;
				info.damagePoints = 2;
				info.armorDamage = 1;
				info.description = "Can stab through armor";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nADP:{info.armorDamage}";
				break;
			case 3:
				info.Tag = Tags.Wrench;
				info.Rarity = Rarity.Scarce;
				info.Type = Types.Weapon;
				info.name = "WRENCH";
				info.maxUses = 4;
				info.currentUses = info.maxUses;
				info.damagePoints = 2;
				info.armorDamage = 0;
				info.description = "Durable, weak against armor";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nADP:{info.armorDamage}";
				break;
			case 4:
				info.Tag = Tags.DiamondChainsaw;
				info.Rarity = Rarity.Anomalous;
				info.Type = Types.Weapon;
				info.name = "CHAINSAW";
				info.maxUses = 8;
				info.currentUses = info.maxUses;
				info.damagePoints = 5;
				info.description = "Handheld rock saw";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}";
				break;
			case 5:
				info.Tag = Tags.HuntingRifle;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Weapon;
				info.name = "HUNTING RIFLE";
				info.maxUses = 3;
				info.currentUses = info.maxUses;
				info.damagePoints = 5;
				info.range = 10;
				info.description = "Fires armor piercing rounds";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			case 6:
				info.Tag = Tags.PlasmaRailgun;
				info.Rarity = Rarity.Anomalous;
				info.Type = Types.Weapon;
				info.name = "PLASMA RAILGUN";
				info.maxUses = 5;
				info.currentUses = info.maxUses;
				info.damagePoints = 10;
				info.range = 5;
				info.description = "Fires a voltaic plasma bolt";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			/*
			case 4:
				info.Tag = Tags.ToolKit;
				info.Rarity = Rarity.Scarce;
				info.Type = Types.Consumable;
				info.name = "TOOLKIT";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.description = "Fully repairs vehicles";
				info.stats = "\nUP:" + info.maxUses;
				break;
			case 7:
				info.Tag = Tags.Mallet;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Weapon;
				info.maxUses = 6;
				info.currentUses = info.maxUses;
				info.name = "MALLET";
				info.damagePoints = 3;
				info.armorDamage = 0;
				info.description = "Bounces off armor";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nADP:{info.armorDamage}";
				break;
			case 8:
				info.Tag = Tags.Axe;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Weapon;
				info.name = "AXE";
				info.maxUses = 4;
				info.currentUses = info.maxUses;
				info.damagePoints = 4;
				info.armorDamage = 2;
				info.description = "Can cut through armor";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nADP:{info.armorDamage}";
				break;
			case 9:
				info.Tag = Tags.Rock;
				info.Rarity = Rarity.Common;
				info.Type = Types.Weapon;
				info.name = "ROCK";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.damagePoints = 1;
				info.range = 3;
				info.description = "Can be thrown again";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			case 10:
				info.Tag = Tags.SmokeGrenade;
				info.Rarity = Rarity.Scarce;
				info.Type = Types.Weapon;
				info.name = "SMOKE GRENADE";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.damagePoints = 0;
				info.range = 3;
				info.description = "Stuns nearby enemies for 1 turn";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			case 11:
				info.Tag = Tags.Dynamite;
				info.Rarity = Rarity.Scarce;
				info.Type = Types.Weapon;
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
				info.Tag = Tags.StickyGrenade;
				info.Rarity = Rarity.Anomalous;
				info.Type = Types.Weapon;
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
				info.Tag = Tags.FusionCell;
				info.Rarity = Rarity.Common;
				info.Type = Types.Consumable;
				info.name = "FUSION CELL";
				info.maxUses = 5; // max charge
				info.currentUses = info.maxUses; // current charge
				info.isFlammable = true;
				info.description = "Powers vehicles";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 14:
				info.Tag = Tags.Helmet;
				info.Rarity = Rarity.Scarce;
				info.Type = Types.Armor;
				info.name = "HELMET";
				info.maxUses = 2;
				info.currentUses = info.maxUses;
				info.isEquipable = true;
				info.description = "Absorbs 2 melee DP";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 15:
				info.Tag = Tags.Vest;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Armor;
				info.name = "VEST";
				info.maxUses = 3;
				info.currentUses = info.maxUses;
				info.isEquipable = true;
				info.isFlammable = true;
				info.description = "Absorbs 3 ranged DP";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 16:
				info.Tag = Tags.GrapheneShield;
				info.Rarity = Rarity.Anomalous;
				info.Type = Types.Armor;
				info.name = "GRAPHENE SHIELD";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.isEquipable = true;
				info.description = "Blocks all DP except boss DP";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 17:
				info.Tag = Tags.Backpack;
				info.Rarity = Rarity.Limited;
				info.Type = Types.Storage;
				info.name = "BACKPACK";
				info.maxUses = 1;
				info.currentUses = info.maxUses;
				info.isEquipable = true;
				info.isFlammable = true;
				info.description = "Adds 1 inventory slot";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 18:
				info.Tag = Tags.Crate;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Storage;
				info.name = "CRATE";
				info.maxUses = 2;
				info.currentUses = info.maxUses;
				info.isAttachable = true;
				info.description = "Adds 2 vehicle storage";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 19:
				info.Tag = Tags.Battery;
				info.Rarity = Rarity.Anomalous;
				info.Type = Types.Storage;
				info.name = "BATTERY";
				info.maxUses = 5; // max charge
				info.currentUses = info.maxUses; // current charge
				info.isAttachable = true;
				info.isFlammable = true;
				info.description = "Adds 5 fuel slots";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 20:
				info.Tag = Tags.Lightrod;
				info.Rarity = Rarity.Limited;
				info.Type = Types.Utility;
				info.name = "LIGHTROD";
				info.isFlammable = true;
				info.description = "Illuminates surroundings";
				break;
			case 21:
				info.Tag = Tags.Extinguisher;
				info.Rarity = Rarity.Scarce;
				info.Type = Types.Utility;
				info.name = "EXTINGUISHER";
				info.maxUses = 4;
				info.currentUses = info.maxUses;
				info.isFlammable = true;
				info.description = "Extinguishes fires";
				info.stats = "\n\UP:" + info.maxUses;
				break;
			case 22:
				info.Tag = Tags.Spotlight;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Utility;
				info.name = "SPOTLIGHT";
				info.isAttachable = true;
				info.description = "Outputs directional light";
				break;
			case 23:
				info.Tag = Tags.Blowtorch;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Utility;
				info.name = "BLOWTORCH";
				info.maxUses = 4;
				info.currentUses = info.maxUses;
				info.isFlammable = true;
				info.description = "Sets objects on fire";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 24:
				info.Tag = Tags.ThermalImager;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Utility;
				info.name = "THERMAL IMAGER";
				info.maxUses = 4;
				info.currentUses = info.maxUses;
				info.description = "Reveals heat signatures";
				info.stats = $"\n\UP:{info.maxUses}";
				break;
			case 25:
				info.Tag = Tags.NightVision;
				info.Rarity = Rarity.Anomalous;
				info.Type = Types.Utility;
				info.name = "NIGHT VISION";
				info.isEquipable = true;
				info.description = "Enables nighttime visibility";
				break;
			case 26:
				info.Tag = Tags.Tranquilizer;
				info.Rarity = Rarity.Scarce;
				info.Type = Types.Weapon;
				info.name = "TRANQUILIZER";
				info.maxUses = 2;
				info.currentUses = info.maxUses;
				info.damagePoints = 0;
				info.range = 4;
				info.description = "Stuns enemies for 1 turn";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			case 27:
				info.Tag = Tags.Carbine;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Weapon;
				info.name = "CARBINE";
				info.maxUses = 4;
				info.currentUses = info.maxUses;
				info.damagePoints = 3;
				info.range = 4;
				info.description = "Fires rifle bullets";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			case 28:
				info.Tag = Tags.Flamethrower;
				info.Rarity = Rarity.Rare;
				info.Type = Types.Weapon;
				info.name = "FLAMETHROWER";
				info.maxUses = 4;
				info.currentUses = info.maxUses;
				info.damagePoints = 1;
				info.range = 3;
				info.isFlammable = true;
				info.description = "Sprays streaks of fire";
				info.stats = $"\nUP:{info.maxUses}/{info.maxUses}\tDP:{info.damagePoints}\nRP:{info.range}";
				break;
			default:
				break;
			*/
		}
		return info;
	}
}
