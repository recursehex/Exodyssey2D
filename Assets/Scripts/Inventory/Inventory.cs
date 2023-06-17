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

    public int GetWeaponRange(int idx)
    {
        return itemList[idx].itemInfo.range;
    }

    public List<ItemInventory> GetItemList()
    {
        return itemList;
    }
}
