using UnityEngine;
using UnityEngine.UI;

public enum ItemType
{
    // consumable
    MedKit,
    MedKitPlus,
    //RovKit,

    // melee weapon
    Branch,
    Knife,
    SteelBeam,
    Mallet,
    Axe,
    HonedGavel,
    TribladeRotator,
    BladeOfEternity,

    // storage
    HydrogenCanister,
    ExternalTank,
    Backpack,
    StorageCrate,

    // utility
    Flashlight,
    Lightrod,
    Spotlight,
    Matchbox,
    Blowtorch,
    Extinguisher,
    RangeScanner,
    AudioLocalizer,
    ThermalImager,
    QuantumRelocator,
    TemporalSedative,

    // ranged weapon
    Flamethrower,
    PFL,
    LightningRailgun,
    PaintBlaster,

    Unknown,
}

public enum ItemRarity
{
    Common, // white
    Limited, // green
    Scarce, // yellow
    Rare, // blue
    Numinous, // purple
    Secret, // red

    Unknown,
}

public enum ItemClass
{
    Utility,
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
    public ItemType type;
    public ItemRarity rarity;
    public ItemClass itemClass;

    public string itemName;
    public string description; // ingame desc of the item

    public int maxUP = -1; // set to positive value for items with UP, -1 means infinite uses
    public int currentUP;

    public int damagePoints; // set only for items with inf.itemClass = Weapon
    public bool isRanged; // false means Melee, true means Ranged

    public bool isEquipable = false; // can be equipped by characters, enabling the item and emptying an inventory slot
    public bool isAttachable = false; // can be attached to vehicles, enabling the item

    public float shellDamageMultiplier = 1.0f; // multiplied by damagePoints value if the weapon does different dmg to shelled bugs 
    public bool needsFuel = false; // special tag for the Lightning Railgun, which needs fuel to start


    // after click on item, need to act as selecting an item if it is not consumable
    public AfterItemUse UseItem(Player p, int nPos)
    {
        AfterItemUse ret = new AfterItemUse();

        switch (type)
        {
            case ItemType.MedKit:
                if (p.currentHP < p.maxHP)
                {
                    p.ChangeHealth(1);
                    p.ChangeActionPoints(-1);
                    currentUP--;
                }
                break;
            case ItemType.MedKitPlus:
                if (p.currentHP < p.maxHP)
                {
                    p.ChangeHealth(2);
                    p.ChangeActionPoints(-1);
                    currentUP--;
                }
                break;
                
            case ItemType.Branch:
                ProcessSelection(p.inventoryUI.getCurrentSelected(), nPos);
                p.inventoryUI.setCurrentSelected(nPos);
                ret.selectedIdx = nPos;
                p.enemyDamage = damagePoints;
                break;
                /*
            case ItemType.LightningRailgun:
                p.enemyDamage = damagePoints;
                break;
                */
        }
        // if item runs out of UP, it needs to be removed
        if (currentUP == 0)
        {
            ret.fRemove = true;
        }
        return ret;
    }

    public void ProcessSelection(int posOld, int posNew)
    {
        // unselect previous selected item
        if (posOld != -1)
        {
            // make button unpressed
            GameObject.Find("InventoryPressed" + posOld).transform.localScale = new Vector3(1, 1, 1);
        }
 
        // make button pressed
        GameObject.Find("InventoryPressed" + posNew).transform.localScale = new Vector3(0, 0, 0);
    }

    public bool ProcessWeaponUse()
    {
        currentUP--;
        return (currentUP == 0);
    }

