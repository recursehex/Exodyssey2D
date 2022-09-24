using System.Collections.Generic;

public class Inventory
{
    public List<ItemInventory> itemList;

    private int invMaxSize = 2;

    public Inventory()
    {
        itemList = new List<ItemInventory>();

        // test code for giving player an item
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

    public bool IsRangedWeaponSelected(int idx)
    {
        return itemList[idx].itemInfo.isRanged;
    }

    public List<ItemInventory> GetItemList()
    {
        return itemList;
    }
}
