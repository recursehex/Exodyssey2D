using System.Collections.Generic;
using UnityEngine;

public enum ItemTag
{
    // CONSUMABLE
    MedKit = 0,
    //ToolKit,

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
    //HuntingRifle,
    PlasmaRailgun,

    // ARMOR
    //Helmet,
    //Vest,
    //GrapheneShield,

    // STORAGE
    //HydrogenCanister,
    //Bacpack,
    //FuelTank,
    //Crate,

    // UTILITY
    //Lightrod;
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
/// Contains all item variables, creates items with specific values, & manages items after usage
/// </summary>
public class ItemInfo
{
    public ItemTag tag;                         // Name of item
    public Rarity rarity;                       // Rarity of item
    public ItemType type;                       // Type of item

    public string name;                         // Ingame name of item
    public string description;                  // Ingame desc of item

    public int maxUP = -1;                      // Set to positive value for items with UP, -1 = infinite uses
    public int currentUP = -1;

    public int damagePoints = -1;               // Set only for weapons
    public int range = 0;                       // Maximum distance a Ranged weapon can attack to, 0 = Melee

    public bool isEquipable = false;            // Can be equipped by characters, enabling the item & emptying an inventory slot
    public bool isAttachable = false;           // Can be attached to vehicles, enabling the item
    public bool isFlammable = false;            // Will be destroyed by fire and helps it to spread

    public int shellDamage = -1;                // Set only if weapon does different damage to shelled aliens

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
    public AfterItemUse UseItem(Player player, int selectedIdx)
    {
        AfterItemUse ret = new();

        // Checks by item type category
        switch (type)
        {
            case ItemType.Consumable:
                // If the consumable is a healing item
                if (tag == ItemTag.MedKit && player.currentHP < player.maxHP && player.currentAP > 0)
                {
                    player.ChangeHP(player.maxHP);
                    player.ChangeAP(-1);
                    currentUP--;
                    description = "Use:Heals all HP" +
                    "\n" +
                    "UP:" + currentUP + "/" + maxUP;
                    ret.consumableWasUsed = true;
                }
                break;
            // If the item is a weapon, as clicking it will select it
            case ItemType.Weapon:
                bool weaponIsSelected = ProcessSelection(player.inventoryUI.GetCurrentSelected(), selectedIdx);
                if (!weaponIsSelected)
                {
                    selectedIdx = -1;
                }
                player.inventoryUI.SetCurrentSelected(selectedIdx);
                // Reset damageToEnemy since weapon was deselected
                if (selectedIdx == -1)
                {
                    player.damagePoints = 0;
                    player.ClearTargetsAndTracers();
                }
                // Set damageToEnemy as the damage of the weapon
                else
                {
                    player.damagePoints = damagePoints;
                    player.DrawTargetsAndTracers();
                }
                ret.selectedIdx = selectedIdx;
                break;
        }
        // If the item runs out of UP, it needs to be removed
        if (currentUP == 0)
        {
            ret.needToRemoveItem = true;
        }
        return ret;
    }

