using System.Collections.Generic;

public class Inventory
{
	private readonly List<InventoryItem> InventoryList;
	public int InventorySize { get; private set;} = 2;
	public int Count => InventoryList.Count;
	public Inventory()
	{
		InventoryList = new();
	}
	public InventoryItem this[int index]
	{
		get => InventoryList[index];
	}
	/// <summary>
	/// Returns true if Item was successfully added, returns false if inventory is full
	/// </summary>
	public bool TryAddItem(InventoryItem Item)
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
