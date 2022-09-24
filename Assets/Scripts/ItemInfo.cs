using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    //PFL,
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

public enum ItemRarity
{
    Common = 0, // white
    Limited,    // green
    Scarce,     // yellow
    Rare,       // blue
    Numinous,   // purple
    Secret,     // red

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
    public bool fRemove = false;
    public int selectedIdx = -1;
}

/// <summary>
/// Contains all item variables, creates items with specific values, and manages items after usage
/// </summary>
public class ItemInfo
{
    public ItemTag tag;                         // name of item
    public ItemRarity rarity;                   // rarity of item
    public ItemType type;                       // type of item

    public string name;                         // ingame name of item
    public string description;                  // ingame desc of item

    public int maxUP = -1;                      // set to positive value for items with UP, -1 means infinite uses
    public int currentUP = -1;

    public int healingPoints = -1;              // set to positive value for items that heal players, -1 means it does not heal

    public int damagePoints = -1;               // set only for items with inf.ItemType = Weapon
    public bool isRanged = false;               // false means Melee, true means Ranged
    public int range = -1;                      // max distance a Ranged weapon can attack to

    public bool isEquipable = false;            // can be equipped by characters, enabling the item and emptying an inventory slot
    public bool isAttachable = false;           // can be attached to vehicles, enabling the item

    public float shellDamageMultiplier = 1.0f;  // multiplied by damagePoints value if the weapon does different dmg to shelled bugs 
    public bool needsFuel = false;              // special tag for the Lightning Railgun (maybe other weapons), which needs fuel to start

    public static int lastItemIdx = (int)ItemTag.Unknown;

    static public List<ItemInfo> GenerateAllPossibleItems()
    {
        List<ItemInfo> ret = new List<ItemInfo>();

        for (int i = 0; i < lastItemIdx; i++)
        {
            ItemInfo item = ItemFactoryFromNumber(i);
            ret.Add(item);
        }

        return ret;
    }

    /// <summary>
    /// Called when player clicks on an item in an inventory slot
    /// </summary>
    /// <param name="p"></param>
    /// <param name="nPos"></param>
    /// <returns></returns>
    public AfterItemUse UseItem(Player p, int nPos)
    {
        AfterItemUse ret = new AfterItemUse();

        // checks by item type category
        switch (type)
        {
            case ItemType.Consumable:
                // if the item heals players
                if (healingPoints != -1 && p.currentHP < p.maxHP)
                {
                    p.ChangeHealth(healingPoints);
                    p.ChangeActionPoints(-1);
                    currentUP--;
                    description = "Use:Heals " + healingPoints + " HP" +
                    "\n" +
                    "UP:" + currentUP;
                }
                break;
            // if item is a weapon as clicking will select it
            case ItemType.Weapon:
                bool fIsSelected = ProcessSelection(p.inventoryUI.getCurrentSelected(), nPos);

                if (!fIsSelected)
                {
                    nPos = -1;
                }

                p.inventoryUI.setCurrentSelected(nPos);

                if (nPos == -1) // reset enemyDamage since weapon was deselected
                {
                    p.enemyDamage = 0;
                }
                else // set player damage as weapon damage
                {
                    p.enemyDamage = damagePoints;
                }

                ret.selectedIdx = nPos;               
                break;
        }
        // if item runs out of UP, it needs to be removed
        if (currentUP == 0)
        {
            ret.fRemove = true;
        }
        return ret;
    }

    /// <summary>
    /// Called by UseItem when a weapon is selected or deselected
    /// </summary>
    /// <param name="posOld"></param>
    /// <param name="posNew"></param>
    public bool ProcessSelection(int posOld, int posNew)
    {
        bool fIsSelected = true;
        // if an item is already selected
        if (posOld != -1)
        {
            if (posNew == posOld)
            {
                // deselect old item
                GameObject.Find("InventoryPressed" + posOld).transform.localScale = new Vector3(1, 1, 1);
                fIsSelected = false;
            }
            else // if selected unselected item
            {
                // deselect old item and select new item
                GameObject.Find("InventoryPressed" + posOld).transform.localScale = new Vector3(1, 1, 1);
                GameObject.Find("InventoryPressed" + posNew).transform.localScale = new Vector3(0, 0, 0);
            }
        }
        else // if no item is selected
        {
            // select new item
            GameObject.Find("InventoryPressed" + posNew).transform.localScale = new Vector3(0, 0, 0);
        }
        return fIsSelected;
    }

