using System.Collections.Generic;

public class Inventory
{
    public List<ItemInventory> itemList;

    private readonly int invMaxSize = 2;

    public Inventory()
    {
        itemList = new List<ItemInventory>();
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

    public void RemoveItem(int index)
    {
        itemList.RemoveAt(index);
    }

    public bool ProcessDamageAfterWeaponDrop(Player player, int index)
    {
        return itemList[index].itemInfo.ProcessDamageAfterWeaponDrop(player);
    }

    public bool UpdateWeaponUP(int index)
    {
        return itemList[index].itemInfo.UpdateWeaponUP();
    }

    public int GetWeaponRange(int index)
    {
        return itemList[index].itemInfo.range;
    }

    public List<ItemInventory> GetItemList()
    {
        return itemList;
    }
}
