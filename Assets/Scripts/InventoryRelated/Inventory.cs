using System.Collections.Generic;

public class Inventory
{
    public List<ItemInventory> itemList;

    private readonly int invMaxSize = 2;

    public Inventory()
    {
        itemList = new List<ItemInventory>();

        // Test code for giving player an item
        //AddItem(new ItemInventory { itemType = ItemType.MedKit, amount = 1 });
    }

    public bool AddItem(ItemInventory item)
    {
        if (itemList.Count >= invMaxSize)
        {
            return false;
        }
        itemList.Add(item);
        return true;
    }

    public void RemoveItem(int idx)
    {
        itemList.RemoveAt(idx);
    }

    public bool ProcessDamageAfterWeaponDrop(Player p, int idx)
    {
        return itemList[idx].itemInfo.ProcessDamageAfterWeaponDrop(p);
    }

    public bool UpdateWeaponUP(int idx)
    {
        return itemList[idx].itemInfo.UpdateWeaponUP();
    }

    /// <summary>
    /// Returns 0 if weapon is not ranged, > 0 if it is ranged to specify range
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public int IsRangedWeaponSelected(int idx)
    {
        return itemList[idx]?.itemInfo.isRanged == true ? itemList[idx].itemInfo.range : 0;
    }

    public List<ItemInventory> GetItemList()
    {
        return itemList;
    }
}
