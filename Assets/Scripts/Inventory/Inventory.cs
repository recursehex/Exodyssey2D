using System.Collections.Generic;

public class Inventory
{
	public List<ItemInventory> InventoryList;
	public int InventorySize { get; private set;} = 2;
	public Inventory()
	{
		InventoryList = new();
	}
	/// <summary>
	/// Returns true if Item was successfully added, returns false if inventory is full
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool AddItem(ItemInventory Item)
	{
		if (InventoryList.Count >= InventorySize)
		{
			return false;
		}
		InventoryList.Add(Item);
		return true;
	}
	/// <summary>
	/// Removes item from inventory at specified index
	/// </summary>
	/// <param name="index"></param>
	public void RemoveItem(int index)
	{
		InventoryList.RemoveAt(index);
	}
}
