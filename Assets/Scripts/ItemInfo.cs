using System.Collections.Generic;
using UnityEngine;

public enum ItemTag
{
    // CONSUMABLE
    MedKit = 0,
    //ToolKit,
    //FusionCell,

    // MELEE
    Branch,
    //Knife,
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

public enum ItemType
{
    Utility = 0,
    Weapon,
    Consumable,
    Armor,
    Storage,

    Unknown,
}

public class AfterItemUse
{
    public bool needToRemoveItem = false;
    public int selectedIdx = -1;
    public bool consumableWasUsed = false;
}

/// <summary>
/// Contains all item variables, creates items with specific values, and manages items after usage
/// </summary>
public class ItemInfo
{
    public ItemTag tag;                 // Name of item
    public Rarity rarity;               // Rarity of item
    public ItemType type;               // Type of item

    public string name;                 // Ingame name of item
    public string description;          // Ingame desc of item
    public string stats = "";           // Ingame list of durability, damage, shell damage, range

    public int maxDurability = -1;      // Max uses item has
    public int currentDurability = -1;  // Current uses item has left

    public int damage = -1;             // Set only for weapons
    public int range = 0;               // Maximum distance a Ranged weapon can attack to, 0 = Melee

    public bool isEquipable = false;    // Can be equipped by characters, enabling the item & emptying an inventory slot
    public bool isAttachable = false;   // Can be attached to vehicles, enabling the item
    public bool isFlammable = false;    // Will be destroyed by fire and helps it to spread

    public int shellDamage = -1;        // Set only if weapon does different damage to shelled aliens

    public static int lastItemIdx = (int)ItemTag.Unknown;

