using System.Collections.Generic;
using UnityEngine;
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
	public Tags Tag 			= Tags.Unknown;					// Name of item
	public Rarity Rarity 		= Rarity.Common;				// Rarity of item
	public Types Type 			= Types.Unknown;				// Type of item
	public string Name 			{ get; private set; }			// Ingame name of item
	public string Description 	{ get; private set; }			// Ingame description of item
	public string Stats 		{ get; private set; }			// Ingame list of durability, damage, armor damage, and range
	private readonly int maxUses = 1;							// Max durability of item
	public int CurrentUses 		{ get; private set; } = 1;		// Current durability of item
	public int DamagePoints 	{ get; private set; } = -1;		// Damage of item, -1 = not a weapon
	public int ArmorDamage 		{ get; private set; } = -1;		// Damage of item to armor, -1 = does same damage as DamagePoints
	public int Range 			{ get; private set; } = -1;		// Range of item, -1 = not a ranged weapon
	public bool IsEquipable 	{ get; private set; } = false;	// If item can be equipped, enabling and removing from inventory
	public bool IsAttachable 	{ get; private set; } = false;	// If item can be attached to vehicles, enabling and removing from inventory
	public bool IsFlammable 	{ get; private set; } = false;	// If item is flammable, can be destroyed by fire and helps it spread
	public bool IsStunning 		{ get; private set; } = false;	// If item stuns enemies when used
	private static readonly int lastItemIndex = (int)Tags.Unknown;
	private static readonly List<Rarity> ItemRarityList = GenerateAllRarities();
	private static List<Rarity> GenerateAllRarities()
	{
		return Enumerable.Range(0, lastItemIndex)
						 .Select(i => new ItemInfo(i).Rarity)
						 .ToList();
	}
	public static int GetRandomIndexFrom(Rarity Rarity)
	{
		var indices = Enumerable.Range(0, ItemRarityList.Count)
								.Where(i => ItemRarityList[i] == Rarity)
								.ToList();
		if (indices.Count == 0)
			return -1;
		return indices[Random.Range(0, indices.Count)];
	}
	/// <summary>
	/// Decreases item durability by amount and updates description
	/// </summary>
	public void DecreaseDurability(int amount = 1)
	{
		CurrentUses -= amount;
		Stats = $"\nUP:{CurrentUses}/{maxUses}";
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
		switch (n)
		{
			case 0:
				Tag 			= Tags.MedKit;
				Rarity 			= Rarity.Limited;
				Type 			= Types.Consumable;
				Name 			= "MEDKIT";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				IsFlammable 	= true;
				Description 	= "Fully heals injuries";
				Stats	 		= $"\nUP:{maxUses}/{maxUses}";
				break;
			case 1:
				Tag 			= Tags.ToolKit;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Consumable;
				Name 			= "TOOLKIT";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				Description 	= "Fully repairs vehicles";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			case 2:
				Tag 			= Tags.PowerCell;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Consumable;
				Name 			= "POWER CELL";
				maxUses 		= 5;
				CurrentUses 	= maxUses;
				IsFlammable 	= true;
				Description 	= "Powers vehicles";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			case 3:
				Tag 			= Tags.Branch;
				Rarity 			= Rarity.Common;
				Type 			= Types.Weapon;
				Name 			= "BRANCH";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				DamagePoints 	= 1;
				ArmorDamage 	= 0;
				IsFlammable 	= true;
				Description 	= "Fragile stick";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nAD:{ArmorDamage}";
				break;
			case 4:
				Tag 			= Tags.Knife;
				Rarity 			= Rarity.Limited;
				Type 			= Types.Weapon;
				Name 			= "KNIFE";
				maxUses 		= 2;
				CurrentUses 	= maxUses;
				DamagePoints 	= 2;
				ArmorDamage 	= 1;
				Description 	= "Can stab through armor";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nAD:{ArmorDamage}";
				break;
			case 5:
				Tag 			= Tags.Wrench;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Weapon;
				Name 			= "WRENCH";
				maxUses 		= 4;
				CurrentUses 	= maxUses;
				DamagePoints 	= 2;
				ArmorDamage 	= 1;
				Description 	= "Weak against armor";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nAD:{ArmorDamage}";
				break;
			case 6:
				Tag 			= Tags.Mallet;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Weapon;
				maxUses 		= 6;
				CurrentUses 	= maxUses;
				Name 			= "MALLET";
				DamagePoints 	= 3;
				ArmorDamage 	= 0;
				IsStunning 		= true;
				Description 	= "Stuns, bounces off armor";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nAD:{ArmorDamage}";
				break;
			case 7:
				Tag 			= Tags.FireAxe;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Weapon;
				Name 			= "FIRE AXE";
				maxUses 		= 4;
				CurrentUses 	= maxUses;
				DamagePoints 	= 4;
				ArmorDamage 	= 2;
				Description 	= "Cuts through armor";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nAD:{ArmorDamage}";
				break;
			case 8:
				Tag 			= Tags.Chainsaw;
				Rarity 			= Rarity.Anomalous;
				Type 			= Types.Weapon;
				Name 			= "CHAINSAW";
				maxUses 		= 8;
				CurrentUses 	= maxUses;
				DamagePoints 	= 5;
				Description 	= "Handheld rock saw";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}";
				break;
			case 9:
				Tag 			= Tags.Tranquilizer;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Weapon;
				Name 			= "TRANQUILIZER";
				maxUses 		= 2;
				CurrentUses 	= maxUses;
				DamagePoints 	= 0;
				Range 			= 4;
				IsStunning 		= true;
				Description 	= "Stuns enemies for 1 turn";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			case 10:
				Tag 			= Tags.Carbine;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Weapon;
				Name 			= "CARBINE";
				maxUses 		= 4;
				CurrentUses 	= maxUses;
				DamagePoints 	= 3;
				Range 			= 4;
				Description 	= "Fires rifle bullets";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			// case 11:
			// 	Tag 			= Tags.Flamethrower;
			// 	Rarity 			= Rarity.Rare;
			// 	Type 			= Types.Weapon;
			// 	Name 			= "FLAMETHROWER";
			// 	maxUses 		= 4;
			// 	CurrentUses 	= maxUses;
			// 	DamagePoints 	= 1;
			// 	Range 			= 3;
			// 	IsFlammable 	= true;
			// 	Description 	= "Sprays streaks of fire";
			// 	Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
			// 	break;
			case 11:
				Tag 			= Tags.HuntingRifle;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Weapon;
				Name 			= "HUNTING RIFLE";
				maxUses 		= 3;
				CurrentUses 	= maxUses;
				DamagePoints 	= 5;
				Range 			= 10;
				Description 	= "Fires armor piercing rounds";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			case 12:
				Tag 			= Tags.PlasmaRailgun;
				Rarity 			= Rarity.Anomalous;
				Type 			= Types.Weapon;
				Name 			= "PLASMA RAILGUN";
				maxUses 		= 5;
				CurrentUses 	= maxUses;
				DamagePoints 	= 10;
				Range 			= 5;
				Description 	= "Fires a voltaic bolt of plasma";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			case 13:
				Tag 			= Tags.Rock;
				Rarity 			= Rarity.Common;
				Type 			= Types.Weapon;
				Name 			= "ROCK";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				DamagePoints 	= 1;
				Range 			= 3;
				Description 	= "Can be thrown again";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			/*
			case ?:
				Tag 			= Tags.SmokeGrenade;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Weapon;
				Name 			= "SMOKE GRENADE";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				DamagePoints 	= 0;
				Range 			= 3;
				IsStunning 		= true;
				Description 	= "Stuns nearby enemies for 1 turn";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			case ?:
				Tag 			= Tags.Dynamite;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Weapon;
				Name 			= "DYNAMITE";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				DamagePoints 	= 5;
				Range 			= 3;
				IsFlammable 	= true;
				Description 	= "Explodes after enemy turn";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			case ?:
				Tag 			= Tags.StickyGrenade;
				Rarity 			= Rarity.Anomalous;
				Type 			= Types.Weapon;
				Name 			= "STICKY GRENADE";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				DamagePoints 	= 3;
				Range 			= 5;
				IsFlammable 	= true;
				Description 	= "Sticks to enemy and explodes";
				Stats 			= $"\nUP:{maxUses}/{maxUses}\tDP:{DamagePoints}\nRP:{Range}";
				break;
			case ?:
				Tag 			= Tags.Helmet;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Armor;
				Name 			= "HELMET";
				maxUses 		= 2;
				CurrentUses 	= maxUses;
				IsEquipable 	= true;
				Description 	= "Absorbs 2 melee DP";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			case ?:
				Tag 			= Tags.Vest;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Armor;
				Name 			= "VEST";
				maxUses 		= 3;
				CurrentUses 	= maxUses;
				IsEquipable 	= true;
				IsFlammable 	= true;
				Description 	= "Absorbs 3 ranged DP";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			case ?:
				Tag 			= Tags.GrapheneShield;
				Rarity 			= Rarity.Anomalous;
				Type 			= Types.Armor;
				Name 			= "GRAPHENE SHIELD";
				maxUses 		= 1;
				CurrentUses 	= maxUses;
				Description 	= "Blocks all DP except boss DP";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			case ?:
				Tag 			= Tags.Battery;
				Rarity 			= Rarity.Anomalous;
				Type 			= Types.Utility;
				Name 			= "BATTERY";
				maxUses 		= 5; 		// max charge
				CurrentUses 	= maxUses; 	// current charge
				IsAttachable 	= true;
				IsFlammable 	= true;
				Description 	= "Adds 5 power charges";
				Stats 			= $"\nCHARGE:{maxUses}/{maxUses}";
				break;
			*/
			case 14:
				Tag 			= Tags.Flare;
				Rarity 			= Rarity.Limited;
				Type 			= Types.Utility;
				Name 			= "FLARE";
				IsFlammable 	= true;
				Description 	= "Temporary light";
				break;
			/*
			case ?:
				Tag 			= Tags.Lightrod;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Utility;
				Name 			= "LIGHTROD";
				IsFlammable 	= true;
				Description 	= "Illuminates surroundings";
				break;
			*/
			case 15:
				Tag 			= Tags.Extinguisher;
				Rarity 			= Rarity.Scarce;
				Type 			= Types.Utility;
				Name 			= "EXTINGUISHER";
				maxUses 		= 4;
				CurrentUses 	= maxUses;
				IsFlammable 	= true;
				Description 	= "Extinguishes fires";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			/*
			case ?:
				Tag 			= Tags.Spotlight;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Utility;
				Name 			= "SPOTLIGHT";
				IsAttachable 	= true;
				Description 	= "Outputs directional light";
				break;
			case ?:
				Tag 			= Tags.Blowtorch;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Utility;
				Name 			= "BLOWTORCH";
				maxUses 		= 4;
				CurrentUses 	= maxUses;
				IsFlammable 	= true;
				Description 	= "Sets objects on fire";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			case ?:
				Tag 			= Tags.ThermalImager;
				Rarity 			= Rarity.Rare;
				Type 			= Types.Utility;
				Name 			= "THERMAL IMAGER";
				maxUses 		= 4;
				CurrentUses 	= maxUses;
				Description 	= "Reveals heat signatures";
				Stats 			= $"\nUP:{maxUses}/{maxUses}";
				break;
			case ?:
				Tag 			= Tags.NightVision;
				Rarity 			= Rarity.Anomalous;
				Type 			= Types.Utility;
				Name 			= "NIGHT VISION";
				IsEquipable 	= true;
				Description 	= "Enables nighttime visibility";
				break;
			*/
		}
	}
}