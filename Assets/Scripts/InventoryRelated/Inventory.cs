using System.Collections.Generic;

public class Inventory
{
    public List<ItemInventory> itemList;

    private int invMaxSize = 2;
    private int idxSelectedItem = -1;

    public Inventory()
    {
        itemList = new List<ItemInventory>();

        //AddItem(new ItemInventory { itemType = ItemType.MedKit, amount = 1 });
        //AddItem(new ItemInventory { itemType = ItemType.MedKitPlus, amount = 1 });
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

    public List<ItemInventory> GetItemList()
    {
        return itemList;
    }
}
