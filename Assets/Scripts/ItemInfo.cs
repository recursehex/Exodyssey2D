using System.Collections.Generic;
using UnityEngine;

public enum ItemTag
{
    // CONSUMABLE
    MedKit = 0,
    MedKitPlus,
    //RovKit,

    // MELEE
    Branch,
    //Knife,
    //SteelBeam,
    //Mallet,
    //Axe,
    //HonedGavel,
    //TribladeRotator,
    //BladeOfEternity,

    // RANGED
    //Rock
    //Flamethrower,
    //ShellPiercer
    //CatalyticRadiator
    //PFL,
    //BrinkOfExtinction,
    //BorrowedSpacetime,
    LightningRailgun,
    //PaintBlaster,

    // STORAGE
    //HydrogenCanister,
    //ExternalTank,
    //Backpack,
    //StorageCrate,

    // UTILITY
    //Flashlight,
    //Lightrod,
    //Spotlight,
    //Matchbox,
    //Blowtorch,
    //Extinguisher,
    //RangeScanner,
    //AudioLocalizer,
    //ThermalImager,
    //QuantumRelocator,
    //TemporalSedative,

    Unknown,
}

public enum ItemType
{
    Utility = 0,
    Weapon,
    Consumable,
    Armor,
    Storage,
    Platonic,

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

    public int healingPoints = -1;              // Set to positive value for items that heal Players, -1 = does not heal

    public int damagePoints = -1;               // Set only for items with inf.ItemType = Weapon
    public bool isRanged = false;               // false = Melee, true = Ranged
    public int range = -1;                      // Maximum distance a Ranged weapon can attack to

    public bool isEquipable = false;            // Can be equipped by characters, enabling the item & emptying an inventory slot
    public bool isAttachable = false;           // Can be attached to vehicles, enabling the item

