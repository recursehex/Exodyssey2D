using System.Collections.Generic;

public class Inventory
{
	public List<ItemInventory> itemList;
	public int InventorySize { get; private set;} = 2;
	public Inventory()
	{
		itemList = new List<ItemInventory>();
	}
	/// <summary>
	/// Returns true if Item was successfully added, returns false if inventory is full
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool AddItem(ItemInventory item)
	{
		if (itemList.Count >= InventorySize) return false;
		itemList.Add(item);
		return true;
	}
	/// <summary>
	/// Removes item from inventory at specified index
	/// </summary>
	/// <param name="index"></param>
	public void RemoveItem(int index)
	{
		itemList.RemoveAt(index);
	}
}