    static public List<Rarity> GenerateAllRarities()
    {
        List<Rarity> ret = new();
        for (int i = 0; i < lastItemIdx; i++)
        {
            ItemInfo item = ItemFactory(i);
            ret.Add(item.rarity);
        }
        return ret;
    }
    /// <summary>
    /// Called when Player clicks on an item in an inventory slot
    /// </summary>
    /// <param name="player"></param>
    /// <param name="selectedIdx"></param>
    /// <returns></returns>
    public AfterItemUse ClickItem(Player player, int selectedIdx)
    {
        AfterItemUse ret = new();
        // Checks by item type category
        switch (type)
        {
            case ItemType.Consumable:
                // If the consumable is a MedKit
                if (tag == ItemTag.MedKit && player.currentHealth < player.maxHealth && player.currentEnergy > 0)
                {
                    player.ChangeHealth(player.maxHealth);
                    player.ChangeEnergy(-1);
                    currentDurability--;
                    stats = "\n" + "UP:" + currentDurability + "/" + maxDurability;
                    ret.consumableWasUsed = true;
                }
                break;
            // Selects or unselects a weapon
            case ItemType.Weapon:
                bool weaponIsSelected = ProcessSelection(player.inventoryUI.GetCurrentSelected(), selectedIdx);
                if (!weaponIsSelected)
                {
                    selectedIdx = -1;
                }
                player.inventoryUI.SetCurrentSelected(selectedIdx);
                // Reset damageToEnemy if weapon is unselected
                if (selectedIdx == -1)
                {
                    player.damage = 0;
                    player.ClearTargetsAndTracers();
                }
                // Set damageToEnemy as the damage of the weapon
                else
                {
                    player.damage = damage;
                    player.DrawTargetsAndTracers();
                }
                ret.selectedIdx = selectedIdx;
                break;
        }
        // If the item runs out of UP, it needs to be removed
        if (currentDurability == 0)
        {
            ret.needToRemoveItem = true;
        }
        return ret;
    }
    /// <summary>
    /// Called by UseItem when weapon is selected or unselected
    /// </summary>
    /// <param name="posOld"></param>
    /// <param name="posNew"></param>
    public bool ProcessSelection(int posOld, int posNew)
    {
        // Unselect old item
        if (posOld == posNew && posOld != -1)
        {
            GameObject.Find("InventoryPressed" + posOld).transform.localScale = Vector3.one;
            return false;
        }
        else
        {
            // Unselect old item & select new item
            if (posOld != -1)
            {
                GameObject.Find("InventoryPressed" + posOld).transform.localScale = Vector3.one;
            }
            if (posNew != -1)
            {
                GameObject.Find("InventoryPressed" + posNew).transform.localScale = Vector3.zero;
            }
        }
        return true;
    }
    /// <summary>
    /// Ensures damage is reset after Player drops a weapon
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public bool ProcessDamageAfterWeaponDrop(Player p)
    {
        if (type == ItemType.Weapon)
        {
            p.damage = 0;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Changes UP of a weapon after usage
    /// </summary>
    /// <returns></returns>
    public bool UpdateWeaponUP()
    {
        currentDurability--;
        stats = "\n" + "UP:" + currentDurability + "/" + maxDurability + "\t" + "DP:" + damage;

        if (shellDamage >= 0)
            stats += "\n" + "SDP:" + shellDamage;

        if (range > 0)
            stats += "\n" + "RP:" + range;

        return currentDurability == 0;
    }
    /// <summary>
    /// Returns percentage for a desired rarity
    /// </summary>
    /// <returns></returns>
    public static Dictionary<Rarity, int> RarityPercentMap()
    {
        Dictionary<Rarity, int> RarityToPercentage = new()
        {
            [Rarity.Common] = 35,
            [Rarity.Limited] = 30,
            [Rarity.Scarce] = 20,
            [Rarity.Rare] = 10,
            [Rarity.Anomalous] = 5,
        };
        return RarityToPercentage;
    }
    /// <summary>
    /// Returns info for a desired item 
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static ItemInfo ItemFactory(int n)
    {
        ItemInfo inf = new();
        switch (n)
        {
            case 0:
                inf.tag = ItemTag.MedKit;
                inf.rarity = Rarity.Limited;
                inf.type = ItemType.Consumable;
                inf.name = "MEDKIT";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.isFlammable = true;
                inf.description = "Heals oneself";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability;
                break;
            case 1:
                inf.tag = ItemTag.Branch;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "BRANCH";
                inf.maxDurability = 2;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 1;
                inf.shellDamage = 0;
                inf.isFlammable = true;
                inf.description = "Fragile stick";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "SDP:" + inf.shellDamage;
                break;
            case 2:
                inf.tag = ItemTag.DiamondChainsaw;
                inf.rarity = Rarity.Rare;
                //inf.rarity = Rarity.Anomalous;
                inf.type = ItemType.Weapon;
                inf.name = "CHAINSAW";
                inf.maxDurability = 8;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 5;
                inf.description = "Handheld rock saw";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage;
                break;
            case 3:
                inf.tag = ItemTag.PlasmaRailgun;
                //inf.rarity = Rarity.Anomalous;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "PLASMA RAILGUN";
                inf.maxDurability = 5;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 10;
                inf.range = 5;
                inf.description = "Fires a voltaic plasma bolt";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;
            /*
            case 4:
                inf.tag = ItemTag.ToolKit;
                inf.rarity = Rarity.Scarce;
                inf.type = ItemType.Consumable;
                inf.name = "TOOLKIT";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.description = "Repairs vehicles";
                inf.stats = "\n" + "UP:" + inf.maxDurability;
                break;

            case 5:
                inf.tag = ItemTag.Knife;
                inf.rarity = Rarity.Limited;
                inf.type = ItemType.Weapon;
                inf.name = "KNIFE";
                inf.maxDurability = 3;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 2;
                inf.shellDamage = 1;
                inf.description = "Can stab shelled aliens";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "SDP:" + inf.shellDamage;
                break;

            case 6:
                inf.tag = ItemTag.Wrench;
                inf.rarity = Rarity.Scarce;
                inf.type = ItemType.Weapon;
                inf.name = "WRENCH";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 2;
                inf.shellDamage = 0;
                inf.description = "Useless for shelled aliens";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "SDP:" + inf.shellDamage;
                break;

            case 7:
                inf.tag = ItemTag.Mallet;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Weapon;
                inf.maxDurability = 6;
                inf.currentDurability = inf.maxDurability;
                inf.name = "MALLET";
                inf.damage = 3;
                inf.shellDamage = 0;
                inf.description = "Bounces off shelled aliens";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "SDP:" + inf.shellDamage;
                break;

            case 8:
                inf.tag = ItemTag.Axe;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Weapon;
                inf.name = "AXE";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 4;
                inf.shellDamage = 2;
                inf.description = "Can cut shelled aliens";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "SDP:" + inf.shellDamage;
                break;

            case 9:
                inf.tag = ItemTag.Rock;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "ROCK";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 1;
                inf.range = 3;
                inf.description = "Can be thrown again";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;

            case 10:
                inf.tag = ItemTag.SmokeGrenade;
                inf.rarity = Rarity.Scarce;
                inf.type = ItemType.Weapon;
                inf.name = "SMOKE GRENADE";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 0;
                inf.range = 3;
                inf.description = "Stuns nearby enemies for 1 turn";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;

            case 11:
                inf.tag = ItemTag.Dynamite;
                inf.rarity = Rarity.Scarce;
                inf.type = ItemType.Weapon;
                inf.name = "DYNAMITE";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 5;
                inf.range = 3;
                inf.isFlammable = true;
                inf.description = "Fuse lights after landing";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;

            case 12:
                inf.tag = ItemTag.StickyGrenade;
                inf.rarity = Rarity.Anomalous;
                inf.type = ItemType.Weapon;
                inf.name = "STICKY GRENADE";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 3;
                inf.range = 5;
                inf.isFlammable = true;
                inf.description = "Sticks to enemies before detonating";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;

            case 13:
                inf.tag = ItemTag.FusionCell;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Consumable;
                inf.name = "FUSION CELL";
                inf.maxDurability = 5; // max charge
                inf.currentDurability = inf.maxDurability; // current charge
                inf.isFlammable = true;
                inf.description = "Powers vehicles";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 14:
                inf.tag = ItemTag.Helmet;
                inf.rarity = Rarity.Scarce;
                inf.type = ItemType.Armor;
                inf.name = "HELMET";
                inf.maxDurability = 2;
                inf.currentDurability = inf.maxDurability;
                inf.isEquipable = true;
                inf.description = "Absorbs 2 melee DP";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 15:
                inf.tag = ItemTag.Vest;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Armor;
                inf.name = "VEST";
                inf.maxDurability = 3;
                inf.currentDurability = inf.maxDurability;
                inf.isEquipable = true;
                inf.isFlammable = true;
                inf.description = "Absorbs 3 ranged DP";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 16:
                inf.tag = ItemTag.GrapheneShield;
                inf.rarity = Rarity.Anomalous;
                inf.type = ItemType.Armor;
                inf.name = "GRAPHENE SHIELD";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.isEquipable = true;
                inf.description = "Blocks all DP except boss DP";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 17:
                inf.tag = ItemTag.Backpack;
                inf.rarity = Rarity.Limited;
                inf.type = ItemType.Storage;
                inf.name = "BACKPACK";
                inf.maxDurability = 1;
                inf.currentDurability = inf.maxDurability;
                inf.isEquipable = true;
                inf.isFlammable = true;
                inf.description = "Adds 1 inventory slot";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 18:
                inf.tag = ItemTag.Crate;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Storage;
                inf.name = "CRATE";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.isAttachable = true;
                inf.description = "Adds 4 vehicle storage";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 19:
                inf.tag = ItemTag.Battery;
                inf.rarity = Rarity.Anomalous;
                inf.type = ItemType.Storage;
                inf.name = "BATTERY";
                inf.maxDurability = 5; // max charge
                inf.currentDurability = inf.maxDurability; // current charge
                inf.isAttachable = true;
                inf.isFlammable = true;
                inf.description = "Adds 5 fuel slots";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 20:
                inf.tag = ItemTag.Lightrod;
                inf.rarity = Rarity.Limited;
                inf.type = ItemType.Utility;
                inf.name = "LIGHTROD";
                inf.isFlammable = true;
                inf.description = "Illuminates surroundings";
                break;

            case 21:
                inf.tag = ItemTag.Extinguisher;
                inf.rarity = Rarity.Scarce;
                inf.type = ItemType.Utility;
                inf.name = "EXTINGUISHER";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.isFlammable = true;
                inf.description = "Extinguishes burning tiles";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 22:
                inf.tag = ItemTag.Spotlight;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Utility;
                inf.name = "SPOTLIGHT";
                inf.isAttachable = true;
                inf.description = "Outputs directional light";
                break;

            case 23:
                inf.tag = ItemTag.Blowtorch;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Utility;
                inf.name = "BLOWTORCH";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.isFlammable = true;
                inf.description = "Starts fires on tiles or enemies";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 24:
                inf.tag = ItemTag.ThermalImager;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Utility;
                inf.name = "THERMAL IMAGER";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.description = "Take infrared picture";
                inf.stats = "\n\" + "UP:" + inf.maxDurability;
                break;

            case 25:
                inf.tag = ItemTag.NightVision;
                inf.rarity = Rarity.Anomalous;
                inf.type = ItemType.Utility;
                inf.name = "NIGHT VISION";
                inf.isEquipable = true;
                inf.description = "Enables nighttime visibility";
                break;

            case 26:
                inf.tag = ItemTag.Tranquilizer;
                inf.rarity = Rarity.Scarce;
                inf.type = ItemType.Weapon;
                inf.name = "TRANQUILIZER";
                inf.maxDurability = 2;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 0;
                inf.range = 4;
                inf.description = "Stuns enemies for 1 turn";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;

            case 27:
                inf.tag = ItemTag.Carbine;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Weapon;
                inf.name = "CARBINE";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 3;
                inf.range = 4;
                inf.description = "Fires rifle bullets";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;

            case 28:
                inf.tag = ItemTag.Flamethrower;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Weapon;
                inf.name = "FLAMETHROWER";
                inf.maxDurability = 4;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 1;
                inf.range = 3;
                inf.isFlammable = true;
                inf.description = "Sprays a streak of fire";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;
            */

            case 4:
                inf.tag = ItemTag.HuntingRifle;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Weapon;
                inf.name = "HUNTING RIFLE";
                inf.maxDurability = 3;
                inf.currentDurability = inf.maxDurability;
                inf.damage = 5;
                inf.range = 10;
                inf.description = "Fires piercing bullets";
                inf.stats = "\n" + "UP:" + inf.maxDurability + "/" + inf.maxDurability + "\t" + "DP:" + inf.damage + "\n" + "RP:" + inf.range;
                break;
        }
        return inf;
    }
}