    public float shellDamageMultiplier = 1.0f;  // Multiplied by damagePoints value if the weapon does different dmg to shelled bugs 
    public bool isMortar = false;               // false = will use tracers to find line of sight, true = ignores obstacles
    public bool needsFuel = false;              // Special tag for the Lightning Railgun (maybe other weapons), which needs fuel to start

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
                if (healingPoints != -1 && player.currentHP < player.maxHP && player.currentAP > 0)
                {
                    player.ChangeHP(healingPoints);
                    player.ChangeAP(-1);
                    currentUP--;
                    description = "Use:Heals " + healingPoints + " HP" +
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
                    player.damageToEnemy = 0;
                    player.ClearTargetsAndTracers();
                }
                // Set damageToEnemy as the damage of the weapon
                else
                {
                    player.damageToEnemy = damagePoints;
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
    /// Ensures damageToEnemy is reset after Player drops a weapon
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public bool ProcessDamageAfterWeaponDrop(Player p)
    {
        if (type == ItemType.Weapon)
        {
            p.damageToEnemy = 0;
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
        return new Dictionary<Rarity, int>()
        {
            { Rarity.Common, 35 },
            { Rarity.Limited, 30 },
            { Rarity.Scarce, 20 },
            { Rarity.Rare, 10 },
            { Rarity.Numinous, 4 },
            { Rarity.Secret, 1 }
        };
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
                inf.maxUP = 1;
                inf.currentUP = inf.maxUP;
                inf.healingPoints = 1;
                inf.name = "MEDKIT";
                inf.description = "Use:Heals " + inf.healingPoints + " HP" +
                    "\n" +
                    "UP:" + inf.maxUP + "/" + inf.maxUP;
                break;
            case 1:
                inf.tag = ItemTag.MedKitPlus;
                inf.rarity = Rarity.Rare;
                inf.type = ItemType.Consumable;
                inf.maxUP = 3;
                inf.currentUP = inf.maxUP;
                inf.healingPoints = 2;
                inf.name = "MEDKIT+";
                inf.description = "Use:Heals " + inf.healingPoints + " HP" +
                    "\n" +
                    "UP:" + inf.maxUP + "/" + inf.maxUP;
                break;
            case 2:
                inf.tag = ItemTag.Branch;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "BRANCH";
                inf.maxUP = 2;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 1;
                inf.description = "Use:Equip weapon" +
                    "\n" +
                    "UP:" + inf.maxUP + "/" + inf.maxUP +
                    "\t" +
                    "DP:" + inf.damagePoints;
                break;
            case 3:
                inf.tag = ItemTag.LightningRailgun;
                //inf.rarity = Rarity.Numinous;
                inf.rarity = Rarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "LIGHTNING RAILGUN";
                inf.damagePoints = 5;
                inf.isRanged = true;
                inf.range = 5;
                inf.needsFuel = true;
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
                    inf.tag = ItemTag.RovKit;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Consumable;
                    inf.name = "ROVKIT";
                    inf.maxUP = 2;
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
                    inf.tag = ItemTag.SteelBeam;
                    inf.rarity = Rarity.Limited;
                    inf.type = ItemType.Weapon;
                    inf.name = "STEEL BEAM";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.shellDamageMultiplier = 0.0f;
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
                    inf.name = "MALLET";
                    inf.damagePoints = 2;
                    inf.shellDamageMultiplier = 0.0f;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + "\u221e" +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 7:
                    inf.tag = ItemTag.Axe;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Weapon;
                    inf.name = "AXE";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 4;
                    inf.shellDamageMultiplier = 0.5f;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 4:
                    inf.tag = ItemTag.HonedGavel;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "HONED GAVEL";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.shellDamageMultiplier = 2.0f;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 9:
                    inf.tag = ItemTag.TribladeRotator;
                    inf.rarity = Rarity.Numinous;
                    inf.type = ItemType.Weapon;
                    inf.name = "TRIBLADE ROTATOR";
                    inf.maxUP = 8;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 5;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 10:
                    inf.tag = ItemTag.BladeOfEternity;
                    inf.rarity = Rarity.Numinous;
                    inf.type = ItemType.Weapon;
                    inf.name = "BLADE OF ETERNTY";
                    inf.damagePoints = 4;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + "\u221e" +
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
                    inf.description = "Use:???";
                    break;

                case 12:
                    inf.tag = ItemTag.ExternalTank;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "EXTERNAL TANK";
                    inf.maxUP = 10; // max fuel
                    inf.currentUP = inf.maxUP; // current fuel
                    inf.isAttachable = true;
                    inf.description = "Use:Attach tank";
                    break;

                case 13:
                    inf.tag = ItemTag.Backpack;
                    inf.rarity = Rarity.Limited;
                    inf.type = ItemType.Storage;
                    inf.name = "BACKPACK";
                    inf.isEquipable = true;
                    inf.description = "Use:Equip backpack";
                    break;

                case 14:
                    inf.tag = ItemTag.StorageCrate;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "STORAGE CRATE";
                    inf.isAttachable = true;
                    inf.description = "Use:Attach crate";
                    break;

                case 15:
                    inf.tag = ItemTag.Flashlight;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Utility;
                    inf.name = "FLASHLIGHT";
                    inf.description = "Use:Toggle light";
                    break;

                case 16:
                    inf.tag = ItemTag.Lightrod;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "LIGHTROD";
                    inf.description = "Use:Toggle light";
                    break;

                case 17:
                    inf.tag = ItemTag.Spotlight;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Utility;
                    inf.name = "SPOTLIGHT";
                    inf.isAttachable = true;
                    inf.description = "Use:Attach light";
                    break;

                case 18:
                    inf.tag = ItemTag.Matchbox;
                    inf.rarity = Rarity.Limited;
                    inf.type = ItemType.Utility;
                    inf.name = "MATCHBOX";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.description = "Use:Equip matchbox";
                    break;

                case 19:
                    inf.tag = ItemTag.Blowtorch;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "BLOWTORCH";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.description = "Use:Equip blowtorch";
                    break;

                case 20:
                    inf.tag = ItemTag.StunPistol;
                    inf.rarity = Rarity.Scarce;
                    inf.type = ItemType.Weapon;
                    inf.name = "STUN PISTOL";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 1;
                    inf.isRanged = true;
                    inf.range = 5;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 21:
                    inf.tag = ItemTag.InfernalShotgun;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "INFERNAL SHOTGUN";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.isRanged = true;
                    inf.range = 3;
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
                    inf.isRanged = true;
                    inf.range = 3;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 23:
                    inf.tag = ItemTag.ShellPiercer;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "SHELL PIERCER";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 4;
                    inf.isRanged = true;
                    inf.range = 99;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 24:
                    inf.tag = ItemTag.CatalyticRadiator;
                    inf.rarity = Rarity.Numinous;
                    inf.type = ItemType.Weapon;
                    inf.name = "CATALYTIC RADIATOR";
                    inf.maxUP = 3;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 5;
                    inf.isRanged = true;
                    inf.range = 2;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 25:
                    inf.tag = ItemTag.PFL;
                    inf.rarity = Rarity.Numinous;
                    inf.type = ItemType.Weapon;
                    inf.name = "POSITIVE FEEDBACK LOOP";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 1;   // if get kill, inf.damagePoints++;
                    inf.isRanged = true;
                    inf.range = 7;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP +
                        "\t" +
                        "DP:" + inf.damagePoints;
                    break;

                case 26:
                    inf.tag = ItemTag.BrinkOfExtinction;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Numinous;
                    inf.name = "BRINK OF EXTINCTION";
                    inf.maxUP = 3;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 10;
                    inf.isRanged = true;
                    inf.range = 99;
                    inf.isMortar = true;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 27:
                    inf.tag = ItemTag.BorrowedSpacetime;
                    inf.rarity = Rarity.Rare;
                    inf.type = ItemType.Numinous;
                    inf.name = "BORROWED SPACETIME";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.isRanged = true;
                    inf.range = 99;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 28:
                    inf.tag = ItemTag.PaintBlaster;
                    inf.rarity = Rarity.Secret;
                    inf.type = ItemType.Weapon;
                    inf.name = "PAINT BLASTER";
                    inf.damagePoints = 0;
                    inf.isRanged = true;
                    inf.range = 99;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + "\u221e";
                    break;
                */
        }
        return inf;
    }
}
