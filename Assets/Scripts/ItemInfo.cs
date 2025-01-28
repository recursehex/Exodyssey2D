using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using System.Linq;

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
		//FusionCell,

		// MELEE
		Branch,
		Knife,
		Wrench,
		Mallet,
		//Axe,
		DiamondChainsaw,

		// RANGED
		Tranquilizer,
		//Carbine,
		//Flamethrower,
		HuntingRifle,
		PlasmaRailgun,

		// THROWABLE
		//Rock,
		//SmokeGrenade,
		//Dynamite,
		//StickyGrenade,

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
	public string Name { get; private set; } = "UNKNOWN";
	// Ingame description of item
	public string Description { get; private set; } = "UNKNOWN";
	// Ingame list of durability, damage, armor damage, and range
	public string Stats { get; private set; } = "UNKNOWN";
	// Max durability of item
	public int MaxUses { get; private set; } = 1;
	// Current durability of item
	public int CurrentUses { get; private set; } = 1;
	// Damage of item, -1 = not a weapon
	public int DamagePoints { get; private set; } = -1;
	// Damage of item to armor, -1 = does same damage as DamagePoints
	public int ArmorDamage { get; private set; } = -1;
	// Range of item, -1 = not a ranged weapon
	public int Range { get; private set; } = 0;
	// Whether item can be equipped, enabling it and removing it from inventory
	public bool IsEquipable { get; private set; } = false;
	// Whether item can be attached to vehicles, enabling it and removing it from inventory
	public bool IsAttachable { get; private set; } = false;
	// Whether item is flammable, can be destroyed by fire and helps it spread
	public bool IsFlammable { get; private set; } = false;
	// Whether item stuns enemies when used
	public bool IsStunning { get; private set; } = false;
	private static readonly int lastItemIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> ItemRarityList = GenerateAllRarities();
	private static List<Rarity> GenerateAllRarities()
	{
		return Enumerable.Range(0, lastItemIndex)
						.Select(i => ItemFactory(i).Rarity)
						.ToList();
	}
	public static int GetRandomIndexOfSpecifiedRarity(Rarity specifiedRarity)
	{
		var indices = Enumerable.Range(0, ItemRarityList.Count)
								.Where(i => ItemRarityList[i] == specifiedRarity)
								.ToList();
		if (indices.Count == 0)
			return -1;
		return indices[Random.Range(0, indices.Count)];
	}
	/// <summary>
	/// Decreases item durability by 1 and updates description
	/// </summary>
	public void DecreaseDurability() 
	{
		CurrentUses--;
		Stats = $"\nUP:{CurrentUses}/{MaxUses}";
		if (Type is Types.Weapon) 
		{
			Stats += $"\tDP:{DamagePoints}";
			if (ArmorDamage >= 0)
			{
				Stats += $"\nADP:{ArmorDamage}";
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
	public static ItemInfo ItemFactory(int n)
	{
		ItemInfo Info = new();
		switch (n)
		{
			case 0:
				Info.Tag 			= Tags.MedKit;
				Info.Rarity 		= Rarity.Limited;
				Info.Type 			= Types.Consumable;
				Info.Name 			= "MEDKIT";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsFlammable 	= true;
				Info.Description 	= "Fully heals injuries";
				Info.Stats	 		= $"\nUP:{Info.MaxUses}/{Info.MaxUses}";
				break;
			case 1:
				Info.Tag 			= Tags.ToolKit;
				Info.Rarity 		= Rarity.Scarce;
				Info.Type 			= Types.Consumable;
				Info.Name 			= "TOOLKIT";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.Description 	= "Fully repairs vehicles";
				Info.Stats 			= "\nUP:" + Info.MaxUses;
				break;
			// case ?:
			// 	Info.Tag 			= Tags.FusionCell;
			// 	Info.Rarity 		= Rarity.Common;
			// 	Info.Type 			= Types.Consumable;
			// 	Info.Name 			= "FUSION CELL";
			// 	Info.MaxUses 		= 1; 				// max charge
			// 	Info.CurrentUses 	= Info.MaxUses; 	// current charge
			// 	Info.IsFlammable 	= true;
			// 	Info.Description 	= "Powers vehicles";
			// 	Info.Stats 			= $"\n\UP:{Info.MaxUses}";
			// 	break;
			case 2:
				Info.Tag 			= Tags.Branch;
				Info.Rarity 		= Rarity.Common;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "BRANCH";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 1;
				Info.ArmorDamage 	= 0;
				Info.IsFlammable 	= true;
				Info.Description 	= "Fragile stick";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nADP:{Info.ArmorDamage}";
				break;
			case 3:
				Info.Tag 			= Tags.Knife;
				Info.Rarity 		= Rarity.Limited;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "KNIFE";
				Info.MaxUses 		= 2;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 2;
				Info.ArmorDamage 	= 1;
				Info.Description 	= "Can stab through armor";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nADP:{Info.ArmorDamage}";
				break;
			case 4:
				Info.Tag 			= Tags.Wrench;
				Info.Rarity 		= Rarity.Scarce;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "WRENCH";
				Info.MaxUses 		= 4;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 2;
				Info.ArmorDamage 	= 1;
				Info.Description 	= "Weak against armor";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nADP:{Info.ArmorDamage}";
				break;
			case 5:
				Info.Tag 			= Tags.Mallet;
				Info.Rarity 		= Rarity.Rare;
				Info.Type 			= Types.Weapon;
				Info.MaxUses 		= 6;
				Info.CurrentUses 	= Info.MaxUses;
				Info.Name 			= "MALLET";
				Info.DamagePoints 	= 3;
				Info.ArmorDamage 	= 0;
				Info.IsStunning 	= true;
				Info.Description 	= "Stuns, bounces off armor";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nADP:{Info.ArmorDamage}";
				break;
			// case ?:
			// 	Info.Tag 			= Tags.Axe;
			// 	Info.Rarity 		= Rarity.Rare;
			// 	Info.Type 			= Types.Weapon;
			// 	Info.Name 			= "AXE";
			// 	Info.MaxUses 		= 4;
			// 	Info.CurrentUses 	= Info.MaxUses;
			// 	Info.DamagePoints 	= 4;
			// 	Info.ArmorDamage 	= 2;
			// 	Info.Description 	= "Cuts through armor";
			// 	Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nADP:{Info.ArmorDamage}";
			// 	break;
			case 6:
				Info.Tag 			= Tags.DiamondChainsaw;
				Info.Rarity 		= Rarity.Anomalous;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "CHAINSAW";
				Info.MaxUses 		= 8;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 5;
				Info.Description 	= "Handheld rock saw";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}";
				break;
			case 7:
				Info.Tag 			= Tags.Tranquilizer;
				Info.Rarity 		= Rarity.Scarce;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "TRANQUILIZER";
				Info.MaxUses 		= 2;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 0;
				Info.Range 			= 4;
				Info.IsStunning 	= true;
				Info.Description 	= "Stuns enemies for 1 turn";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
				break;
			// case ?:
			// 	Info.Tag 			= Tags.Carbine;
			// 	Info.Rarity 		= Rarity.Rare;
			// 	Info.Type 			= Types.Weapon;
			// 	Info.Name 			= "CARBINE";
			// 	Info.MaxUses 		= 4;
			// 	Info.CurrentUses 	= Info.MaxUses;
			// 	Info.DamagePoints 	= 3;
			// 	Info.Range 			= 4;
			// 	Info.Description 	= "Fires rifle bullets";
			// 	Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
			// 	break;
			// case ?:
			// 	Info.Tag 			= Tags.Flamethrower;
			// 	Info.Rarity 		= Rarity.Rare;
			// 	Info.Type 			= Types.Weapon;
			// 	Info.Name 			= "FLAMETHROWER";
			// 	Info.MaxUses 		= 4;
			// 	Info.CurrentUses 	= Info.MaxUses;
			// 	Info.DamagePoints 	= 1;
			// 	Info.Range 			= 3;
			// 	Info.IsFlammable 	= true;
			// 	Info.Description 	= "Sprays streaks of fire";
			// 	Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
			// 	break;
			case 8:
				Info.Tag 			= Tags.HuntingRifle;
				Info.Rarity 		= Rarity.Rare;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "HUNTING RIFLE";
				Info.MaxUses 		= 3;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 5;
				Info.Range 			= 10;
				Info.Description 	= "Fires armor piercing rounds";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
				break;
			case 9:
				Info.Tag 			= Tags.PlasmaRailgun;
				Info.Rarity 		= Rarity.Anomalous;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "PLASMA RAILGUN";
				Info.MaxUses 		= 5;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 10;
				Info.Range 			= 5;
				Info.Description 	= "Fires a voltaic plasma bolt";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
				break;
			/*
			case 9:
				Info.Tag 			= Tags.Rock;
				Info.Rarity 		= Rarity.Common;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "ROCK";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 1;
				Info.Range 			= 3;
				Info.Description 	= "Can be thrown again";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
				break;
			case 10:
				Info.Tag 			= Tags.SmokeGrenade;
				Info.Rarity 		= Rarity.Scarce;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "SMOKE GRENADE";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 0;
				Info.Range 			= 3;
				Info.IsStunning 	= true;
				Info.Description 	= "Stuns nearby enemies for 1 turn";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
				break;
			case 11:
				Info.Tag 			= Tags.Dynamite;
				Info.Rarity 		= Rarity.Scarce;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "DYNAMITE";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 5;
				Info.Range 			= 3;
				Info.IsFlammable 	= true;
				Info.Description 	= "Fuse lights after landing";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
				break;
			case 12:
				Info.Tag 			= Tags.StickyGrenade;
				Info.Rarity 		= Rarity.Anomalous;
				Info.Type 			= Types.Weapon;
				Info.Name 			= "STICKY GRENADE";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.DamagePoints 	= 3;
				Info.Range 			= 5;
				Info.IsFlammable 	= true;
				Info.Description 	= "Sticks to enemies before detonating";
				Info.Stats 			= $"\nUP:{Info.MaxUses}/{Info.MaxUses}\tDP:{Info.DamagePoints}\nRP:{Info.Range}";
				break;
			case 14:
				Info.Tag 			= Tags.Helmet;
				Info.Rarity 		= Rarity.Scarce;
				Info.Type 			= Types.Armor;
				Info.Name 			= "HELMET";
				Info.MaxUses 		= 2;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsEquipable 	= true;
				Info.Description 	= "Absorbs 2 melee DP";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 15:
				Info.Tag 			= Tags.Vest;
				Info.Rarity 		= Rarity.Rare;
				Info.Type 			= Types.Armor;
				Info.Name 			= "VEST";
				Info.MaxUses 		= 3;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsEquipable 	= true;
				Info.IsFlammable 	= true;
				Info.Description 	= "Absorbs 3 ranged DP";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 16:
				Info.Tag 			= Tags.GrapheneShield;
				Info.Rarity 		= Rarity.Anomalous;
				Info.Type 			= Types.Armor;
				Info.Name 			= "GRAPHENE SHIELD";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsEquipable 	= true;
				Info.Description 	= "Blocks all DP except boss DP";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 17:
				Info.Tag 			= Tags.Backpack;
				Info.Rarity 		= Rarity.Limited;
				Info.Type 			= Types.Storage;
				Info.Name 			= "BACKPACK";
				Info.MaxUses 		= 1;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsEquipable 	= true;
				Info.IsFlammable 	= true;
				Info.Description 	= "Adds 1 inventory slot";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 18:
				Info.Tag 			= Tags.Crate;
				Info.Rarity 		= Rarity.Rare;
				Info.Type 			= Types.Storage;
				Info.Name 			= "CRATE";
				Info.MaxUses 		= 2;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsAttachable 	= true;
				Info.Description 	= "Adds 2 vehicle storage";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 19:
				Info.Tag 			= Tags.Battery;
				Info.Rarity 		= Rarity.Anomalous;
				Info.Type 			= Types.Storage;
				Info.Name 			= "BATTERY";
				Info.MaxUses 		= 5; 				// max charge
				Info.CurrentUses 	= Info.MaxUses; 	// current charge
				Info.IsAttachable 	= true;
				Info.IsFlammable 	= true;
				Info.Description 	= "Adds 5 fuel slots";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 20:
				Info.Tag 			= Tags.Lightrod;
				Info.Rarity 		= Rarity.Limited;
				Info.Type 			= Types.Utility;
				Info.Name 			= "LIGHTROD";
				Info.IsFlammable 	= true;
				Info.Description 	= "Illuminates surroundings";
				break;
			case 21:
				Info.Tag 			= Tags.Extinguisher;
				Info.Rarity 		= Rarity.Scarce;
				Info.Type 			= Types.Utility;
				Info.Name 			= "EXTINGUISHER";
				Info.MaxUses 		= 4;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsFlammable 	= true;
				Info.Description 	= "Extinguishes fires";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 22:
				Info.Tag 			= Tags.Spotlight;
				Info.Rarity 		= Rarity.Rare;
				Info.Type 			= Types.Utility;
				Info.Name 			= "SPOTLIGHT";
				Info.IsAttachable 	= true;
				Info.Description 	= "Outputs directional light";
				break;
			case 23:
				Info.Tag 			= Tags.Blowtorch;
				Info.Rarity 		= Rarity.Rare;
				Info.Type 			= Types.Utility;
				Info.Name 			= "BLOWTORCH";
				Info.MaxUses 		= 4;
				Info.CurrentUses 	= Info.MaxUses;
				Info.IsFlammable 	= true;
				Info.Description 	= "Sets objects on fire";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 24:
				Info.Tag 			= Tags.ThermalImager;
				Info.Rarity 		= Rarity.Rare;
				Info.Type 			= Types.Utility;
				Info.Name 			= "THERMAL IMAGER";
				Info.MaxUses 		= 4;
				Info.CurrentUses 	= Info.MaxUses;
				Info.Description 	= "Reveals heat signatures";
				Info.Stats 			= $"\n\UP:{Info.MaxUses}";
				break;
			case 25:
				Info.Tag 			= Tags.NightVision;
				Info.Rarity 		= Rarity.Anomalous;
				Info.Type 			= Types.Utility;
				Info.Name 			= "NIGHT VISION";
				Info.IsEquipable 	= true;
				Info.Description 	= "Enables nighttime visibility";
				break;
			*/
		}
		return Info;
	}
}