using System.Collections.Generic;

public class Inventory
{
    public List<ItemInventory> itemList;

    private int invMaxSize = 2;

    public Inventory()
    {
        itemList = new List<ItemInventory>();

        // Test code for giving player an item
        //AddItem(new ItemInventory { itemType = ItemType.MedKit, amount = 1 });
    }

    public bool AddItem(ItemInventory item)
    {
        bool ret = false;
        if (itemList.Count < invMaxSize)
        {
            itemList.Add(item);
            ret = true;
        }
        return ret;

    }

    public void RemoveItem(int idx)
    {
        itemList.RemoveAt(idx);
    }

    public bool ProcessDamageAfterWeaponDrop(Player p, int idx)
    {
        return itemList[idx].itemInfo.ProcessDamageAfterWeaponDrop(p);
    }

    public bool ProcessWeaponUse(int idx)
    {
        return itemList[idx].itemInfo.ProcessWeaponUse();
    }

    /// <summary>
    /// Returns 0 if weapon is not ranged, > 0 if it is ranged to specify range
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public int IsRangedWeaponSelected(int idx)
    {
        int ret = 0;
        if (itemList[idx].itemInfo.isRanged)
        {
            ret = itemList[idx].itemInfo.range;
        }
        return ret;
    }

    public List<ItemInventory> GetItemList()
    {
        return itemList;
    }
}