    // ensures that player damage is reset after dropping a weapon
    public bool ProcessDamageAfterWeaponDrop(Player p)
    {
        if (type == ItemType.Weapon)
        {
            p.enemyDamage = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Called when a weapon is used to attack
    /// </summary>
    /// <returns></returns>
    public bool ProcessWeaponUse()
    {
        if (currentUP > 0)
        {
            currentUP--;
            description = "Use:Equip weapon" +
                "\n" +
                "UP:" + currentUP +
                "\t" +
                "DP:" + damagePoints;
        }
        else // if infinite UP
        {
            description = "Use:Equip weapon" +
                "\n" +
                "UP:" + "\u221e" +
                "\t" +
                "DP:" + damagePoints;
        }
        return (currentUP == 0);
    }

    public static Dictionary<ItemRarity, int> FillRarityNamestoPercentageMap()
    {
        // weighting for each rarity group
        Dictionary<ItemRarity, int> RarityToPercentage = new Dictionary<ItemRarity, int>
        {
            [ItemRarity.Common] = 35,
            [ItemRarity.Limited] = 30,
            [ItemRarity.Scarce] = 20,
            [ItemRarity.Rare] = 10,
            [ItemRarity.Numinous] = 4,
            [ItemRarity.Secret] = 1,
        };
        return RarityToPercentage;
    }

    public static ItemInfo ItemFactoryFromNumber(int n)
    {
        ItemInfo inf = new ItemInfo();

        switch (n)
        {
            case 0:
                inf.tag = ItemTag.MedKit;
                inf.rarity = ItemRarity.Limited;
                inf.type = ItemType.Consumable;
                inf.maxUP = 1;
                inf.currentUP = inf.maxUP;
                inf.healingPoints = 1;
                inf.name = "MEDKIT";
                inf.description = "Use:Heals " + inf.healingPoints + " HP" +
                    "\n" +
                    "UP:" + inf.maxUP;
                break;

            case 1:
                inf.tag = ItemTag.MedKitPlus;
                inf.rarity = ItemRarity.Rare;
                inf.type = ItemType.Consumable;
                inf.maxUP = 3;
                inf.currentUP = inf.maxUP;
                inf.healingPoints = 2;
                inf.name = "MEDKIT+";
                inf.description = "Use:Heals " + inf.healingPoints + " HP" +
                    "\n" +
                    "UP:" + inf.maxUP;
                break;

            case 2:
                inf.tag = ItemTag.Branch;
                //inf.rarity = ItemRarity.Common;
                inf.rarity = ItemRarity.Scarce;
                inf.type = ItemType.Weapon;
                inf.name = "BRANCH";
                inf.maxUP = 2;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 1;
                inf.description = "Use:Equip weapon" +
                    "\n" +
                    "UP:" + inf.maxUP +
                    "\t" +
                    "DP:" + inf.damagePoints;
                break;
            case 3:
                inf.tag = ItemTag.LightningRailgun;
                //inf.rarity = ItemRarity.Numinous;
                inf.rarity = ItemRarity.Common;
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
                    "DP:" + inf.damagePoints;
                break;
                /*
                case 3:
                    inf.tag = ItemTag.RovKit;
                    inf.rarity = ItemRarity.Scarce;
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
                    inf.rarity = ItemRarity.Limited;
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
                    inf.rarity = ItemRarity.Limited;
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
                    inf.rarity = ItemRarity.Rare;
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
                    inf.rarity = ItemRarity.Scarce;
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
                    inf.rarity = ItemRarity.Rare;
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
                    inf.rarity = ItemRarity.Numinous;
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
                    inf.rarity = ItemRarity.Numinous;
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
                    inf.rarity = ItemRarity.Common;
                    inf.type = ItemType.Storage;
                    inf.name = "HYDROGEN CANISTER";
                    inf.maxUP = 5; // max fuel
                    inf.currentUP = inf.maxUP; // current fuel
                    inf.description = "Use:???";
                    break;

                case 12:
                    inf.tag = ItemTag.ExternalTank;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "EXTERNAL TANK";
                    inf.maxUP = 10; // max fuel
                    inf.currentUP = inf.maxUP; // current fuel
                    inf.isAttachable = true;
                    inf.description = "Use:Attach tank";
                    break;

                case 13:
                    inf.tag = ItemTag.Backpack;
                    inf.rarity = ItemRarity.Limited;
                    inf.type = ItemType.Storage;
                    inf.name = "BACKPACK";
                    inf.isEquipable = true;
                    inf.description = "Use:Equip backpack";
                    break;

                case 14:
                    inf.tag = ItemTag.StorageCrate;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "STORAGE CRATE";
                    inf.isAttachable = true;
                    inf.description = "Use:Attach crate";
                    break;

                case 15:
                    inf.tag = ItemTag.Flashlight;
                    inf.rarity = ItemRarity.Scarce;
                    inf.type = ItemType.Utility;
                    inf.name = "FLASHLIGHT";
                    inf.description = "Use:Toggle light";
                    break;

                case 16:
                    inf.tag = ItemTag.Lightrod;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "LIGHTROD";
                    inf.description = "Use:Toggle light";
                    break;

                case 17:
                    inf.tag = ItemTag.Spotlight;
                    inf.rarity = ItemRarity.Scarce;
                    inf.type = ItemType.Utility;
                    inf.name = "SPOTLIGHT";
                    inf.isAttachable = true;
                    inf.description = "Use:Attach light";
                    break;

                case 18:
                    inf.tag = ItemTag.Matchbox;
                    inf.rarity = ItemRarity.Limited;
                    inf.type = ItemType.Utility;
                    inf.name = "MATCHBOX";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.description = "Use:Equip matchbox";
                    break;

                case 19:
                    inf.tag = ItemTag.Blowtorch;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "BLOWTORCH";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.description = "Use:Equip blowtorch";
                    break;

                case 20:
                    inf.tag = ItemTag.StunPistol;
                    inf.rarity = ItemRarity.Scarce;
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

                case 20:
                    inf.tag = ItemTag.InfernalShotgun;
                    inf.rarity = ItemRarity.Rare;
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

                case 20:
                    inf.tag = ItemTag.Flamethrower;
                    inf.rarity = ItemRarity.Rare;
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

                case 20:
                    inf.tag = ItemTag.ShellPiercer;
                    inf.rarity = ItemRarity.Rare;
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

                case 21:
                    inf.tag = ItemTag.PFL;
                    inf.rarity = ItemRarity.Numinous;
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

                case 20:
                    inf.tag = ItemTag.UniversalIris;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Numinous;
                    inf.name = "UNIVERSAL IRIS";
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

                case 20:
                    inf.tag = ItemTag.TimesEdge;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Numinous;
                    inf.name = "TIME'S EDGE";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.isRanged = true;
                    inf.range = 99;
                    inf.description = "Use:Equip weapon" +
                        "\n" +
                        "UP:" + inf.maxUP;
                    break;

                case 23:
                    inf.tag = ItemTag.PaintBlaster;
                    inf.rarity = ItemRarity.Secret;
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