    public static ItemInfo ItemFactoryFromNumber(int n)
    {
        ItemInfo inf = new ItemInfo();

        switch(n)
        {
            case 0:
                inf.type = ItemType.MedKit;
                inf.rarity = ItemRarity.Limited;
                inf.itemClass = ItemClass.Consumable;
                inf.maxUP = 1;
                inf.currentUP = inf.maxUP;
                inf.itemName = "MEDKIT";
                inf.description = "Use: Heals 1 HP" +
                    "\n" +
                    "UP:" + inf.currentUP;
                break;

            case 1:
                inf.type = ItemType.MedKitPlus;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Consumable;
                inf.maxUP = 3;
                inf.currentUP = inf.maxUP;
                inf.itemName = "MEDKIT+";
                inf.description = "Use: Heals 2 HP" +
                    "\n" +
                    "UP:" + inf.currentUP;
                break;

            case 2:
                inf.type = ItemType.Branch;
                inf.rarity = ItemRarity.Common;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "BRANCH";
                inf.description = "Use: Equip weapon";
                inf.maxUP = 2;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 1;
                break;
            /*
            case 3:
                inf.type = ItemType.RovKit;
                inf.rarity = ItemRarity.Scarce;
                inf.itemClass = ItemClass.Consumable;
                inf.itemName = "ROVKIT";
                inf.description = "A repair kit that restores any vehicle to full HP per use.";
                inf.maxUP = 2;
                inf.currentUP = inf.maxUP;
                break;

            case 4:
                inf.type = ItemType.Knife;
                inf.rarity = ItemRarity.Limited;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "KNIFE";
                inf.description = "A knife that deals 1 DP per strike.";
                inf.maxUP = 2;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 2;
                break;

            case 5:
                inf.type = ItemType.SteelBeam;
                inf.rarity = ItemRarity.Limited;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "STEEL BEAM";
                inf.description = "A steel beam that deals 2 DP per strike, and no DP to shelled aliens.";
                inf.maxUP = 4;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 2;
                inf.shellDamageMultiplier = 0;
                break;

            case 6:
                inf.type = ItemType.Mallet;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "MALLET";
                inf.description = "Mallet: This heavy mallet deals 2 DP per strike or throw, and no DP to shelled aliens.";
                inf.damagePoints = 2;
                inf.shellDamageMultiplier = 0.0f;
                break;

            case 7:
                inf.type = ItemType.Axe;
                inf.rarity = ItemRarity.Scarce;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "AXE";
                inf.description = "An axe that deals 4 DP per use, and half its DP to shelled aliens.";
                inf.maxUP = 4;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 4;
                inf.shellDamageMultiplier = 0.5f;
                break;
            */

            case 4:
                inf.type = ItemType.HonedGavel;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "HONED GAVEL";
                inf.description = "Unsheathed Colossus: This heavy sword deals 2 DP per strike, and twice its DP to shelled aliens.";
                inf.maxUP = 4;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 2;
                inf.shellDamageMultiplier = 2.0f;
                break;

            /*
            case 9:
                inf.type = ItemType.TribladeRotator;
                inf.rarity = ItemRarity.Numinous;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "TRIBLADE ROTATOR";
                inf.description = "Triple Chainsaw: This triple-bladed chainsaw deals 4 DP per strike.";
                inf.maxUP = 8;
                inf.currentUP = inf.maxUP;
                inf.damagePoints = 5;
                break;

            case 10:
                inf.type = ItemType.BladeOfEternity;
                inf.rarity = ItemRarity.Numinous;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "BLADE OF ETERNTY";
                inf.description = "Perpetual Cleaver: This ethereal blade deals 4 DP per strike.";
                inf.damagePoints = 4;
                break;

            case 11:
                inf.type = ItemType.HydrogenCanister;
                inf.rarity = ItemRarity.Common;
                inf.itemClass = ItemClass.Storage;
                inf.itemName = "HYDROGEN CANISTER";
                inf.description = "A canister that stores up to 5 liters of Hx.";
                inf.maxUP = 5; // max fuel
                inf.currentUP = inf.maxUP; // current fuel
                break;

            case 12:
                inf.type = ItemType.ExternalTank;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Storage;
                inf.itemName = "EXTERNAL TANK";
                inf.description = "An attachable tank that adds a capacity of 10 liters of Hx to a vehicle.";
                inf.maxUP = 10; // max fuel
                inf.currentUP = inf.maxUP; // current fuel
                inf.isAttachable = true;
                break;

            case 13:
                inf.type = ItemType.Backpack;
                inf.rarity = ItemRarity.Limited;
                inf.itemClass = ItemClass.Storage;
                inf.itemName = "BACKPACK";
                inf.description = "An equipable backpack that adds 1 inventory slot.";
                inf.isEquipable = true;
                break;

            case 14:
                inf.type = ItemType.StorageCrate;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Storage;
                inf.itemName = "STORAGE CRATE";
                inf.description = "An attachable crate that adds 4 storage slots to a vehicle.";
                inf.isAttachable = true;
                break;

            case 15:
                inf.type = ItemType.Flashlight;
                inf.rarity = ItemRarity.Scarce;
                inf.itemClass = ItemClass.Utility;
                inf.itemName = "FLASHLIGHT";
                inf.description = "A handheld flashlight that outputs light in one direction.";
                break;

            case 16:
                inf.type = ItemType.Lightrod;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Utility;
                inf.itemName = "LIGHTROD";
                inf.description = "A handheld flashlight that outputs light in all directions. Emits a loud hum that alerts aliens.";
                break;

            case 17:
                inf.type = ItemType.Spotlight;
                inf.rarity = ItemRarity.Scarce;
                inf.itemClass = ItemClass.Utility;
                inf.itemName = "SPOTLIGHT";
                inf.description = "An array of lights that attaches to vehicles.";
                inf.isAttachable = true;
                break;

            case 18:
                inf.type = ItemType.Matchbox;
                inf.rarity = ItemRarity.Limited;
                inf.itemClass = ItemClass.Utility;
                inf.itemName = "MATCHBOX";
                inf.description = "A box of matches that starts fires on tiles or aliens.";
                inf.maxUP = 2;
                inf.currentUP = inf.maxUP;
                break;

            case 19:
                inf.type = ItemType.Blowtorch;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Utility;
                inf.itemName = "BLOWTORCH";
                inf.description = "A blowtorch that starts fires on tiles or aliens.";
                inf.maxUP = 4;
                inf.currentUP = inf.maxUP;
                break;

            case 20:
                inf.type = ItemType.Flamethrower;
                inf.rarity = ItemRarity.Rare;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "FLAMETHROWER";
                inf.description = "Modified Blowtorch: Firing this weapon sprays a streak of fire, roasting anything in its path.";
                inf.maxUP = 4;
                inf.currentUP = inf.maxUP;
                inf.isRanged = true;
                break;

            case 21:
                inf.type = ItemType.PFL;
                inf.rarity = ItemRarity.Numinous;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "POSITIVE FEEDBACK LOOP";
                inf.description = "Vicious Cycle: Kills with this weapon refund ammunition and increase its damage.";
                inf.maxUP = 2;
                inf.damagePoints = 1;
                // if get kill, inf.damagePoints++;
                inf.currentUP = inf.maxUP;
                inf.isRanged = true;
                break;

            case 22:
                inf.type = ItemType.LightningRailgun;
                inf.rarity = ItemRarity.Numinous;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "LIGHTNING RAILGUN";
                inf.description = "Pinnacle of Technology: Charging this weapon fires a devastating blast of extreme electricity. Requires 1 Hx to start, then runs forever.";
                inf.description = "Use: Deals 5 DP" +
                    "\n" +
                    "UP:" + "Infinity";
                inf.damagePoints = 5;
                inf.isRanged = true;
                inf.needsFuel = true;
                break;
            case 23:
                inf.type = ItemType.PaintBlaster;
                inf.rarity = ItemRarity.Secret;
                inf.itemClass = ItemClass.Weapon;
                inf.itemName = "PAINT BLASTER";
                inf.description = "Fueled by Fun: This blaster fires pellets of paint, dealing no DP.";
                inf.damagePoints = 0;
                inf.isRanged = true;
                break;
            */
        }
        return inf;
    }
}
