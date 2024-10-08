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
	public bool TryAddItem(ItemInventory Item)
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
	public void RemoveItem(int index)
	{
		InventoryList.RemoveAt(index);
	}
}
