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

    public int damagePoints;                    // set only for items with inf.ItemType = Weapon
    public bool isRanged;                       // false means Melee, true means Ranged

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
                inf.rarity = ItemRarity.Numinous;
                inf.rarity = ItemRarity.Common;
                inf.type = ItemType.Weapon;
                inf.name = "LIGHTNING RAILGUN";
                inf.damagePoints = 5;
                inf.isRanged = true;
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
                    inf.description = "A repair kit that restores any vehicle to full HP per use.";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    break;

                case 4:
                    inf.tag = ItemTag.Knife;
                    inf.rarity = ItemRarity.Limited;
                    inf.type = ItemType.Weapon;
                    inf.name = "KNIFE";
                    inf.description = "A knife that deals 1 DP per strike.";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    break;

                case 5:
                    inf.tag = ItemTag.SteelBeam;
                    inf.rarity = ItemRarity.Limited;
                    inf.type = ItemType.Weapon;
                    inf.name = "STEEL BEAM";
                    inf.description = "A steel beam that deals 2 DP per strike, and no DP to shelled aliens.";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 2;
                    inf.shellDamageMultiplier = 0;
                    break;

                case 6:
                    inf.tag = ItemTag.Mallet;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "MALLET";
                    inf.description = "Mallet: This heavy mallet deals 2 DP per strike or throw, and no DP to shelled aliens.";
                    inf.damagePoints = 2;
                    inf.shellDamageMultiplier = 0.0f;
                    break;

                case 7:
                    inf.tag = ItemTag.Axe;
                    inf.rarity = ItemRarity.Scarce;
                    inf.type = ItemType.Weapon;
                    inf.name = "AXE";
                    inf.description = "An axe that deals 4 DP per use, and half its DP to shelled aliens.";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 4;
                    inf.shellDamageMultiplier = 0.5f;
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
                    inf.description = "Triple Chainsaw: This triple-bladed chainsaw deals 4 DP per strike.";
                    inf.maxUP = 8;
                    inf.currentUP = inf.maxUP;
                    inf.damagePoints = 5;
                    break;

                case 10:
                    inf.tag = ItemTag.BladeOfEternity;
                    inf.rarity = ItemRarity.Numinous;
                    inf.type = ItemType.Weapon;
                    inf.name = "BLADE OF ETERNTY";
                    inf.description = "Perpetual Cleaver: This ethereal blade deals 4 DP per strike.";
                    inf.damagePoints = 4;
                    break;

                case 11:
                    inf.tag = ItemTag.HydrogenCanister;
                    inf.rarity = ItemRarity.Common;
                    inf.type = ItemType.Storage;
                    inf.name = "HYDROGEN CANISTER";
                    inf.description = "A canister that stores up to 5 liters of Hx.";
                    inf.maxUP = 5; // max fuel
                    inf.currentUP = inf.maxUP; // current fuel
                    break;

                case 12:
                    inf.tag = ItemTag.ExternalTank;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "EXTERNAL TANK";
                    inf.description = "An attachable tank that adds a capacity of 10 liters of Hx to a vehicle.";
                    inf.maxUP = 10; // max fuel
                    inf.currentUP = inf.maxUP; // current fuel
                    inf.isAttachable = true;
                    break;

                case 13:
                    inf.tag = ItemTag.Backpack;
                    inf.rarity = ItemRarity.Limited;
                    inf.type = ItemType.Storage;
                    inf.name = "BACKPACK";
                    inf.description = "An equipable backpack that adds 1 inventory slot.";
                    inf.isEquipable = true;
                    break;

                case 14:
                    inf.tag = ItemTag.StorageCrate;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Storage;
                    inf.name = "STORAGE CRATE";
                    inf.description = "An attachable crate that adds 4 storage slots to a vehicle.";
                    inf.isAttachable = true;
                    break;

                case 15:
                    inf.tag = ItemTag.Flashlight;
                    inf.rarity = ItemRarity.Scarce;
                    inf.type = ItemType.Utility;
                    inf.name = "FLASHLIGHT";
                    inf.description = "A handheld flashlight that outputs light in one direction.";
                    break;

                case 16:
                    inf.tag = ItemTag.Lightrod;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "LIGHTROD";
                    inf.description = "A handheld flashlight that outputs light in all directions. Emits a loud hum that alerts aliens.";
                    break;

                case 17:
                    inf.tag = ItemTag.Spotlight;
                    inf.rarity = ItemRarity.Scarce;
                    inf.type = ItemType.Utility;
                    inf.name = "SPOTLIGHT";
                    inf.description = "An array of lights that attaches to vehicles.";
                    inf.isAttachable = true;
                    break;

                case 18:
                    inf.tag = ItemTag.Matchbox;
                    inf.rarity = ItemRarity.Limited;
                    inf.type = ItemType.Utility;
                    inf.name = "MATCHBOX";
                    inf.description = "A box of matches that starts fires on tiles or aliens.";
                    inf.maxUP = 2;
                    inf.currentUP = inf.maxUP;
                    break;

                case 19:
                    inf.tag = ItemTag.Blowtorch;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Utility;
                    inf.name = "BLOWTORCH";
                    inf.description = "A blowtorch that starts fires on tiles or aliens.";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    break;

                case 20:
                    inf.tag = ItemTag.Flamethrower;
                    inf.rarity = ItemRarity.Rare;
                    inf.type = ItemType.Weapon;
                    inf.name = "FLAMETHROWER";
                    inf.description = "Modified Blowtorch: Firing this weapon sprays a streak of fire, roasting anything in its path.";
                    inf.maxUP = 4;
                    inf.currentUP = inf.maxUP;
                    inf.isRanged = true;
                    break;

                case 21:
                    inf.tag = ItemTag.PFL;
                    inf.rarity = ItemRarity.Numinous;
                    inf.type = ItemType.Weapon;
                    inf.name = "POSITIVE FEEDBACK LOOP";
                    inf.description = "Vicious Cycle: Kills with this weapon refund ammunition and increase its damage.";
                    inf.maxUP = 2;
                    inf.damagePoints = 1;
                    // if get kill, inf.damagePoints++;
                    inf.currentUP = inf.maxUP;
                    inf.isRanged = true;
                    break;

                case 22:
                    inf.tag = ItemTag.LightningRailgun;
                    inf.rarity = ItemRarity.Numinous;
                    inf.type = ItemType.Weapon;
                    inf.name = "LIGHTNING RAILGUN";
                    inf.description = "Pinnacle of Technology: Charging this weapon fires a devastating blast of extreme electricity. Requires 1 Hx to start, then runs forever.";
                    inf.description = "Use: Deals 5 DP" +
                        "\n" +
                        "UP:" + "Infinity";
                    inf.damagePoints = 5;
                    inf.isRanged = true;
                    inf.needsFuel = true;
                    break;
                case 23:
                    inf.tag = ItemTag.PaintBlaster;
                    inf.rarity = ItemRarity.Secret;
                    inf.type = ItemType.Weapon;
                    inf.name = "PAINT BLASTER";
                    inf.description = "Fueled by Fun: This blaster fires pellets of paint, dealing no DP.";
                    inf.damagePoints = 0;
                    inf.isRanged = true;
                    break;
                */
        }
        return inf;
    }
}