    /// <summary>
    /// Called by UseItem when weapon is selected or deselected
    /// </summary>
    /// <param name="posOld"></param>
    /// <param name="posNew"></param>
    public bool ProcessSelection(int posOld, int posNew)
    {
        // Deselect old item
        if (posOld == posNew && posOld != -1)
        {
            GameObject.Find("InventoryPressed" + posOld).transform.localScale = Vector3.one;
            return false;
        }
        else
        {
            // Deselect old item & select new item
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
    /// Ensures damagePoints is reset after Player drops a weapon
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public bool ProcessDamageAfterWeaponDrop(Player p)
    {
        if (type == ItemType.Weapon)
        {
            p.damagePoints = 0;
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
        if (currentUP > 0)
        {
            currentUP--;
            description = "Use:Equip weapon" +
            "\n" +
            "UP:" + currentUP + "/" + maxUP +
            "\t" +
            "DP:" + damagePoints;
        }
        // If a weapon has infinite UP
        else
        {
            description = "Use:Equip weapon" +
                "\n" +
                "UP:" + "\u221e" +
                "\t" +
                "DP:" + damagePoints;
        }
        if (range > 0)
            description += "\n" + "RP:" + range;

        return (currentUP == 0);
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
                inf.maxUP = 1;
                inf.currentUP = inf.maxUP;
                inf.isFlammable = true;
                inf.description = "Use:Heals user" +
                    "\n" +
                    "UP:" + inf.maxUP + "/" + inf.maxUP;
                break;
            case 1:
                inf.tag = ItemTag.Branch;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "BRANCH";
                inf.maxUP = 2;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 1;
                inf.isFlammable = true;
                inf.description = "Use:Equip weapon" +
                    "\n" +
                    "UP:" + inf.maxUP + "/" + inf.maxUP +
                    "\t" +
                    "DP:" + inf.damagePoints;
                break;
            case 2:
                inf.tag = ItemTag.DiamondChainsaw;
                inf.rarity = Rarity.Rare;
                //inf.rarity = Rarity.Anomalous;
                inf.type = ItemType.Weapon;
                inf.name = "CHAINSAW";
                inf.maxUP = 8;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 5;
                inf.description = "Use:Equip weapon" +
                    "\n" +
                    "UP:" + inf.maxUP +
                    "\t" +
                    "DP:" + inf.damagePoints;
                break;
            case 3:
                inf.tag = ItemTag.PlasmaRailgun;
                //inf.rarity = Rarity.Anomalous;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "PLASMA RAILGUN";
                inf.damagePoints = 10;
                inf.range = 5;
                inf.description = "Use:Equip weapon" +
                    "\n" +
                    "UP:" + "\u221e" +
                    "\t" +
                    "DP:" + inf.damagePoints +
                    "\n" +
                    "RP:" + inf.range;
                break;
                /*
                case 3:
                    inf.tag = ItemTag.ToolKit;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Consumable;
                    inf.name = "TOOLKIT";
                    inf.maxUP = 1;
                    inf.currentUP = inf.maxUP;
                    inf.description = "Use:Repairs vehicle" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 4:
                    inf.tag = ItemTag.Knife;
                    inf.rarity = Rarity.Limited;
                    inf.type = ItemType.Weapon;
                    inf.name = "KNIFE";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 5:
                    inf.tag = ItemTag.Wrench;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Weapon;
                    inf.name = "WRENCH";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.shellDamage = 0;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 6:
                    inf.tag = ItemTag.Mallet;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.maxUP = 6;
                    inf.currentUP = inf.maxUP;
                    inf.name = "MALLET";
                    inf.damagePoints = 3;
                    inf.shellDamage = 0;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 7:
                    inf.tag = ItemTag.Axe;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "AXE";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 4;
                    inf.shellDamage = 2;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 7:
                    inf.tag = ItemTag.Rock;
                    inf.rarity = Rarity.Common;
                    inf.type = ItemType.Weapon;
                    inf.name = "ROCK";
                    inf.maxUP = 1;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 1;
                    inf.range = 3;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 7:
                    inf.tag = ItemTag.SmokeGrenade;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Weapon;
                    inf.name = "SMOKE GRENADE";
                    inf.maxUP = 1;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 0;
                    inf.range = 3;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 7:
                    inf.tag = ItemTag.Dynamite;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Weapon;
                    inf.name = "DYNAMITE";
                    inf.maxUP = 1;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 5;
                    inf.range = 3;
                    inf.isFlammable = true;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 7:
                    inf.tag = ItemTag.StickyGrenade;
                    inf.rarity = Rarity.Anomalous;
                    inf.type = ItemType.Weapon;
                    inf.name = "STICKY GRENADE";
                    inf.maxUP = 1;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 3;
                    inf.range = 5;
                    inf.isFlammable = true;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 11:
                    inf.tag = ItemTag.HydrogenCanister;
                    inf.rarity = Rarity.Common;
                    inf.type = ItemType.Storage;
                    inf.name = "HYDROGEN CANISTER";
                    inf.maxUP = 5; // max fuel
                    inf.currentUP = inf.maxUP; // current fuel
                    inf.isFlammable = true;
                    inf.description = "Use:???";
                    break;

                case 13:
                    inf.tag = ItemTag.Helmet;
                    inf.rarity = Rarity.Common;
                    inf.type = ItemType.Armor;
                    inf.name = "HELMET";
                    inf.maxUP = 2;
                    inf.isEquipable = true;
                    inf.description = "Use:Equip helmet";
                    break;

                case 13:
                    inf.tag = ItemTag.Vest;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Armor;
                    inf.name = "VEST";
                    inf.maxUP = 3;
                    inf.isEquipable = true;
                    inf.isFlammable = true;
                    inf.description = "Use:Equip vest";
                    break;

                case 13:
                    inf.tag = ItemTag.GrapheneShield;
                    inf.rarity = Rarity.Anomalous;
                    inf.type = ItemType.Armor;
                    inf.name = "GRAPHENE SHIELD";
                    inf.isEquipable = true;
                    inf.description = "Use:Equip shield";
                    break;

                case 13:
                    inf.tag = ItemTag.Backpack;
                    inf.rarity = Rarity.Limited;
                    inf.type = ItemType.Storage;
                    inf.name = "BACKPACK";
                    inf.isEquipable = true;
                    inf.isFlammable = true;
                    inf.description = "Use:Equip backpack";
                    break;

                case 12:
                    inf.tag = ItemTag.FuelTank;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "FUEL TANK";
                    inf.maxUP = 10; // max fuel
                    inf.currentUP = inf.maxUP; // current fuel
                    inf.isAttachable = true;
                    inf.isFlammable = true;
                    inf.description = "Use:Attach tank";
                    break;
                
                case 14:
                    inf.tag = ItemTag.Crate;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "CRATE";
                    inf.isAttachable = true;
                    inf.description = "Use:Attach crate";
                    break;
                case 16:
                    inf.tag = ItemTag.Lightrod;
                    inf.rarity = Rarity.Limited;
                    inf.type = ItemType.Utility;
                    inf.name = "LIGHTROD";
                    inf.isFlammable = true;
                    inf.description = "Use:Toggle light";
                    break;

                case 16:
                    inf.tag = ItemTag.Extinguisher;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Utility;
                    inf.name = "EXTINGUISHER";
                    inf.isFlammable = true;
                    inf.description = "Use:Equip tool";
                    break;

                case 17:
                    inf.tag = ItemTag.Spotlight;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "SPOTLIGHT";
                    inf.isAttachable = true;
                    inf.description = "Use:Attach light";
                    break;

                case 19:
                    inf.tag = ItemTag.Blowtorch;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "BLOWTORCH";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.isFlammable = true;
                    inf.description = "Use:Equip tool";
                    break;

                case 19:
                    inf.tag = ItemTag.ThermalImager;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "THERMAL IMAGER";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.description = "Use:Take picture";
                    break;

                case 19:
                    inf.tag = ItemTag.NightVision;
                    inf.rarity = Rarity.Anomalous;
                    inf.type = ItemType.Utility;
                    inf.name = "NIGHT VISION";
                    inf.isEquipable = true;
                    inf.description = "Use:Equip goggles";
                    break;

                case 20:
                    inf.tag = ItemTag.Tranquilizer;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Weapon;
                    inf.name = "TRANQUILIZER";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 0;
                    inf.range = 4;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 21:
                    inf.tag = ItemTag.Carbine;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "CARBINE";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 3;
                    inf.range = 4;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 22:
                    inf.tag = ItemTag.Flamethrower;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "FLAMETHROWER";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 1;
                    inf.range = 3;
                    inf.isFlammable = true;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 23:
                    inf.tag = ItemTag.HuntingRifle;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "HUNTING RIFLE";
                    inf.maxUP = 3;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 5;
                    inf.range = 99;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;
                */
        }
        return inf;
    }
}
